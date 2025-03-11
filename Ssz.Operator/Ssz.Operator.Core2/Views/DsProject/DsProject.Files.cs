using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Properties;

using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Utils.Wpf;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;
using Ssz.Operator.Core.CustomExceptions;
using Microsoft.Extensions.FileProviders;

namespace Ssz.Operator.Core
{
    public partial class DsProject
    {
        #region public functions

        public const string DsShapeFileExtension = @".dsShape";

        public const string DsShapeFileExtensionUpper = @".DSSHAPE";

        public const string DsPageFileExtension = @".dsPage";

        public const string DsPageFileExtensionUpper = @".DSPAGE";

        public const string DsProjectFileExtension = @".dsProject";

        public static bool StreamExists(string? fileFullName, IFileProvider? fileProvider = null)
        {
            if (String.IsNullOrEmpty(fileFullName)) 
                return false;

            if (fileProvider is null)                
                return File.Exists(fileFullName);
            else
                return fileProvider.GetFileInfo(fileFullName).Exists;             
        }

        /// <summary>
        ///     Returns Stream with CanSeek == true
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        public static async Task<Stream?> GetStreamAsync(string? fileFullName)
        {
            if (string.IsNullOrEmpty(fileFullName))
            {
                LoggersSet.Logger.LogDebug("File full name is empty.");
                return null;
            }

            if (Instance.FileProvider is not null)
            {
                try
                {
                    var readStream = await ((IFileInfoEx)Instance.FileProvider.GetFileInfo(fileFullName)).CreateReadStreamAsync();
                    if (readStream.CanSeek)
                        return readStream;

                    var stream = new MemoryStream();
                    readStream.CopyTo(stream);
                    readStream.Dispose();
                    stream.Position = 0;
                    return stream;
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "Application.GetRemoteStream() Exception.");
                    return null;
                }
            }
            else
            {
                try
                {
                    if (!File.Exists(fileFullName))
                    {
                        LoggersSet.Logger.LogDebug("File doesn't exist. " + fileFullName);
                        return null;
                    }

                    return new MemoryStream(File.ReadAllBytes(fileFullName));
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "File.OpenRead() Exception. " + fileFullName);
                    return null;
                }                
            }
        }

        public static void DeleteUnusedFiles(DirectoryInfo? dsPagesDirectoryInfo, DirectoryInfo? dsShapesDirectoryInfo)
        {
            if (Instance.IsReadOnly || !(dsPagesDirectoryInfo?.Exists == true)) 
                return;
            
            try
            {                
                var usedDirs = new List<string>();                
                foreach (FileInfo fi in dsPagesDirectoryInfo.GetFiles(@"*" + DsPageFileExtension,
                    SearchOption.TopDirectoryOnly))
                    usedDirs.Add(DrawingBase.GetDrawingFilesDirectoryFullName(fi.FullName));
                if (dsShapesDirectoryInfo is not null)
                    foreach (FileInfo fi in dsShapesDirectoryInfo.GetFiles(@"*" + DsShapeFileExtension,
                        SearchOption.TopDirectoryOnly))
                        usedDirs.Add(DrawingBase.GetDrawingFilesDirectoryFullName(fi.FullName));

                var allDirs = new CaseInsensitiveHashSet();
                foreach (
                    DirectoryInfo di in
                    dsPagesDirectoryInfo.GetDirectories(@"*" + DsPageFileExtension + "_files", SearchOption.TopDirectoryOnly))
                    allDirs.Add(di.FullName);
                if (dsShapesDirectoryInfo is not null)
                    foreach (
                        DirectoryInfo di in
                        dsShapesDirectoryInfo.GetDirectories(@"*" + DsShapeFileExtension + @"_files", SearchOption.TopDirectoryOnly))
                        allDirs.Add(di.FullName);

                foreach (string usedDir in usedDirs) allDirs.Remove(usedDir);

                foreach (string unUsedDir in allDirs)
                    try
                    {
                        Directory.Delete(unUsedDir, true);
                    }
                    catch (Exception)
                    {
                    }
            }
            catch (Exception)
            {
            }
        }

        public static async Task<DrawingBase?> ReadDrawingAsync(string? drawingFileFullName, bool visualDesignMode, bool loadXamlContent)
        {
            if (String.IsNullOrEmpty(drawingFileFullName))
                return null;
            var isDrawing = DrawingBase.IsDsPageFile(drawingFileFullName) ||
                            DrawingBase.IsDsShapeFile(drawingFileFullName);
            if (!isDrawing) 
                return null;

            DrawingBase? drawing = null;
            try
            {
                using (Stream? stream = await GetStreamAsync(drawingFileFullName))
                {
                    if (stream is null) 
                        return null;
                    var streamInfo = DrawingBase.GetStreamInfo(stream);

                    if (streamInfo != -1) // Not XML
                    {
                        DrawingInfo? drawingInfo = null;
                        if (DrawingBase.IsDsPageFile(drawingFileFullName))
                        {
                            drawingInfo = new DsPageDrawingInfo(drawingFileFullName);
                            drawing = new DsPageDrawing(visualDesignMode, loadXamlContent);
                        }
                        else if (DrawingBase.IsDsShapeFile(drawingFileFullName))
                        {
                            drawingInfo = new DsShapeDrawingInfo(drawingFileFullName);
                            drawing = new DsShapeDrawing(visualDesignMode, loadXamlContent);
                        }
                        else
                        {
                            return null;
                        }

                        if (streamInfo == 0 || streamInfo == 1) throw new Exception("Unsupported old file format.");

                        using (var reader = new SerializationReader(stream))
                        {
                            drawingInfo.DeserializeOwnedData(reader, null);
                        }

                        drawing.SetDrawingInfo(drawingInfo);
                        using (var reader = new SerializationReader(stream))
                        {
                            drawing.DeserializeOwnedData(reader, null);
                        }
                    }
                    else
                    {
                        using (var xmlTextReader = new XmlTextReader(stream))
                        {
                            xmlTextReader.MoveToContent();
                            if (xmlTextReader.NodeType ==
                                XmlNodeType.EndElement)
                                throw new Exception(@"XAML format error.");
                            if (xmlTextReader.NodeType !=
                                XmlNodeType.Element) throw new Exception(@"XAML format error.");
                            if (xmlTextReader.Name !=
                                "DsProject") throw new Exception(@"XAML format error.");
                            while (xmlTextReader.Read())
                            {
                                if (xmlTextReader.NodeType ==
                                    XmlNodeType.EndElement)
                                    break;
                                if (xmlTextReader.NodeType !=
                                    XmlNodeType.Element)
                                    continue;
                                if (xmlTextReader.Name ==
                                    "DsPageDrawing" || xmlTextReader.Name ==
                                    "DsShapeDrawing")
                                {
                                    string xaml = xmlTextReader.ReadOuterXml();

                                    xaml = PrepareObsoleteXaml(xaml);                                    

                                    drawing = XamlHelper.Load(xaml) as DrawingBase;

                                    if (drawing is not null)
                                    {
                                        drawing.FileFullName = drawingFileFullName;

                                        // Need NOT convert XAML to latest format.
                                        if (drawing.SerializationVersionDateTime >
                                            new DateTime(2017, 06, 22, 17, 10, 00) &&
                                            drawing.SerializationVersionDateTime <
                                            DrawingBase.CurrentSerializationVersionDateTime)
                                            drawing.SerializationVersionDateTime =
                                                DrawingBase.CurrentSerializationVersionDateTime;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                drawing = null;
                LoggersSet.Logger.LogError(ex, drawingFileFullName + @": " + Resources.DrawingFileNotFoundMessage);
            }
            catch (BlockUnsupportedVersionException ex)
            {
                drawing = null;
                LoggersSet.Logger.LogError(ex, drawingFileFullName + @": " + Resources.DrawingFileOpenErrorUnsupportedVersion);
            }
            catch (Exception ex)
            {
                drawing = null;
                LoggersSet.Logger.LogError(ex, drawingFileFullName + @": " + Resources.DrawingFileOpenErrorGeneral);
            }

            if (drawing is not null)
            {
                drawing.ParentItem = Instance;

                drawing.SetDataUnchanged();
            }

            return drawing;
        }

        public static DsPageDrawing? ReadDsPageInPlay(Stream? stream, string dsPageFileFullName,
            IDsContainer? parentContainer, IPlayWindowBase? playWindow)
        {
            if (stream is null) 
                return null;

            if (!stream.CanSeek) 
                throw new ArgumentException(@"stream must be seekable (CanSeek is true)");

            DsPageDrawing? dsPageDrawing = null;

            try
            {
                if (stream is null) 
                    return null;
                var streamInfo = DrawingBase.GetStreamInfo(stream);

                if (streamInfo != -1) // Not XML
                {
                    var drawingInfo = new DsPageDrawingInfo(dsPageFileFullName);
                    dsPageDrawing = new DsPageDrawing(false, true);

                    if (streamInfo == 0 || streamInfo == 1) throw new Exception("Unsupported old file format.");

                    using (var reader = new SerializationReader(stream))
                    {
                        drawingInfo.DeserializeOwnedData(reader, null);
                    }

                    dsPageDrawing.SetDrawingInfo(drawingInfo);
                    using (var reader = new SerializationReader(stream))
                    {
                        dsPageDrawing.DeserializeOwnedData(reader, null);
                    }
                }
                else
                {
                    using (var xmlTextReader = new XmlTextReader(stream))
                    {
                        xmlTextReader.MoveToContent();
                        if (xmlTextReader.NodeType ==
                            XmlNodeType.EndElement)
                            throw new Exception(@"XAML format error.");
                        if (xmlTextReader.NodeType !=
                            XmlNodeType.Element) throw new Exception(@"XAML format error.");
                        if (xmlTextReader.Name !=
                            "DsProject") throw new Exception(@"XAML format error.");
                        while (xmlTextReader.Read())
                        {
                            if (xmlTextReader.NodeType ==
                                XmlNodeType.EndElement)
                                break;
                            if (xmlTextReader.NodeType !=
                                XmlNodeType.Element)
                                continue;
                            if (xmlTextReader.Name ==
                                "DsPageDrawing")
                            {
                                string xaml =
                                    xmlTextReader.ReadOuterXml();

                                xaml = PrepareObsoleteXaml(xaml);

                                dsPageDrawing = XamlHelper.Load(xaml) as DsPageDrawing;

                                if (dsPageDrawing is not null) dsPageDrawing.FileFullName = dsPageFileFullName;
                            }
                        }
                    }
                }
            }
            catch (BlockUnsupportedVersionException ex)
            {
                dsPageDrawing = null;
                LoggersSet.Logger.LogError(ex, Resources.DrawingFileOpenErrorUnsupportedVersion);
            }
            catch (Exception ex)
            {
                dsPageDrawing = null;
                LoggersSet.Logger.LogError(ex, Resources.DrawingFileOpenErrorGeneral);
            }

            if (dsPageDrawing is not null)
            {
                dsPageDrawing.ParentItem = parentContainer ?? Instance;
                dsPageDrawing.PlayWindow = playWindow;
            }

            return dsPageDrawing;
        }

        public static async Task<DrawingInfo?> ReadDrawingInfoAsync(string? drawingFileFullName, bool readOnlyDrawingGuid,
            List<string>? errorMessages = null)
        {
            if (String.IsNullOrEmpty(drawingFileFullName))
                return null;

            var isDsPageDrawing = DrawingBase.IsDsPageFile(drawingFileFullName);
            var isDsShapeDrawing = DrawingBase.IsDsShapeFile(drawingFileFullName);
            if (!isDsPageDrawing && !isDsShapeDrawing)
                return null;

            try
            {
                if (readOnlyDrawingGuid)
                {                    
                    byte[] buffer = new byte[100];
                    if (Instance.FileProvider is not null)
                    {
                        using (Stream fs = await ((IndexedDBFile)Instance.FileProvider.GetFileInfo(drawingFileFullName)).CreateReadStreamAsync())
                        {
                            fs.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
                        }                        
                    }
                    else
                    {
                        using (FileStream fs = new(drawingFileFullName, FileMode.Open, FileAccess.Read))
                        {
                            fs.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
                        }
                    }                    

                    if (BitConverter.ToInt32(buffer, 0) == 6)
                    {
                        DrawingInfo? drawingInfo = null;
                        if (isDsPageDrawing) 
                            drawingInfo = new DsPageDrawingInfo(drawingFileFullName);
                        if (isDsShapeDrawing) 
                            drawingInfo = new DsShapeDrawingInfo(drawingFileFullName);
                        if (drawingInfo is null) 
                            throw new InvalidOperationException();

                        using (var reader = new SerializationReader(buffer))
                        {
                            drawingInfo.DeserializeGuidOnly(reader);
                        }

                        return drawingInfo;
                    }
                }

                using (Stream? stream = await GetStreamAsync(drawingFileFullName))
                {
                    if (stream is null)
                        return null;

                    var streamInfo = DrawingBase.GetStreamInfo(stream);

                    if (streamInfo != -1) // Not XML
                    {
                        if (streamInfo == 0 || streamInfo == 1)
                        {
                            throw new Exception("Unsupported old file format.");
                        }

                        DrawingInfo? drawingInfo = null;
                        if (isDsPageDrawing)
                            drawingInfo = new DsPageDrawingInfo(drawingFileFullName);
                        else if (isDsShapeDrawing)
                            drawingInfo = new DsShapeDrawingInfo(drawingFileFullName);
                        else
                            return null;

                        using (var reader = new SerializationReader(stream))
                        {
                            if (readOnlyDrawingGuid)
                                drawingInfo.DeserializeGuidOnly(reader);
                            else
                                drawingInfo.DeserializeOwnedData(reader, null);
                        }

                        return drawingInfo;
                    }

                    var deserializedVersionDateTime = DateTime.MinValue;
                    var guid = Guid.Empty;
                    string desc = "";
                    string group = "";
                    DsConstant[]? dsConstantsCollection = null;
                    var styleInfo = new GuidAndName
                    {
                        Guid = GenericFaceplateDsPageType.TypeGuid
                    };
                    DsPageTypeBase? styleObject = null;
                    var mark = 0;
                    byte[]? previewImageBytes = null;
                    var usedAddonsInfo = new List<GuidAndName>();
                    bool excludeFromTagSearch = false;

                    using (var xmlTextReader = new XmlTextReader(stream))
                    {
                        xmlTextReader.MoveToContent();
                        if (xmlTextReader.NodeType ==
                            XmlNodeType.EndElement)
                            return null;
                        if (xmlTextReader.NodeType !=
                            XmlNodeType.Element) return null;
                        if (xmlTextReader.Name !=
                            "DsProject") return null;
                        while (xmlTextReader.Read())
                        {
                            if (xmlTextReader.NodeType ==
                                XmlNodeType.EndElement)
                                break;
                            if (xmlTextReader.NodeType !=
                                XmlNodeType.Element)
                                continue;
                            if (xmlTextReader.Name ==
                                "DsPageDrawing" || xmlTextReader.Name ==
                                "DsShapeDrawing")
                            {
                                if (xmlTextReader.MoveToAttribute("SerializationVersionDateTime"))
                                {
                                    xmlTextReader.ReadAttributeValue();
                                    deserializedVersionDateTime = DateTime.Parse(xmlTextReader.Value);

                                    if (deserializedVersionDateTime > DrawingBase.CurrentSerializationVersionDateTime)
                                    {
                                        string message = Path.GetFileName(drawingFileFullName) + @": " +
                                                         Resources.DrawingFileOpenErrorUnsupportedVersion;
                                        LoggersSet.Logger.LogCritical(message);
                                        if (errorMessages is not null) errorMessages.Add(message);
                                    }
                                }

                                // Need NOT convert XAML to latest format, if not too old.
                                if (deserializedVersionDateTime
                                    >= new DateTime(2018, 12, 22, 00, 00, 00) &&
                                    deserializedVersionDateTime < DrawingBase.CurrentSerializationVersionDateTime)
                                    deserializedVersionDateTime = DrawingBase.CurrentSerializationVersionDateTime;

                                if (readOnlyDrawingGuid)
                                {
                                    if (xmlTextReader.MoveToAttribute("Guid"))
                                    {
                                        xmlTextReader.ReadAttributeValue();
                                        guid = Guid.Parse(xmlTextReader.Value);
                                    }
                                }
                                else
                                {
                                    xmlTextReader.MoveToElement();

                                    string xaml =
                                        xmlTextReader.ReadOuterXml();

                                    xaml = PrepareObsoleteXaml(xaml);

                                    var drawing = XamlHelper.Load(xaml) as DrawingBase;
                                    if (drawing is null) 
                                        return null;

                                    drawing.FileFullName = drawingFileFullName;

                                    guid = drawing.Guid;
                                    desc = drawing.Desc;
                                    group = drawing.Group;
                                    dsConstantsCollection = drawing.DsConstantsCollection.ToArray();
                                    var dsPageDrawing = drawing as DsPageDrawing;
                                    if (dsPageDrawing is not null)
                                    {
                                        styleInfo.Guid = dsPageDrawing.DsPageTypeGuid;
                                        styleObject = dsPageDrawing.DsPageTypeObject;
                                        excludeFromTagSearch = dsPageDrawing.ExcludeFromTagSearch;
                                    }

                                    previewImageBytes = drawing.PreviewImageBytes;
                                    mark = drawing.Mark;                                    

                                    usedAddonsInfo = AddonsHelper.GetAddonsInfo(drawing.GetUsedAddonGuids());

                                    string[] unSupportedAddonsNameToDisplays =
                                        AddonsHelper.GetNotInAddonsCollection(usedAddonsInfo);
                                    if (unSupportedAddonsNameToDisplays.Length > 0)
                                    {
                                        string message = Path.GetFileName(drawingFileFullName) + @": " +
                                                         Resources.DrawingFileOpenErrorAddonIsNotFound + @" ";
                                        foreach (
                                            string unSupportedAddonNameToDisplay in unSupportedAddonsNameToDisplays)
                                            message += unSupportedAddonNameToDisplay + @"; ";
                                        LoggersSet.Logger.LogCritical(message);
                                        if (errorMessages is not null) errorMessages.Add(message);
                                    }
                                }
                            }
                        }
                    }

                    if (!readOnlyDrawingGuid)
                    {
                        // Refresh Style NameToDisplay
                        var styleDispalyName = AddonsHelper.GetDsPageTypeName(styleInfo.Guid);
                        if (styleDispalyName is not null) styleInfo.Name = styleDispalyName;
                    }

                    if (isDsPageDrawing)
                        return new DsPageDrawingInfo(drawingFileFullName, guid, desc, group, previewImageBytes,
                            deserializedVersionDateTime,
                            dsConstantsCollection is not null ? dsConstantsCollection.ToArray() : new DsConstant[0], 
                            mark,
                            usedAddonsInfo,
                            excludeFromTagSearch,
                            styleInfo,
                            styleObject);
                    if (isDsShapeDrawing)
                        return new DsShapeDrawingInfo(drawingFileFullName, guid, desc, group, previewImageBytes,
                            deserializedVersionDateTime,
                            dsConstantsCollection is not null ? dsConstantsCollection.ToArray() : new DsConstant[0], 
                            mark,
                            usedAddonsInfo, 
                            new byte[64],
                            0);
                }

                return null;
            }
            catch (BlockUnsupportedVersionException ex)
            {
                string message = Path.GetFileName(drawingFileFullName) + @": " + Resources.DrawingFileOpenErrorUnsupportedVersion;
                LoggersSet.Logger.LogCritical(ex, message);
                if (errorMessages is not null) errorMessages.Add(message + @" " + Resources.SeeErrorLogForDetails);
                return null;
            }
            catch (Exception ex)
            {
                string message = Path.GetFileName(drawingFileFullName) + @": " + Resources.DrawingFileOpenErrorGeneral;
                LoggersSet.Logger.LogCritical(ex, message);
                if (errorMessages is not null) errorMessages.Add(message + @" " + Resources.SeeErrorLogForDetails);
                return null;
            }
        }

        private static string PrepareObsoleteXaml(string drawingXml)
        {
            // Obsolete
            drawingXml = drawingXml.Replace(@"Ssz.Operator.Core.DataBinding", @"Ssz.Operator.Core");
            drawingXml = drawingXml.Replace(@"Ssz.Operator.Core.DataItem", @"Ssz.Operator.Core");

            return drawingXml;
        }

        public async Task<DsPageDrawing?> ReadDsPageInPlayAsync(string? dsPageFileRelativePath,
            IDsContainer? parentContainer, IPlayWindowBase? playWindow)
        {
            if (string.IsNullOrEmpty(dsPageFileRelativePath) ||
                !StringHelper.EndsWithIgnoreCase(dsPageFileRelativePath, DsPageFileExtension)) return null;

            if (parentContainer is null) 
                parentContainer = Instance;

            string dsPageFileFullName = Path.Combine(DsPagesDirectoryFullName, dsPageFileRelativePath!);
            var stream = await GetStreamAsync(dsPageFileFullName);
            if (stream is null) 
                return null;
            try
            {
                return ReadDsPageInPlay(stream, dsPageFileFullName, parentContainer, playWindow);
            }
            finally
            {
                stream.Dispose();
            }
        }

        public async Task<ToolkitOperationResult> ImportFromXamlAsync(string xamlFileName,
            IProgressInfo progressInfo)
        {
            if (!IsInitialized || IsReadOnly || FileProvider is not null) 
                return ToolkitOperationResult.DoneWithErrors;

            var result = ToolkitOperationResult.Done;

            await Task.Delay(0);

            var dsPageAdded = false;
            var complexDsShapeAdded = false;

            using (var xmlTextReader = new XmlTextReader(File.OpenRead(xamlFileName)))
            {
                xmlTextReader.MoveToContent();
                if (xmlTextReader.NodeType == XmlNodeType.EndElement) 
                    return ToolkitOperationResult.DoneWithErrors;
                if (xmlTextReader.NodeType != XmlNodeType.Element) 
                    return ToolkitOperationResult.DoneWithErrors;
                if (xmlTextReader.Name != "DsProject") 
                    return ToolkitOperationResult.DoneWithErrors;
                        
                while (xmlTextReader.Read())
                {
                    if (xmlTextReader.NodeType == XmlNodeType.EndElement) break;
                    if (xmlTextReader.NodeType != XmlNodeType.Element) continue;
                    if (xmlTextReader.Name == "DsPageDrawing" || xmlTextReader.Name == "DsShapeDrawing")
                    {
                        //await Dispatcher.Yield(DispatcherPriority.Normal);
                        try
                        {
                            string drawingXml = xmlTextReader.ReadOuterXml();
                            var drawing = XamlHelper.Load(drawingXml) as DrawingBase;
                            if (drawing is DsPageDrawing)
                            {
                                dsPageAdded = true;
                                drawing.FileFullName = Path.Combine(DsPagesDirectoryFullName, drawing.Name + DsPageFileExtension);
                            }
                            else if (drawing is DsShapeDrawing)
                            {
                                complexDsShapeAdded = true;
                                drawing.FileFullName = Path.Combine(DsShapesDirectoryFullName, drawing.Name + DsShapeFileExtension);
                            }

                            await SaveUnconditionallyAsync(drawing, IfFileExistsActions.CreateBackup);
                        }
                        catch (Exception ex)
                        {
                            result = ToolkitOperationResult.DoneWithErrors;
                            LoggersSet.Logger.LogError(ex.Message);
                        }
                    }
                }
            }

            if (dsPageAdded) OnDsPageDrawingsListChanged();
            if (complexDsShapeAdded) OnDsShapeDrawingsListChanged();

            return result;
        }

        public async Task<ToolkitOperationResult> ExportToXamlAsync(string xamlFileName,
            List<DrawingInfo> drawingInfos, IProgressInfo progressInfo)
        {
            if (drawingInfos is null) 
                return ToolkitOperationResult.DoneWithErrors;
            if (!IsInitialized || FileProvider is not null) 
                return ToolkitOperationResult.DoneWithErrors;

            using (var xmlTextWriter = new XmlTextWriter(File.Create(xamlFileName), Encoding.UTF8))
            {
                xmlTextWriter.Formatting =
                    Formatting.Indented;
                xmlTextWriter.WriteStartDocument();
                xmlTextWriter.WriteStartElement(
                    "DsProject");

                var i = 0;
                var count = drawingInfos.Count;
                var cancellationToken = progressInfo is not null
                    ? progressInfo.GetCancellationToken()
                    : new CancellationToken();
                foreach (DrawingInfo drawingInfo in drawingInfos)
                {
                    i += 1;
                    if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;
                    if (progressInfo is not null) await progressInfo.RefreshProgressBarAsync(i, count);

                    var drawing = await ReadDrawingAsync(drawingInfo.FileFullName, false, true);

                    if (drawing is not null)
                        XamlHelper.Save(drawing,
                            xmlTextWriter);
                }

                xmlTextWriter.WriteEndElement();
                xmlTextWriter.WriteEndDocument();
                xmlTextWriter.Close();
            }

            return ToolkitOperationResult.Done;
        }

        /// <summary>
        ///     path relative to Pages directory.
        /// </summary>
        /// <param name="fileRelativePath"></param>
        /// <returns></returns>
        public string? GetExistingDsPageFileFullNameOrNull(string? fileRelativePath)
        {
            if (!IsInitialized || String.IsNullOrWhiteSpace(fileRelativePath)) 
                return null;

            string? fileFullName = Path.Combine(DsPagesDirectoryFullName, fileRelativePath);

            if (FileProvider is not null)
            {
                if (!FileProvider.GetFileInfo(fileFullName).Exists)
                    fileFullName = null;                
            }
            else
            {
                if (!File.Exists(fileFullName))
                    fileFullName = null;
            }

            return fileFullName;
        }

        /// <summary>
        ///     path relative to Sahpes directory.
        /// </summary>
        /// <param name="fileRelativePath"></param>
        /// <returns></returns>
        public string? GetExistingDsShapeFileFullNameOrNull(string? fileRelativePath)
        {
            if (!IsInitialized || String.IsNullOrWhiteSpace(fileRelativePath))
                return null;

            string? fileFullName = Path.Combine(DsShapesDirectoryFullName, fileRelativePath);

            if (FileProvider is not null)
            {
                if (!FileProvider.GetFileInfo(fileFullName).Exists)
                    fileFullName = null;
            }
            else
            {
                if (!File.Exists(fileFullName))
                    fileFullName = null;                
            }

            return fileFullName;
        }

        /// <summary>
        ///     returns path relative to dsPages directory.
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        public string GetFileRelativePath(string? fileFullName)
        {
            if (!IsInitialized || String.IsNullOrEmpty(fileFullName)) 
                return "";

            var dsPagesDirectoryFullName = DsPagesDirectoryFullName;            
            if (StringHelper.StartsWithIgnoreCase(fileFullName, dsPagesDirectoryFullName + Path.DirectorySeparatorChar))
                return fileFullName.Substring(dsPagesDirectoryFullName.Length + 1);
            return "";
        }        

        public async Task DrawingCopyAsync(FileInfo sourceDrawingFileInfo, FileInfo destinationDrawingFileInfo)
        {
            if (destinationDrawingFileInfo.Exists) 
                throw new ArgumentException();

            File.Copy(sourceDrawingFileInfo.FullName,
                destinationDrawingFileInfo.FullName, false);

            string drawingFilesDir = DrawingBase.GetDrawingFilesDirectoryFullName(sourceDrawingFileInfo.FullName);
            if (Directory.Exists(drawingFilesDir))
            {
                string destinationDrawingFilesDir =
                    DrawingBase.GetDrawingFilesDirectoryFullName(destinationDrawingFileInfo.FullName);
                if (Directory.Exists(destinationDrawingFilesDir))
                    try
                    {
                        Directory.Delete(destinationDrawingFilesDir, true);
                    }
                    catch (Exception)
                    {
                    }

                Directory.CreateDirectory(destinationDrawingFilesDir);
                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(drawingFilesDir, "*",
                    SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(drawingFilesDir, destinationDrawingFilesDir));

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(drawingFilesDir, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace(drawingFilesDir, destinationDrawingFilesDir), true);
            }

            if (_allDsPagesCache is not null &&
                DrawingBase.IsDsPageFile(destinationDrawingFileInfo.FullName) &&
                FileSystemHelper.Compare(destinationDrawingFileInfo.Directory?.FullName, DsPagesDirectoryFullName))
            {
                var dsPageDrawing = await ReadDrawingAsync(destinationDrawingFileInfo.FullName, false, false) as DsPageDrawing;
                if (dsPageDrawing is not null)
                {
                    DsPageDrawing cacheDsPageDrawing = PrepareForCache(dsPageDrawing);
                    dsPageDrawing.Dispose();
                    _allDsPagesCache[dsPageDrawing.Name] = cacheDsPageDrawing;
                    _allDsPagesCacheIsChanged = true;
                }
            }
            else if (_allDsShapeDrawingInfos is not null &&
                     DrawingBase.IsDsShapeFile(destinationDrawingFileInfo.FullName) &&
                     FileSystemHelper.Compare(destinationDrawingFileInfo.Directory?.FullName, DsShapesDirectoryFullName))
            {
                var dsShapeDrawingInfo =
                    await ReadDrawingInfoAsync(destinationDrawingFileInfo.FullName, false) as DsShapeDrawingInfo;
                if (dsShapeDrawingInfo is not null)
                    _allDsShapeDrawingInfos[dsShapeDrawingInfo.Name] = dsShapeDrawingInfo;
            }
        }

        public void DrawingDelete(FileInfo drawingFileInfo)
        {
            if (drawingFileInfo.Exists) 
                File.Delete(drawingFileInfo.FullName);

            string drawingFilesDirectoryFullName = DrawingBase.GetDrawingFilesDirectoryFullName(drawingFileInfo.FullName);
            if (Directory.Exists(drawingFilesDirectoryFullName))
                try
                {
                    Directory.Delete(drawingFilesDirectoryFullName, true);
                }
                catch (Exception)
                {
                }

            if (_allDsPagesCache is not null &&
                DrawingBase.IsDsPageFile(drawingFileInfo.FullName) &&
                FileSystemHelper.Compare(drawingFileInfo.Directory?.FullName, DsPagesDirectoryFullName))
            {
                _allDsPagesCache.Remove(Path.GetFileNameWithoutExtension(drawingFileInfo.Name));
                _allDsPagesCacheIsChanged = true;
            }
            else if (_allDsShapeDrawingInfos is not null &&
                     DrawingBase.IsDsShapeFile(drawingFileInfo.FullName) &&
                     FileSystemHelper.Compare(drawingFileInfo.Directory?.FullName, DsShapesDirectoryFullName))
            {
                _allDsShapeDrawingInfos.Remove(Path.GetFileNameWithoutExtension(drawingFileInfo.Name));
            }
        }

        public async Task<bool> SaveUnconditionallyAsync(DrawingBase? drawing, IfFileExistsActions ifChangedOutsideSaveType,
            bool generateNewGuid = true, List<string>? errorMessages = null)
        {
            if (drawing is null) 
                return false;
            if (!IsInitialized || FileProvider is not null) 
                return false;

            if (IsReadOnly)
            {
                LoggersSet.Logger.LogWarning("Drawing cannot be saved in ReadOnly mode" + drawing.FileFullName);
                return false;
            }

            drawing.BeforeDrawingSave();

            if (File.Exists(drawing.FileFullName))
            {
                var onDriveDrawingInfo = await ReadDrawingInfoAsync(drawing.FileFullName, true);
                if (onDriveDrawingInfo is not null)
                    switch (ifChangedOutsideSaveType)
                    {
                        case IfFileExistsActions.AskNewFileName:
                            if (onDriveDrawingInfo.Guid != drawing.Guid)
                            {
                                var cancelled = AskAndSetNewFileName(drawing,
                                    Path.GetFileName(drawing.FileFullName));
                                if (cancelled) return true;
                            }

                            break;
                        case IfFileExistsActions.CreateBackup:
                            try
                            {
                                File.Copy(drawing.FileFullName, drawing.FileFullName + ".backup", true);
                            }
                            catch
                            {
                            }

                            break;
                        case IfFileExistsActions.CreateBackupAndWarn:
                            try
                            {
                                File.Copy(drawing.FileFullName, drawing.FileFullName + ".backup", true);
                            }
                            catch
                            {
                            }

                            if (onDriveDrawingInfo.Guid != drawing.Guid)
                                MessageBoxHelper.ShowInfo(Resources.BackupDrawingWasCreated + " " +
                                                          drawing.FileFullName +
                                                          ".backup");
                            break;
                    }
            }

            if (generateNewGuid) drawing.Guid = Guid.NewGuid();
            var refresh = drawing is DsShapeDrawing || drawing.IsFaceplate;
            if (refresh) drawing.RefreshDsConstantsCollection();

            try
            {
                var drawingStoreMode = DrawingsStoreMode;
                if (drawing is DsShapeDrawing) drawingStoreMode = DrawingsStoreModeEnum.BinMode;

                switch (drawingStoreMode)
                {
                    case DrawingsStoreModeEnum.BinMode:
                        using (var memoryStream = new MemoryStream(1024 * 1024))
                        {
                            using (var writer = new SerializationWriter(memoryStream))
                            {
                                var drawingInfo = drawing.GetDrawingInfo();
                                drawingInfo.SerializeOwnedData(writer, null);
                            }

                            using (var writer = new SerializationWriter(memoryStream, true))
                            {
                                drawing.SerializeOwnedData(writer, null);
                            }

                            using (FileStream fileStream = File.Create(drawing.FileFullName))
                            {
                                memoryStream.WriteTo(fileStream);
                            }
                        }

                        break;
                    case DrawingsStoreModeEnum.XamlMode:
                        string xamlWithAbsolutePaths;
                        using (var ms = new MemoryStream())
                        using (var xmlTextWriter = new XmlTextWriter(ms, Encoding.UTF8))
                        {
                            xmlTextWriter.Formatting =
                                Formatting.Indented;
                            xmlTextWriter.WriteStartDocument();
                            xmlTextWriter.WriteStartElement(
                                "DsProject");

                            XamlHelper.Save(drawing,
                                xmlTextWriter);

                            xmlTextWriter.WriteEndElement();
                            xmlTextWriter.WriteEndDocument();
                            xmlTextWriter.Flush();

                            ms.Position = 0;
                            using (var reader = new StreamReader(ms, Encoding.UTF8))
                            {
                                xamlWithAbsolutePaths = reader.ReadToEnd();
                            }
                        }

                        string xamlWithRelativePaths = XamlHelper.GetXamlWithRelativePaths(xamlWithAbsolutePaths,
                            drawing.DrawingFilesDirectoryFullName);

                        using (var textWriter = new StreamWriter(File.Create(drawing.FileFullName),
                            Encoding.UTF8))
                        {
                            textWriter.Write(xamlWithRelativePaths);
                        }

                        break;
                }

                var dsPageDrawing = drawing as DsPageDrawing;
                if (_allDsPagesCache is not null && dsPageDrawing is not null &&
                    FileSystemHelper.Compare(Path.GetDirectoryName(dsPageDrawing.FileFullName) ?? @"",
                        DsPagesDirectoryFullName))
                {
                    DsPageDrawing cacheDsPageDrawing = PrepareForCache(dsPageDrawing);
                    _allDsPagesCache[dsPageDrawing.Name] = cacheDsPageDrawing;
                    _allDsPagesCacheIsChanged = true;
                }

                var dsShapeDrawing = drawing as DsShapeDrawing;
                if (_allDsShapeDrawingInfos is not null && dsShapeDrawing is not null &&
                    FileSystemHelper.Compare(Path.GetDirectoryName(dsShapeDrawing.FileFullName),
                        DsShapesDirectoryFullName))
                    _allDsShapeDrawingInfos[dsShapeDrawing.Name] =
                        (DsShapeDrawingInfo) dsShapeDrawing.GetDrawingInfo();

                drawing.SetDataUnchanged();
            }
            catch (UnauthorizedAccessException ex)
            {
                string message = drawing.FileFullName + @": " +
                                 Resources.DrawingSavingUnauthorizedAccessError;
                LoggersSet.Logger.LogError(ex, message);
                if (errorMessages is not null) errorMessages.Add(message + @" " + Resources.SeeErrorLogForDetails);
                return false;
            }
            catch (Exception ex)
            {
                string message = drawing.FileFullName + @": " +
                                 Resources.DrawingSavingError;
                LoggersSet.Logger.LogError(ex, message);
                if (errorMessages is not null) errorMessages.Add(message + @" " + Resources.SeeErrorLogForDetails);
                return false;
            }

            //ExportToXamlAsync(drawing.FileInfo.FullName + ".axaml", drawing);

            return false;
        }

        public bool AskAndSetNewFileName(DrawingBase? drawing, string initialFileName)
        {
            //var dlg = new SaveFileDialog
            //{
            //    FileName = initialFileName
            //};

            //if (drawing is DsPageDrawing)
            //{
            //    var dsPagesDirectoryInfo = DsPagesDirectoryFullName;
            //    if (dsPagesDirectoryInfo is null) return true;

            //    dlg.InitialDirectory = dsPagesDirectoryInfo.FullName;
            //    dlg.Filter = @"Save file (*" + DsPageFileExtension + ")|*" + DsPageFileExtension + "|All files (*.*)|*.*";
            //}
            //else if (drawing is DsShapeDrawing)
            //{
            //    var dsShapesDirectoryInfo = DsShapesDirectoryFullName;
            //    if (dsShapesDirectoryInfo is null) return true;

            //    dlg.InitialDirectory = dsShapesDirectoryInfo.FullName;
            //    dlg.Filter = @"Save file (*" + DsProject.DsShapeFileExtension + ")|*" + DsProject.DsShapeFileExtension + "|All files (*.*)|*.*";
            //}
            //else
            //{
            //    return true;
            //}

            //if (dlg.ShowDialog() != true) return true;

            //if (!StringHelper.StartsWithIgnoreCase(dlg.FileName, DsPagesDirectoryFullName + @"\"))
            //{
            //    MessageBoxHelper.ShowError(Resources.FileMustBeInDsProjectDir);
            //    return true;
            //}

            ///*
            //        if (String.Compare(dlg.FileName, drawing.FileInfo.FullName, true,
            //            CultureInfo.InvariantCulture) == 0)
            //        {
            //            File.Copy(drawing.FileInfo.FullName, drawing.FileInfo.FullName + ".backup", true);
            //            MessageBoxHelper.ShowInfo(Properties.Resources.BackupDrawingWasCreated + " " + drawing.FileInfo.FullName + ".backup");
            //        }*/

            //drawing.FileFullName = new FileInfo(dlg.FileName).FullName;

            return false;
        }        

        #endregion
    }
}