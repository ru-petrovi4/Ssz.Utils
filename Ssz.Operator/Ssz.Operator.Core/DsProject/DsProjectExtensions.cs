using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;

using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public static partial class DsProjectExtensions
    {
        #region public functions

        public static bool IsValidLength(double length)
        {
            return !double.IsNaN(length) && !double.IsInfinity(length) &&
                   length > 0.0;
        }


        public static bool IsValidCenterRelativePosition(Point point)
        {
            return !double.IsNaN(point.X) && !double.IsInfinity(point.X) &&
                   point.X >= 0.0 && point.X <= 1.0 &&
                   !double.IsNaN(point.Y) && !double.IsInfinity(point.Y) &&
                   point.Y >= 0.0 && point.Y <= 1.0;
        }


        public static bool IsPlayMode(this DsProject dsProject)
        {
            return dsProject.Mode == DsProject.DsProjectModeEnum.WindowsPlayMode ||
                   dsProject.Mode == DsProject.DsProjectModeEnum.WebPlayMode;
        }


        public static ComplexDsShape NewComplexDsShape(this DsProject dsProject, DsShapeBase[] dsShapes)
        {
            var dsShapesRect = DsShapeBase.GetBoundingRect(dsShapes);
            var newComplexDsShape = new ComplexDsShape(true, true);
            newComplexDsShape.Name = @"Group";

            newComplexDsShape.WidthInitial = dsShapesRect.Width;
            newComplexDsShape.HeightInitial = dsShapesRect.Height;

            newComplexDsShape.LeftNotTransformed = dsShapesRect.Left;
            newComplexDsShape.TopNotTransformed = dsShapesRect.Top;

            newComplexDsShape.DsShapeDrawingGuid = Guid.NewGuid();
            newComplexDsShape.DsShapeDrawingName = @"";
            newComplexDsShape.DsShapeDrawingDesc = @"";
            newComplexDsShape.DsShapeDrawingGroup = @"";
            newComplexDsShape.DsShapeDrawingWidth = dsShapesRect.Width;
            newComplexDsShape.DsShapeDrawingHeight = dsShapesRect.Height;
            newComplexDsShape.DsShapeDrawingCenterRelativePosition = new Point(0.5, 0.5);

            foreach (DsShapeBase dsShape in dsShapes)
            {
                var centerInitialPositionNotRounded = dsShape.CenterInitialPositionNotRounded;
                centerInitialPositionNotRounded.X = centerInitialPositionNotRounded.X - dsShapesRect.X;
                centerInitialPositionNotRounded.Y = centerInitialPositionNotRounded.Y - dsShapesRect.Y;
                dsShape.CenterInitialPosition = centerInitialPositionNotRounded;
            }

            newComplexDsShape.DsShapes = dsShapes;
            newComplexDsShape.RefreshDsConstantsCollection();
            newComplexDsShape.RefreshForPropertyGrid(newComplexDsShape);

            return newComplexDsShape;
        }


        public static DsPageDrawing NewDsPageDrawingObject(this DsProject dsProject, string name,
            bool visualDesignMode)
        {
            if (!dsProject.IsInitialized) throw new InvalidOperationException();

            var drawing = new DsPageDrawing(visualDesignMode, true)
            {
                Width = dsProject.DefaultDsPageSize.Width,
                Height = dsProject.DefaultDsPageSize.Height,
                FileFullName = dsProject.DsPagesDirectoryInfo!.FullName + @"\" + name + DsProject.DsPageFileExtension
            };

            return drawing;
        }


        public static DsShapeDrawing NewDsShapeDrawingObject(this DsProject dsProject,
            string name,
            bool visualDesignMode, ComplexDsShape? complexDsShape = null)
        {
            if (!dsProject.IsInitialized) throw new InvalidOperationException();

            var drawing = new DsShapeDrawing(visualDesignMode, true)
            {
                FileFullName = dsProject.DsShapesDirectoryInfo?.FullName + @"\" + name + DsProject.DsShapeFileExtension
            };

            if (complexDsShape is not null)
            {
                drawing.Guid = complexDsShape.DsShapeDrawingGuid;
                drawing.Desc = complexDsShape.DsShapeDrawingDesc;
                drawing.Group = complexDsShape.DsShapeDrawingGroup;
                drawing.Width = IsValidLength(complexDsShape.DsShapeDrawingWidth)
                    ? complexDsShape.DsShapeDrawingWidth
                    : complexDsShape.WidthInitialNotRounded;
                drawing.Height = IsValidLength(complexDsShape.DsShapeDrawingHeight)
                    ? complexDsShape.DsShapeDrawingHeight
                    : complexDsShape.HeightInitialNotRounded;

                string xaml = XamlHelper.Save(complexDsShape.DsShapesArray ?? new ArrayList());
                drawing.DsShapesArray = XamlHelper.Load(xaml);

                drawing.TransformDsShapes(drawing.Width / complexDsShape.WidthInitialNotRounded,
                    drawing.Height / complexDsShape.HeightInitialNotRounded);

                drawing.CenterRelativePosition =
                    IsValidCenterRelativePosition(complexDsShape.DsShapeDrawingCenterRelativePosition)
                        ? complexDsShape.DsShapeDrawingCenterRelativePosition
                        : complexDsShape.CenterRelativePosition;

                foreach (DsConstant dsConstant in complexDsShape.DsConstantsCollection)
                {
                    var dsConstantCopy = new DsConstant(dsConstant);
                    dsConstantCopy.Value = dsConstant.DefaultValue;
                    dsConstantCopy.DefaultValue = "";
                    drawing.DsConstantsCollection.Add(dsConstantCopy);
                }

                foreach (var dsShape in drawing.DsShapes)
                    dsShape.RefreshForPropertyGrid(dsShape.Container);
            }

            return drawing;
        }


        public static async Task<ToolkitOperationResult> UpdateComplexDsShapesAsync(this DsProject dsProject,
            DrawingInfo[]? updatingDrawingInfos,
            UpdateComplexDsShapesToolkitOperationOptions? toolkitOperationOptions,
            IProgressInfo? progressInfo = null)
        {
            if (!dsProject.IsInitialized || dsProject.IsReadOnly) return ToolkitOperationResult.DoneWithErrors;

            var notifyProgressDsShapesUpdate = false;
            if (updatingDrawingInfos is null)
            {
                notifyProgressDsShapesUpdate = true;
                updatingDrawingInfos = dsProject.GetAllDsPageDrawingInfos().Values.OfType<DrawingInfo>().ToArray();
            }

            if (updatingDrawingInfos.Length == 0) return ToolkitOperationResult.Done;

            var result = ToolkitOperationResult.Done;

            if (toolkitOperationOptions is null)
                toolkitOperationOptions = new UpdateComplexDsShapesToolkitOperationOptions
                {
                    ComplexDsShapeNames = "",
                    ResetSizeToOriginal = false,
                    ResetCenterRelativePositionToOriginal = false,
                    MoveCenterPointOfDsShapes = new Point(0, 0)
                };

            CaseInsensitiveDictionary<DsShapeDrawingInfo> selectedDsShapeDrawingInfos;
            if (string.IsNullOrEmpty(toolkitOperationOptions.ComplexDsShapeNames))
            {
                selectedDsShapeDrawingInfos = dsProject.GetAllComplexDsShapesDrawingInfos();
            }
            else
            {
                selectedDsShapeDrawingInfos = new CaseInsensitiveDictionary<DsShapeDrawingInfo>();
                foreach (string complexDsShapeName in toolkitOperationOptions.ComplexDsShapeNames.Split(','))
                {
                    string shapeFileName = complexDsShapeName.Trim();
                    if (!shapeFileName.EndsWith(DsProject.DsShapeFileExtension, StringComparison.InvariantCultureIgnoreCase))
                        shapeFileName += DsProject.DsShapeFileExtension;
                    var fileInfo =
                        dsProject.GetExistingDsShapeFileInfoOrNull(shapeFileName);
                    if (fileInfo is null)
                    {
                        MessageBoxHelper.ShowError("Cannot find shape: " +
                                                   shapeFileName);
                        return ToolkitOperationResult.DoneWithErrors;
                    }

                    var dsShapeDrawingInfo =
                        DsProject.ReadDrawingInfo(fileInfo, false) as DsShapeDrawingInfo;
                    if (dsShapeDrawingInfo is null)
                    {
                        MessageBoxHelper.ShowError("Cannot read shape: " +
                                                   shapeFileName);
                        return ToolkitOperationResult.DoneWithErrors;
                    }

                    selectedDsShapeDrawingInfos.Add(dsShapeDrawingInfo.Name,
                        dsShapeDrawingInfo);
                }
            }

            var cancellationToken =
                progressInfo is not null ? progressInfo.GetCancellationToken() : new CancellationToken();

            var dsShapeDrawingsCache = new CaseInsensitiveDictionary<DsShapeDrawing>();

            var changedDsShapesInLoop = new List<string>();
            for (var iteration = 0; iteration < 10; iteration += 1)
            {
                if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;

                changedDsShapesInLoop.Clear();

                var i = 0;
                var count = selectedDsShapeDrawingInfos.Count;
                foreach (
                    DrawingInfo updatingDsShapeDrawingInfo in
                    selectedDsShapeDrawingInfos.Values.ToArray())
                {
                    i += 1;
                    if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;
                    if (progressInfo is not null && notifyProgressDsShapesUpdate)
                        await progressInfo.RefreshProgressBarAsync(i, count);

                    try
                    {
                        var updatingDsShapeDrawing =
                            DsProject.ReadDrawing(updatingDsShapeDrawingInfo.FileInfo, false, true) as
                                DsShapeDrawing;
                        if (updatingDsShapeDrawing is not null)
                        {
                            var o = new UpdateComplexDsShapesToolkitOperationOptions
                            {
                                ResetSizeToOriginal = false,
                                ResetCenterRelativePositionToOriginal = false,
                                MoveCenterPointOfDsShapes = new Point(0, 0)
                            };
                            var changesCount = UpdateComplexDsShapes(updatingDsShapeDrawingInfo,
                                updatingDsShapeDrawing.DsShapes,
                                progressInfo,
                                selectedDsShapeDrawingInfos, dsShapeDrawingsCache, o);
                            if (changesCount > 0)
                            {
                                dsProject.SaveUnconditionally(updatingDsShapeDrawing,
                                    DsProject.IfFileExistsActions.CreateBackup);

                                selectedDsShapeDrawingInfos[updatingDsShapeDrawing.Name] =
                                    (DsShapeDrawingInfo) updatingDsShapeDrawing.GetDrawingInfo();

                                changedDsShapesInLoop.Add(updatingDsShapeDrawing.Name);
                            }

                            AddDsShapeDrawingToCache(dsShapeDrawingsCache,
                                updatingDsShapeDrawing);
                        }
                        else
                        {
                            if (progressInfo is not null)
                                await progressInfo.DebugInfoAsync(
                                    updatingDsShapeDrawingInfo.FileInfo.FullName + @": " +
                                    Resources.DrawingFileOpenErrorGeneral
                                    + "; " + Resources.SeeErrorLogForDetails);
                        }
                    }
                    catch (Exception ex)
                    {
                        result = ToolkitOperationResult.DoneWithErrors;
                        DsProject.LoggersSet.Logger.LogError(ex, @"");
                    }
                }

                if (changedDsShapesInLoop.Count == 0) break;
            }

            if (changedDsShapesInLoop.Count > 0 && progressInfo is not null)
                await progressInfo.DebugInfoAsync(Resources.NotAllComplexDsShapesUpdatedMessage
                                                  + @": " + string.Join(@", ", changedDsShapesInLoop));

            var changedDsPagesCount = 0;
            var i2 = 0;
            var count2 = updatingDrawingInfos.Length;
            foreach (DrawingInfo updatingDrawingInfo in updatingDrawingInfos)
            {
                i2 += 1;
                if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;
                if (progressInfo is not null) await progressInfo.RefreshProgressBarAsync(i2, count2);

                try
                {
                    var updatingDrawing = DsProject.ReadDrawing(updatingDrawingInfo.FileInfo, false, true);
                    if (updatingDrawing is not null)
                    {
                        var changesCount = UpdateComplexDsShapes(updatingDrawingInfo,
                            updatingDrawing.DsShapes,
                            progressInfo,
                            selectedDsShapeDrawingInfos,
                            dsShapeDrawingsCache, toolkitOperationOptions);
                        if (changesCount > 0)
                        {
                            dsProject.SaveUnconditionally(updatingDrawing,
                                DsProject.IfFileExistsActions.CreateBackup);
                            changedDsPagesCount += 1;
                        }

                        updatingDrawing.Dispose();
                    }
                    else
                    {
                        if (progressInfo is not null)
                            await progressInfo.DebugInfoAsync(updatingDrawingInfo.FileInfo.FullName + @": " +
                                                              Resources.DrawingFileOpenErrorGeneral
                                                              + "; " + Resources.SeeErrorLogForDetails);
                    }
                }
                catch (Exception ex)
                {
                    result = ToolkitOperationResult.DoneWithErrors;
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                }
            }

            foreach (var drawing in dsShapeDrawingsCache.Values) drawing.Dispose();

            if (progressInfo is not null)
                await progressInfo.DebugInfoAsync(Resources.ChangedDsPagesCount
                                                  + ": " + changedDsPagesCount);

            return result;
        }


        public static async Task<ToolkitOperationResult> RestoreComplexDsShapesAsync(this DsProject dsProject,
            IProgressInfo? progressInfo,
            DrawingBase[] drawingsToRestoreFrom, CaseInsensitiveDictionary<DsShapeDrawing> newDrawings)
        {
            if (!dsProject.IsInitialized || dsProject.IsReadOnly) return ToolkitOperationResult.DoneWithErrors;

            var cancellationToken =
                progressInfo is not null ? progressInfo.GetCancellationToken() : new CancellationToken();

            var i = 0;
            var count = drawingsToRestoreFrom.Length;
            foreach (var drawing in drawingsToRestoreFrom)
            {
                i += 1;
                if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;
                if (progressInfo is not null) await progressInfo.RefreshProgressBarAsync(i, count);

                var drawingChanged = RestoreComplexDsShapes(dsProject,
                    drawing.DsShapes.OfType<ComplexDsShape>(), newDrawings);
                if (drawingChanged)
                    dsProject.SaveUnconditionally(drawing, DsProject.IfFileExistsActions.CreateBackup);
            }

            //var drawingsToRestoreFromList = drawingsToRestoreFrom.ToList();
            //for (int iteration = 0; iteration < 10; iteration += 1)
            //{
            //drawingsToRestoreFromList = newDrawings.Select(kvp => Tuple.Create((DrawingInfo)null, kvp.Value)).ToList();
            //}
            //if (drawingsToRestoreFromList.Count != 0)
            //    MessageBoxHelper.ShowWarning(Resources.NotAllComplexDsShapesRestoredMessage
            //        + @": " + String.Join(@", ", drawingsToRestoreFromList.Select(t => t.Item1 is not null ? t.Item1.Name : t.Item2.Name)));

            return ToolkitOperationResult.Done;
        }


        public static void CopyDrawingToDsProject(this DsProject dsProject, FileInfo drawingFileInfo)
        {
            if (DrawingBase.IsDsPageFile(drawingFileInfo))
            {
                dsProject.DrawingCopy(drawingFileInfo, new FileInfo(dsProject.DsPagesDirectoryInfo!.FullName + @"\" +
                                                                     drawingFileInfo.Name));
            }
            else if (DrawingBase.IsDsShapeFile(drawingFileInfo))
            {
                var dsShapesDirectoryInfo = dsProject.DsShapesDirectoryInfo;
                if (dsShapesDirectoryInfo is null) return;

                dsProject.DrawingCopy(drawingFileInfo, new FileInfo(dsShapesDirectoryInfo.FullName + @"\" +
                                                                     drawingFileInfo.Name));
            }
        }

        public static List<DrawingInfo>? GetDrawingInfosListFromUser(this DsProject dsProject)
        {
            if (!dsProject.IsInitialized) return null;

            var dlg = new OpenFileDialog
            {
                Title = Resources.GetDsPageDrawingsListDialogTitle,
                Multiselect = true,
                Filter = @"All Drawing Types|*" + DsProject.DsPageFileExtension + ";*" + DsProject.DsShapeFileExtension,
                InitialDirectory = dsProject.DsPagesDirectoryInfo!.FullName
            };

            if (dlg.ShowDialog() != true) return null;

            try
            {
                var result = new List<DrawingInfo>();

                var errorMessages = new List<string>();

                foreach (string fileName in dlg.FileNames)
                {
                    var fi = new FileInfo(fileName);
                    var drawingInfo = DsProject.ReadDrawingInfo(fi, false, errorMessages);
                    if (drawingInfo is not null) result.Add(drawingInfo);
                }

                if (errorMessages.Count > 0) MessageBoxHelper.ShowWarning(string.Join("\n", errorMessages));

                return result;
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
                return null;
            }
        }

        public static PlayWindowClassOptions GetPlayWindowClassOptions(this DsProject dsProject, IPlayWindow playWindow)
        {
            if (!dsProject.IsInitialized) return PlayWindowClassOptions.Default;

            foreach (var playWindowClassOptions in dsProject.PlayWindowClassOptionsCollection
                .OrderByDescending(o => !String.IsNullOrEmpty(o.PlayWindowClassInfo.WindowCategory) && o.PlayWindowClassInfo.WindowDsPageTypeGuid != Guid.Empty)
                .ThenByDescending(o => !String.IsNullOrEmpty(o.PlayWindowClassInfo.WindowCategory))
                .ThenByDescending(o => o.PlayWindowClassInfo.WindowDsPageTypeGuid != Guid.Empty))
            {
                if (playWindowClassOptions.PlayWindowClassInfo.IsForPlayWindow(playWindow))
                    return playWindowClassOptions;
            }

            return PlayWindowClassOptions.Default;
        }

        #endregion

        /*
        public bool RestoreDsProjectComplexDsShapes(List<DrawingInfo> drawingInfosToRestoreFrom)
        {
            if (drawingInfosToRestoreFrom is null) return false;
            if (DsProjectFileInfo is null) return false;

            if (DrawingsStoreMode == DrawingsStoreModeEnum.XamlMode)
            {
                throw new Exception(Resources.RestoreDsProjectComplexDsShapesXamlModeError);
            }

            var allComplexDsShapesDrawingInfos = new Dictionary<Guid, DrawingInfo>();
            foreach (DrawingInfo drawingInfo in GetAllStandardComplexDsShapesDrawingInfos().Values)
            {
                allComplexDsShapesDrawingInfos[drawingInfo.Guid] = drawingInfo;
            }
            foreach (DrawingInfo drawingInfo in GetAllDsProjectComplexDsShapesDrawingInfos().Values)
            {
                allComplexDsShapesDrawingInfos[drawingInfo.Guid] = drawingInfo;
            }

            foreach (DrawingInfo drawingInfo in drawingInfosToRestoreFrom)
            {
                DrawingBase drawing = ReadDrawing(drawingInfo.FileInfo, false, true);
                if (drawing is null) continue;
                bool drawingChanged = RestoreComplexDsShapes(drawing.DsShapes, allComplexDsShapesDrawingInfos);
                if (drawingChanged) SaveUnconditionally(drawing, IfFileExistsActions.CreateBackup, true);
            }

            OnDsShapeDrawingsListChanged();

            return false;
        }*/

        #region private functions

        private static int UpdateComplexDsShapes(DrawingInfo drawingInfo, IEnumerable<DsShapeBase> dsShapes,
            IProgressInfo? progressInfo,
            CaseInsensitiveDictionary<DsShapeDrawingInfo> selectedDsShapeDrawingInfos,
            CaseInsensitiveDictionary<DsShapeDrawing> dsShapeDrawingsCache,
            UpdateComplexDsShapesToolkitOperationOptions toolkitOperationOptions)
        {
            var changesCount = 0;
            foreach (DsShapeBase dsShape in dsShapes)
            {
                var complexDsShape = dsShape as ComplexDsShape;
                if (complexDsShape is null) continue;

                if (!string.IsNullOrEmpty(complexDsShape.DsShapeDrawingName))
                {
                    DrawingInfo? di =
                        selectedDsShapeDrawingInfos.TryGetValue(
                            complexDsShape.DsShapeDrawingName);
                    if (di is null) continue;

                    if (!toolkitOperationOptions.ResetSizeToOriginal &&
                        !toolkitOperationOptions.ResetCenterRelativePositionToOriginal)
                    {
                        if (di.Guid != complexDsShape.DsShapeDrawingGuid)
                        {
                            var dsShapeDrawing =
                                GetDsShapeDrawingFromCache(dsShapeDrawingsCache, di);
                            if (dsShapeDrawing is not null)
                            {
                                complexDsShape.CropUnusedSpace();
                                complexDsShape.PrepareComplexDsShapeGeometry(dsShapeDrawing);
                                dsShapeDrawing.FillInComplexDsShape(complexDsShape);
                                if (progressInfo is not null)
                                    progressInfo.DebugInfoAsync(drawingInfo.FileInfo.Name + @", " +
                                                                Resources.DsShapeUpdated + ": " +
                                                                complexDsShape.GetDsShapeNameToDisplayAndType());
                                changesCount += 1;
                            }
                        }
                    }
                    else
                    {
                        var dsShapeDrawing =
                            GetDsShapeDrawingFromCache(dsShapeDrawingsCache, di);
                        if (dsShapeDrawing is not null)
                        {
                            if (toolkitOperationOptions.ResetSizeToOriginal)
                            {
                                complexDsShape.WidthInitial = dsShapeDrawing.Width;
                                complexDsShape.HeightInitial = dsShapeDrawing.Height;
                                if (progressInfo is not null)
                                    progressInfo.DebugInfoAsync(drawingInfo.FileInfo.Name + @", " +
                                                                Resources.DsShapeResetedSizeToOriginal + ": " +
                                                                complexDsShape.GetDsShapeNameToDisplayAndType());
                            }

                            if (toolkitOperationOptions.ResetCenterRelativePositionToOriginal)
                            {
                                var left = complexDsShape.LeftNotTransformed;
                                var top = complexDsShape.TopNotTransformed;
                                complexDsShape.CenterRelativePosition =
                                    dsShapeDrawing.CenterRelativePosition;
                                complexDsShape.LeftNotTransformed = left;
                                complexDsShape.TopNotTransformed = top;
                                if (progressInfo is not null)
                                    progressInfo.DebugInfoAsync(drawingInfo.FileInfo.Name + @", " +
                                                                Resources
                                                                    .DsShapeResetedCenterRelativePositionToOriginal +
                                                                ": " +
                                                                complexDsShape.GetDsShapeNameToDisplayAndType());
                            }

                            dsShapeDrawing.FillInComplexDsShape(complexDsShape);
                            if (progressInfo is not null)
                                progressInfo.DebugInfoAsync(drawingInfo.FileInfo.Name + @", " +
                                                            Resources.DsShapeUpdated + ": " +
                                                            complexDsShape.GetDsShapeNameToDisplayAndType());
                            changesCount += 1;
                        }
                    }

                    if (toolkitOperationOptions.MoveCenterPointOfDsShapes != new Point(0, 0))
                    {
                        complexDsShape.CenterInitialPosition = new Point(
                            complexDsShape.CenterInitialPosition.X + toolkitOperationOptions.MoveCenterPointOfDsShapes.X,
                            complexDsShape.CenterInitialPosition.Y +
                            toolkitOperationOptions.MoveCenterPointOfDsShapes.Y);
                        if (progressInfo is not null)
                            progressInfo.DebugInfoAsync(drawingInfo.FileInfo.Name + @", " +
                                                        Resources.DsShapeCenterPointMoved + ": " +
                                                        complexDsShape.GetDsShapeNameToDisplayAndType());
                        changesCount += 1;
                    }
                }
                else
                {
                    changesCount += UpdateComplexDsShapes(drawingInfo, complexDsShape.DsShapes,
                        progressInfo,
                        selectedDsShapeDrawingInfos,
                        dsShapeDrawingsCache, toolkitOperationOptions);
                }
            }

            return changesCount;
        }


        private static DsShapeDrawing? GetDsShapeDrawingFromCache(
            CaseInsensitiveDictionary<DsShapeDrawing> dsShapeDrawingsCache,
            DrawingInfo dsShapeDrawingInfo)
        {
            var dsShapeDrawing = dsShapeDrawingsCache.TryGetValue(
                dsShapeDrawingInfo.Name);
            if (dsShapeDrawing is null)
            {
                dsShapeDrawing =
                    DsProject.ReadDrawing(dsShapeDrawingInfo.FileInfo, false, true) as
                        DsShapeDrawing;
                if (dsShapeDrawing is not null)
                    AddDsShapeDrawingToCache(dsShapeDrawingsCache, dsShapeDrawing);
            }

            return dsShapeDrawing;
        }


        private static void AddDsShapeDrawingToCache(
            CaseInsensitiveDictionary<DsShapeDrawing> dsShapeDrawingsCache,
            DsShapeDrawing dsShapeDrawing)
        {
            dsShapeDrawing.CropUnusedSpace();
            dsShapeDrawingsCache[dsShapeDrawing.Name] = dsShapeDrawing;
        }


        private static bool RestoreComplexDsShapes(DsProject dsProject,
            IEnumerable<ComplexDsShape> complexDsShapes,
            CaseInsensitiveDictionary<DsShapeDrawing> newDrawings)
        {
            var drawingChanged = false;

            foreach (ComplexDsShape complexDsShape in complexDsShapes)
            {
                var dsShapeDrawingName = complexDsShape.DsShapeDrawingName;

                if (string.IsNullOrEmpty(dsShapeDrawingName))
                {
                    if (RestoreComplexDsShapes(dsProject, complexDsShape.DsShapes.OfType<ComplexDsShape>(),
                        newDrawings)) drawingChanged = true;
                    continue;
                }

                // Search #1
                var allComplexDsShapesDrawingInfos = dsProject.GetAllComplexDsShapesDrawingInfos();
                var existingDsShapeDrawingInfo =
                    allComplexDsShapesDrawingInfos.TryGetValue(dsShapeDrawingName);
                if (existingDsShapeDrawingInfo is not null) continue;

                if (complexDsShape.DsShapes.Length > 0)
                {
                    // Search #2
                    DsShapeDrawing dsShapeDrawingFromDsShape =
                        dsProject.NewDsShapeDrawingObject(dsShapeDrawingName +
                                                                     @"_temp", false, complexDsShape);
                    if (dsShapeDrawingFromDsShape is null) continue;
                    dsShapeDrawingFromDsShape.CropUnusedSpace();
                    var drawingInfoFromDsShape =
                        (DsShapeDrawingInfo)dsShapeDrawingFromDsShape.GetDrawingInfo();
                    foreach (var kvp in allComplexDsShapesDrawingInfos)
                        if (BytesArrayEqualityComparer.Instance.Equals(drawingInfoFromDsShape.ShortBytesSHA512Hash,
                            kvp.Value.ShortBytesSHA512Hash))
                        {
                            existingDsShapeDrawingInfo = kvp.Value;
                            if (complexDsShape.DsShapeDrawingName != existingDsShapeDrawingInfo.Name)
                            {
                                complexDsShape.DsShapeDrawingName = existingDsShapeDrawingInfo.Name;
                                drawingChanged = true;
                            }

                            break;
                        }

                    if (existingDsShapeDrawingInfo is not null)
                    {
                        dsShapeDrawingFromDsShape.Dispose();
                        continue;
                    }

                    // Search #3                
                    foreach (var kvp in allComplexDsShapesDrawingInfos.Where(
                        k => k.Value.ShortBytesLength == drawingInfoFromDsShape.ShortBytesLength))
                    {
                        var dsShapeDrawingWithSameShortBytesLength =
                            DsProject.ReadDrawing(kvp.Value.FileInfo, false, true)
                                as
                                DsShapeDrawing;
                        if (dsShapeDrawingWithSameShortBytesLength is null) continue;
                        dsShapeDrawingWithSameShortBytesLength.CropUnusedSpace();
                        dsShapeDrawingWithSameShortBytesLength.TransformDsShapes(
                            dsShapeDrawingFromDsShape.Width /
                            dsShapeDrawingWithSameShortBytesLength.Width,
                            dsShapeDrawingFromDsShape.Height /
                            dsShapeDrawingWithSameShortBytesLength.Height);
                        dsShapeDrawingWithSameShortBytesLength.CropUnusedSpace();

                        if (BytesArrayEqualityComparer.Instance.Equals(drawingInfoFromDsShape.ShortBytesSHA512Hash,
                            ((DsShapeDrawingInfo)
                                dsShapeDrawingWithSameShortBytesLength.GetDrawingInfo()).ShortBytesSHA512Hash))
                        {
                            dsShapeDrawingWithSameShortBytesLength.Dispose();
                            existingDsShapeDrawingInfo = kvp.Value;

                            dsShapeDrawingWithSameShortBytesLength =
                                DsProject.ReadDrawing(kvp.Value.FileInfo, false, true)
                                    as
                                    DsShapeDrawing;
                            if (dsShapeDrawingWithSameShortBytesLength is null) continue;
                            dsShapeDrawingWithSameShortBytesLength.CropUnusedSpace();
                            complexDsShape.CropUnusedSpace();
                            complexDsShape.PrepareComplexDsShapeGeometry(
                                dsShapeDrawingWithSameShortBytesLength);
                            dsShapeDrawingWithSameShortBytesLength.FillInComplexDsShape(complexDsShape);
                            dsShapeDrawingWithSameShortBytesLength.Dispose();
                            drawingChanged = true;
                            break;
                        }

                        dsShapeDrawingWithSameShortBytesLength.Dispose();
                    }

                    dsShapeDrawingFromDsShape.Dispose();
                    if (existingDsShapeDrawingInfo is not null) continue;
                }

                for (var i = 1;; i += 1)
                {
                    string newDsShapeDrawingName;
                    if (i < 2) newDsShapeDrawingName = dsShapeDrawingName;
                    else newDsShapeDrawingName = dsShapeDrawingName + @"_" + i;

                    if (allComplexDsShapesDrawingInfos.ContainsKey(newDsShapeDrawingName)) continue;

                    DsShapeDrawing newDrawing =
                        dsProject.NewDsShapeDrawingObject(newDsShapeDrawingName, true,
                            complexDsShape);

                    dsProject.SaveUnconditionally(newDrawing, DsProject.IfFileExistsActions.CreateBackup, false);
                    newDrawings[newDrawing.Name] = newDrawing;

#if DEBUG
                    using (var memoryStream = new MemoryStream(1024 * 1024))
                    {
                        using (var writer = new SerializationWriter(memoryStream))
                        {
                            newDrawing.SerializeOwnedData(writer, SerializationContext.ShortBytes);
                        }

                        using (FileStream fileStream = File.Create(newDrawing.FileFullName + ".hash"))
                        {
                            memoryStream.WriteTo(fileStream);
                        }
                    }
#endif

                    if (complexDsShape.DsShapeDrawingName != newDsShapeDrawingName)
                    {
                        complexDsShape.DsShapeDrawingName = newDsShapeDrawingName;
                        drawingChanged = true;
                    }

                    break;
                }
            }

            return drawingChanged;
        }

        #endregion
    }
}