using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public partial class DsProject
    {
        #region public functions

        public static readonly int CurrentAllDsPagesCacheSerializationVersion = 4;

        public async Task AllDsPagesCacheUpdateAsync(DrawingInfo[] onDriveDsPageDrawingInfos,
            IProgressInfo? progressInfo, List<string>? errorMessages = null)
        {
            CancellationToken? ct = null;
            if (progressInfo is not null) 
                ct = progressInfo.GetCancellationToken();

            _allDsPagesDsConstantsDictionary = null;
            if (_allDsPagesCache is null)
                _allDsPagesCache = new CaseInsensitiveOrderedDictionary<DsPageDrawing>();

            await CheckDrawingsBinSerializationVersionAsync(onDriveDsPageDrawingInfos, progressInfo);

            HashSet<string> dsPagesNamesBeforeUpdate = new(_allDsPagesCache.Keys);

            var i = 0;
            foreach (DrawingInfo drawingInfo in onDriveDsPageDrawingInfos)
            {
                if (ct.HasValue && ct.Value.IsCancellationRequested)
                    throw new OperationCanceledException("Cancelled by user");

                dsPagesNamesBeforeUpdate.Remove(drawingInfo.Name);

                var dsPageDrawing = _allDsPagesCache.TryGetValue(drawingInfo.Name);
                if (dsPageDrawing is not null && dsPageDrawing.Guid == drawingInfo.Guid)
                {
                }
                else
                {
                    if (progressInfo is not null && i > 0 && i % 10 == 0)
                    {
                        progressInfo.ProgressBarCurrentValue += 10;
                        await progressInfo.SetDescriptionAsync(
                            Resources.ProgressInfo_AllDsPagesCacheUpdate_Description + @" " + i);
                        await progressInfo.RefreshProgressBarAsync();
                    }

                    i += 1;

                    dsPageDrawing = ReadDrawing(drawingInfo.FileInfo, false, loadXamlContent: false) as DsPageDrawing;
                    if (dsPageDrawing is not null)
                    {
                        DsPageDrawing cacheDsPageDrawing = PrepareForCache(dsPageDrawing);
                        dsPageDrawing.Dispose();
                        _allDsPagesCache[drawingInfo.Name] = cacheDsPageDrawing;
                        _allDsPagesCacheIsChanged = true;
                    }
                }
            }

            if (dsPagesNamesBeforeUpdate.Count > 0)
            {
                foreach (string dsPageName in dsPagesNamesBeforeUpdate) _allDsPagesCache.Remove(dsPageName);

                _allDsPagesCacheIsChanged = true;
            }
        }

        public async Task AllDsPagesCacheSaveAsync(IProgressInfo? progressInfo = null)
        {
            if (_allDsPagesCache is not null && _allDsPagesCacheIsChanged && !IsReadOnly)
                try
                {
                    using (var memoryStream = new MemoryStream(10 * 1024 * 1024))
                    {
                        using (var writer = new SerializationWriter(memoryStream, true))
                        {
                            using (writer.EnterBlock(1))
                            {
                                writer.Write(DrawingBase.CurrentSerializationVersionDateTime);

                                CancellationToken? ct = null;
                                if (progressInfo is not null) ct = progressInfo.GetCancellationToken();
                                using (writer.EnterBlock(CurrentAllDsPagesCacheSerializationVersion))
                                {
                                    List<DsPageDrawing> allDsPagesCache = _allDsPagesCache.Values.ToList();
                                    var i = 0;
                                    var count = allDsPagesCache.Count;
                                    writer.Write(count);
                                    foreach (DsPageDrawing dsPageDrawing in allDsPagesCache.OrderBy(p => p.Name))
                                    {
                                        if (ct.HasValue && ct.Value.IsCancellationRequested)
                                            throw new OperationCanceledException("Cancelled by user");
                                        if (progressInfo is not null && i > 0 && i % 10 == 0)
                                        {
                                            progressInfo.ProgressBarCurrentValue += 10;
                                            await progressInfo.SetDescriptionAsync(
                                                Resources.ProgressInfo_SavingCache_Description + @" " + i);
                                            await progressInfo.RefreshProgressBarAsync();
                                        }

                                        i += 1;

                                        writer.Write(dsPageDrawing.Name);
                                        dsPageDrawing.GetDrawingInfo()
                                            .SerializeOwnedData(writer, SerializationContext.IndexFile);
                                        dsPageDrawing.SerializeOwnedData(writer, SerializationContext.IndexFile);
                                    }
                                }
                            }
                        }

                        using (FileStream fileStream = File.Create(DsProjectPath + @"\" + AllDsPagesCacheFileName))
                        {
                            memoryStream.WriteTo(fileStream);
                        }
                    }

                    _allDsPagesCacheIsChanged = false;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                }
        }

        /// <summary>
        ///     [PageName, DsPageDrawing]
        /// </summary>
        [Browsable(false)]
        public CaseInsensitiveOrderedDictionary<DsPageDrawing> AllDsPagesCache =>
            _allDsPagesCache ?? new CaseInsensitiveOrderedDictionary<DsPageDrawing>();

        /// <summary>
        ///     Can be with or without .dspage at the end.
        /// </summary>
        /// <param name="windowWithParam"></param>       
        /// <returns></returns>
        public DsPageDrawing? AllDsPagesCacheGetDsPageDrawing(string dsPageName)
        {
            if (String.IsNullOrEmpty(dsPageName))
                return null;

            if (StringHelper.EndsWithIgnoreCase(dsPageName, DsPageFileExtension))
                dsPageName = dsPageName.Substring(0, dsPageName.Length - DsPageFileExtension.Length);
            return AllDsPagesCache.TryGetValue(dsPageName);
        }

        /// <summary>
        ///     [Computed Constant Value, List of ExtendedDsConstant]
        /// </summary>
        /// <returns></returns>
        public CaseInsensitiveOrderedDictionary<List<ExtendedDsConstant>> AllDsPagesCacheGetAllConstantsValues()
        {
            if (_allDsPagesDsConstantsDictionary is null)
            {
                var allDsPagesDsConstantsDictionary =
                    new CaseInsensitiveOrderedDictionary<List<ExtendedDsConstant>>();

                foreach (DsPageDrawing drawing in AllDsPagesCache.Values)
                {
                    drawing.GetDsConstants(allDsPagesDsConstantsDictionary);
                }

                _allDsPagesDsConstantsDictionary = allDsPagesDsConstantsDictionary;
            }

            return _allDsPagesDsConstantsDictionary;
        }

        public IEnumerable<DsPageDrawing> AllDsPagesCacheFindDsPageDrawing(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                return new DsPageDrawing[0];

            string dsPageFileRelativePath = DataEngine.GetTagInfo(tagName).DsPageFileRelativePath;
            if (!string.IsNullOrEmpty(dsPageFileRelativePath))
            {
                var dsPageDrawing =
                    AllDsPagesCache.TryGetValue(Path.GetFileNameWithoutExtension(dsPageFileRelativePath));
                if (dsPageDrawing is null) return new DsPageDrawing[0];
                return new[] {dsPageDrawing};
            }

            CaseInsensitiveOrderedDictionary<List<ExtendedDsConstant>> allConstantsValues =
                AllDsPagesCacheGetAllConstantsValues();

            var dsConstants = allConstantsValues.TryGetValue(tagName);
            if (dsConstants is null) return new DsPageDrawing[0];

            return dsConstants
                .Select(i =>
                    (DsPageDrawing) (i.ComplexDsShape.GetParentDrawing() ??
                                        throw new InvalidOperationException()))
                .Distinct(ReferenceEqualityComparer<DsPageDrawing>.Default)
                .Where(p => !p.ExcludeFromTagSearch)
                .OrderBy(p => p.IsFaceplate)
                .ThenBy(p => p.Name);
        }

        public async Task<DrawingInfo[]> GetDrawingInfosAsync(FileInfo[] drawingFileInfos,
            IProgressInfo? progressInfo, List<string>? errorMessages = null)
        {
            if (!IsInitialized) return new DrawingInfo[0];

            var drawingInfos = new List<DrawingInfo>(drawingFileInfos.Length);

            var i = 0;
            foreach (FileInfo fi in drawingFileInfos)
            {
                if (progressInfo is not null && i > 0 && i % 10 == 0)
                {
                    progressInfo.ProgressBarCurrentValue += 10;
                    await progressInfo.SetDescriptionAsync(
                        Resources.ProgressInfo_GetAllDsPageDrawingInfos_Description + @" " + i);
                    await progressInfo.RefreshProgressBarAsync();
                }

                i += 1;

                var drawingInfo = ReadDrawingInfo(fi, true, errorMessages);
                if (drawingInfo is not null) drawingInfos.Add(drawingInfo);
            }

            return drawingInfos.ToArray();
        }

        public void AllDsPagesCacheDelete()
        {
            _allDsPagesDsConstantsDictionary = null;
            _allDsPagesCache = null;
            _allDsPagesCacheIsChanged = false;
        }

        public void AllComplexDsShapesCacheDelete()
        {
            _allDsShapeDrawingInfos = null;
        }

        #endregion

        #region private functions

        private static DsPageDrawing PrepareForCache(DsPageDrawing dsPageDrawing)
        {
            var cacheDsPageDrawing = new DsPageDrawing(false, false);
            var drawingInfo = dsPageDrawing.GetDrawingInfo();
            cacheDsPageDrawing.SetDrawingInfo(drawingInfo);
            PrepareForCache(dsPageDrawing, cacheDsPageDrawing);
            return cacheDsPageDrawing;
        }

        private static void PrepareForCache(IDsContainer oldContainer, IDsContainer newContainer)
        {
            foreach (var gpi in oldContainer.DsConstantsCollection)
                newContainer.DsConstantsCollection.Add(new DsConstant(gpi));

            var oldDsShapes = oldContainer.DsShapes;
            if (oldDsShapes.Length == 0) return;
            var newDsShapes = new DsShapeBase[oldDsShapes.Length];
            for (var i = 0; i < oldDsShapes.Length; i += 1)
            {
                DsShapeBase oldDsShape = oldDsShapes[i];
                var complexDsShape = oldDsShape as ComplexDsShape;
                if (complexDsShape is not null)
                {
                    var emptyComplexDsShape = new EmptyComplexDsShape(false, false);
                    PrepareForCache(complexDsShape, emptyComplexDsShape);
                    newDsShapes[i] = emptyComplexDsShape;
                }
                else
                {
                    var emptyDsShape = new EmptyDsShape(false, false);                    
                    IEnumerable<FieldInfo> fields = ObjectHelper.GetAllFields(oldDsShape);
                    foreach (FieldInfo field in fields)
                        if (field.FieldType == typeof(DsCommand))
                        {
                            var dsCommand = field.GetValue(oldDsShape) as DsCommand;
                            if (dsCommand is not null && !string.IsNullOrEmpty(dsCommand.Command))
                            {
                                if (emptyDsShape.DsCommands is null)
                                    emptyDsShape.DsCommands = new List<DsCommand>();
                                var dsCommandClone = (DsCommand) dsCommand.Clone();
                                dsCommandClone.ParentItem = emptyDsShape;
                                emptyDsShape.DsCommands.Add(dsCommandClone);
                            }
                        }

                    newDsShapes[i] = emptyDsShape;
                }
            }

            newContainer.DsShapes = newDsShapes;
        }

        #endregion

        #region private fields

        /// <summary>
        ///     [PageName (no extension), DsPageDrawing]
        /// </summary>
        private CaseInsensitiveOrderedDictionary<DsPageDrawing>? _allDsPagesCache;

        private bool _allDsPagesCacheIsChanged;

        /// <summary>
        ///     [PageName (no extension), List]
        /// </summary>
        private volatile CaseInsensitiveOrderedDictionary<List<ExtendedDsConstant>>?
            _allDsPagesDsConstantsDictionary;

        /// <summary>
        ///     [PageName (no extension), DsShapeDrawingInfo]
        /// </summary>
        private CaseInsensitiveOrderedDictionary<DsShapeDrawingInfo>? _allDsShapeDrawingInfos;

        #endregion
    }
}