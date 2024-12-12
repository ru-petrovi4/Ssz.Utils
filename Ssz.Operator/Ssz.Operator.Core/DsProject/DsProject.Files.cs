using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;
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
using Ssz.Utils.Wpf;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;

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

        public static Stream? GetStream(string? fileFullName)
        {
            if (string.IsNullOrEmpty(fileFullName))
            {
                DsProject.LoggersSet.Logger.LogDebug("File full name is empty.");
                return null;
            }

            if (Instance.Mode == DsProjectModeEnum.WebPlayMode)
                try
                {
                    StreamResourceInfo streamResourceInfo = Application.GetRemoteStream(new Uri(fileFullName));
                    if (streamResourceInfo is null) return null;

                    if (streamResourceInfo.Stream.CanSeek) return streamResourceInfo.Stream;

                    var stream = new MemoryStream();
                    streamResourceInfo.Stream.CopyTo(stream);
                    streamResourceInfo.Stream.Dispose();
                    stream.Position = 0;
                    return stream;
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, "Application.GetRemoteStream() Exception.");
                    return null;
                }

            try
            {
                if (!File.Exists(fileFullName))
                {
                    DsProject.LoggersSet.Logger.LogDebug("File doesn't exist. " + fileFullName);
                    return null;
                }

                return new MemoryStream(File.ReadAllBytes(fileFullName));
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, "File.OpenRead() Exception. " + fileFullName);
                return null;
            }
        }

        public static void DeleteUnusedFiles(DirectoryInfo? dsPagesDirectoryInfo, DirectoryInfo? dsShapesDirectoryInfo)
        {
            if (dsPagesDirectoryInfo is null) return;
            
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

        public static DrawingBase? ReadDrawing(FileInfo? drawingFileInfo, bool visualDesignMode, bool loadXamlContent)
        {
            if (drawingFileInfo is null) return null;
            var isDrawing = DrawingBase.IsDsPageFile(drawingFileInfo) ||
                            DrawingBase.IsDsShapeFile(drawingFileInfo);
            if (!isDrawing) return null;

            DrawingBase? drawing = null;
            try
            {
                using (var stream = GetStream(drawingFileInfo.FullName))
                {
                    if (stream is null) return null;
                    var streamInfo = DrawingBase.GetStreamInfo(stream);

                    if (streamInfo != -1) // Not XML
                    {
                        DrawingInfo? drawingInfo = null;
                        if (DrawingBase.IsDsPageFile(drawingFileInfo))
                        {
                            drawingInfo = new DsPageDrawingInfo(drawingFileInfo);
                            drawing = new DsPageDrawing(visualDesignMode, loadXamlContent);
                        }
                        else if (DrawingBase.IsDsShapeFile(drawingFileInfo))
                        {
                            drawingInfo = new DsShapeDrawingInfo(drawingFileInfo);
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
                        using (
                            var xmlTextReader =
                                new XmlTextReader(
                                    stream))
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
                                        drawing.FileFullName = drawingFileInfo.FullName;

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
                DsProject.LoggersSet.Logger.LogError(ex, drawingFileInfo.FullName + @": " + Resources.DrawingFileNotFoundMessage);
            }
            catch (BlockUnsupportedVersionException ex)
            {
                drawing = null;
                DsProject.LoggersSet.Logger.LogError(ex, drawingFileInfo.FullName + @": " + Resources.DrawingFileOpenErrorUnsupportedVersion);
            }
            catch (Exception ex)
            {
                drawing = null;
                DsProject.LoggersSet.Logger.LogError(ex, drawingFileInfo.FullName + @": " + Resources.DrawingFileOpenErrorGeneral);
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
            if (stream is null) return null;

            if (!stream.CanSeek) throw new ArgumentException(@"stream must be seekable (CanSeek is true)");

            DsPageDrawing? dsPageDrawing = null;

            try
            {
                if (stream is null) return null;
                var streamInfo = DrawingBase.GetStreamInfo(stream);

                if (streamInfo != -1) // Not XML
                {
                    var drawingInfo = new DsPageDrawingInfo(new FileInfo(dsPageFileFullName));
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
                    using (
                        var xmlTextReader =
                            new XmlTextReader(
                                stream))
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
                DsProject.LoggersSet.Logger.LogError(ex, Resources.DrawingFileOpenErrorUnsupportedVersion);
            }
            catch (Exception ex)
            {
                dsPageDrawing = null;
                DsProject.LoggersSet.Logger.LogError(ex, Resources.DrawingFileOpenErrorGeneral);
            }

            if (dsPageDrawing is not null)
            {
                dsPageDrawing.ParentItem = parentContainer ?? Instance;
                dsPageDrawing.PlayWindow = playWindow;
            }

            return dsPageDrawing;
        }

        public static DrawingInfo? ReadDrawingInfo(FileInfo? drawingFileInfo, bool readOnlyDrawingGuid,
            List<string>? errorMessages = null)
        {
            if (drawingFileInfo is null) return null;

            var isDsPageDrawing = DrawingBase.IsDsPageFile(drawingFileInfo);
            var isDsShapeDrawing = DrawingBase.IsDsShapeFile(drawingFileInfo);
            if (!isDsPageDrawing &&
                !isDsShapeDrawing)
                return null;

            try
            {
                if (readOnlyDrawingGuid)
                {
                    byte[] buffer;
                    using (FileStream fs = new(drawingFileInfo.FullName, FileMode.Open, FileAccess.Read))
                    using (MemoryStream ms = new())
                    {
                        fs.CopyTo(ms);
                        buffer = ms.ToArray();
                    }

                    if (BitConverter.ToInt32(buffer, 0) == 6)
                    {
                        DrawingInfo? drawingInfo = null;
                        if (isDsPageDrawing) drawingInfo = new DsPageDrawingInfo(drawingFileInfo);
                        if (isDsShapeDrawing) drawingInfo = new DsShapeDrawingInfo(drawingFileInfo);
                        if (drawingInfo is null) throw new InvalidOperationException();

                        using (var reader = new SerializationReader(buffer))
                        {
                            drawingInfo.DeserializeGuidOnly(reader);
                        }

                        return drawingInfo;
                    }
                }

                using (FileStream fs = File.OpenRead(drawingFileInfo.FullName))
                {
                    var streamInfo = DrawingBase.GetStreamInfo(fs);

                    if (streamInfo != -1) // Not XML
                    {
                        if (streamInfo == 0 || streamInfo == 1)
                        {
                            throw new Exception("Unsupported old file format.");
                        }

                        DrawingInfo? drawingInfo = null;
                        if (isDsPageDrawing)
                            drawingInfo = new DsPageDrawingInfo(drawingFileInfo);
                        else if (isDsShapeDrawing)
                            drawingInfo = new DsShapeDrawingInfo(drawingFileInfo);
                        else
                            return null;

                        using (var reader = new SerializationReader(fs))
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

                    using (
                        var xmlTextReader =
                            new XmlTextReader(fs))
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
                                        string message = drawingFileInfo.Name + @": " +
                                                         Resources.DrawingFileOpenErrorUnsupportedVersion;
                                        DsProject.LoggersSet.Logger.LogCritical(message);
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

                                    drawing.FileFullName = drawingFileInfo.FullName;

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
                                        string message = drawingFileInfo.Name + @": " +
                                                         Resources.DrawingFileOpenErrorAddonIsNotFound + @" ";
                                        foreach (
                                            string unSupportedAddonNameToDisplay in unSupportedAddonsNameToDisplays)
                                            message += unSupportedAddonNameToDisplay + @"; ";
                                        DsProject.LoggersSet.Logger.LogCritical(message);
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
                        return new DsPageDrawingInfo(drawingFileInfo, guid, desc, group, previewImageBytes,
                            deserializedVersionDateTime,
                            dsConstantsCollection is not null ? dsConstantsCollection.ToArray() : new DsConstant[0], 
                            mark,
                            usedAddonsInfo,
                            excludeFromTagSearch,
                            styleInfo,
                            styleObject);
                    if (isDsShapeDrawing)
                        return new DsShapeDrawingInfo(drawingFileInfo, guid, desc, group, previewImageBytes,
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
                string message = drawingFileInfo.Name + @": " + Resources.DrawingFileOpenErrorUnsupportedVersion;
                DsProject.LoggersSet.Logger.LogCritical(ex, message);
                if (errorMessages is not null) errorMessages.Add(message + @" " + Resources.SeeErrorLogForDetails);
                return null;
            }
            catch (Exception ex)
            {
                string message = drawingFileInfo.Name + @": " + Resources.DrawingFileOpenErrorGeneral;
                DsProject.LoggersSet.Logger.LogCritical(ex, message);
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

        public DsPageDrawing? ReadDsPageInPlay(string? dsPageFileRelativePath,
            IDsContainer? parentContainer, IPlayWindowBase? playWindow)
        {
            if (string.IsNullOrEmpty(dsPageFileRelativePath) ||
                !StringHelper.EndsWithIgnoreCase(dsPageFileRelativePath, DsPageFileExtension)) return null;

            if (parentContainer is null) 
                parentContainer = Instance;

            string dsPageFileFullName = GetFileFullName(dsPageFileRelativePath!);
            var stream = GetStream(dsPageFileFullName);
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
            if (!IsInitialized || IsReadOnly) return ToolkitOperationResult.DoneWithErrors;

            var result = ToolkitOperationResult.Done;

            var dsPageAdded = false;
            var complexDsShapeAdded = false;

            using (var xmlTextReader = new XmlTextReader(File.OpenRead(xamlFileName)))
            {
                xmlTextReader.MoveToContent();
                if (xmlTextReader.NodeType == XmlNodeType.EndElement) return ToolkitOperationResult.DoneWithErrors;
                if (xmlTextReader.NodeType != XmlNodeType.Element) return ToolkitOperationResult.DoneWithErrors;
                if (xmlTextReader.Name != "DsProject") return ToolkitOperationResult.DoneWithErrors;


                var dsShapesDir = DsShapesDirectoryInfo;
                string dsShapesDirFullName = dsShapesDir is null ? string.Empty : dsShapesDir.FullName;
                while (xmlTextReader.Read())
                {
                    if (xmlTextReader.NodeType == XmlNodeType.EndElement) break;
                    if (xmlTextReader.NodeType != XmlNodeType.Element) continue;
                    if (xmlTextReader.Name == "DsPageDrawing" || xmlTextReader.Name == "DsShapeDrawing")
                    {
                        await Dispatcher.Yield(DispatcherPriority.Normal);
                        try
                        {
                            string drawingXml = xmlTextReader.ReadOuterXml();
                            var drawing = XamlHelper.Load(drawingXml) as DrawingBase;
                            if (drawing is DsPageDrawing)
                            {
                                dsPageAdded = true;
                                drawing.FileFullName = DsPagesDirectoryInfo!.FullName + @"\" + drawing.Name + DsPageFileExtension;
                            }
                            else if (drawing is DsShapeDrawing)
                            {
                                complexDsShapeAdded = true;
                                drawing.FileFullName = dsShapesDirFullName + @"\" + drawing.Name + DsProject.DsShapeFileExtension;
                            }

                            SaveUnconditionally(drawing, IfFileExistsActions.CreateBackup);
                        }
                        catch (Exception ex)
                        {
                            result = ToolkitOperationResult.DoneWithErrors;
                            DsProject.LoggersSet.Logger.LogError(ex.Message);
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
            if (drawingInfos is null) return ToolkitOperationResult.DoneWithErrors;
            if (!IsInitialized) return ToolkitOperationResult.DoneWithErrors;

            using (
                var xmlTextWriter =
                    new XmlTextWriter(
                        File.Create(xamlFileName),
                        Encoding.UTF8)
            )
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
                foreach (
                    DrawingInfo drawingInfo in
                    drawingInfos)
                {
                    i += 1;
                    if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;
                    if (progressInfo is not null) await progressInfo.RefreshProgressBarAsync(i, count);

                    var drawing = ReadDrawing(drawingInfo.FileInfo, false, true);

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
        public FileInfo? GetExistingDsPageFileInfoOrNull(string? fileRelativePath)
        {
            if (string.IsNullOrWhiteSpace(fileRelativePath)) return null;
            if (!IsInitialized) return null;

            var dsPagesDirectoryInfo = DsPagesDirectoryInfo;
            if (dsPagesDirectoryInfo is null) return null;

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(Path.Combine(dsPagesDirectoryInfo.FullName, fileRelativePath));
            }
            catch (Exception)
            {
                return null;
            }

            if (fileInfo.Exists) return fileInfo;

            return null;
        }

        /// <summary>
        ///     path relative to Sahpes directory.
        /// </summary>
        /// <param name="fileRelativePath"></param>
        /// <returns></returns>
        public FileInfo? GetExistingDsShapeFileInfoOrNull(string? fileRelativePath)
        {
            if (string.IsNullOrWhiteSpace(fileRelativePath)) return null;
            if (!IsInitialized) return null;

            var dsShapesDirectoryInfo = DsShapesDirectoryInfo;
            if (dsShapesDirectoryInfo is null) return null;

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(Path.Combine(dsShapesDirectoryInfo.FullName, fileRelativePath));
            }
            catch (Exception)
            {
                return null;
            }

            if (fileInfo.Exists) return fileInfo;

            return null;
        }

        /// <summary>
        ///     returns path relative to dsPages directory.
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        public string GetFileRelativePath(string? fileFullName)
        {
            if (String.IsNullOrEmpty(fileFullName) || !IsInitialized || Mode == DsProjectModeEnum.WebPlayMode) 
                return "";
            var dsPagesDirectoryInfo = DsPagesDirectoryInfo;
            if (dsPagesDirectoryInfo is null) return "";

            if (StringHelper.StartsWithIgnoreCase(fileFullName, dsPagesDirectoryInfo.FullName + @"\"))
                return fileFullName!.Substring(dsPagesDirectoryInfo.FullName.Length + 1);
            return "";
        }

        /// <summary>
        ///     path relative to dsPages directory.
        /// </summary>
        /// <param name="fileRelativePath"></param>
        /// <returns></returns>
        public string GetFileFullName(string fileRelativePath)
        {
            var dsPagesDirectoryInfo = DsPagesDirectoryInfo;
            if (dsPagesDirectoryInfo is null) return "";            
            return Path.Combine(dsPagesDirectoryInfo.FullName, fileRelativePath);
        }

        public void DrawingCopy(FileInfo sourceDrawingFileInfo, FileInfo destinationDrawingFileInfo)
        {
            if (destinationDrawingFileInfo.Exists) throw new ArgumentException();

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
                DrawingBase.IsDsPageFile(destinationDrawingFileInfo) &&
                FileSystemHelper.Compare(destinationDrawingFileInfo.Directory?.FullName, DsPagesDirectoryInfo!.FullName))
            {
                var dsPageDrawing = ReadDrawing(destinationDrawingFileInfo, false, false) as DsPageDrawing;
                if (dsPageDrawing is not null)
                {
                    DsPageDrawing cacheDsPageDrawing = PrepareForCache(dsPageDrawing);
                    dsPageDrawing.Dispose();
                    _allDsPagesCache[dsPageDrawing.Name] = cacheDsPageDrawing;
                    _allDsPagesCacheIsChanged = true;
                }
            }
            else if (_allDsShapeDrawingInfos is not null &&
                     DrawingBase.IsDsShapeFile(destinationDrawingFileInfo) &&
                     FileSystemHelper.Compare(destinationDrawingFileInfo.Directory?.FullName, DsShapesDirectoryInfo!.FullName))
            {
                var dsShapeDrawingInfo =
                    ReadDrawingInfo(destinationDrawingFileInfo, false) as DsShapeDrawingInfo;
                if (dsShapeDrawingInfo is not null)
                    _allDsShapeDrawingInfos[dsShapeDrawingInfo.Name] = dsShapeDrawingInfo;
            }
        }

        public void DrawingDelete(FileInfo drawingFileInfo)
        {
            if (drawingFileInfo.Exists) 
                File.Delete(drawingFileInfo.FullName);

            string drawingFilesDir = DrawingBase.GetDrawingFilesDirectoryFullName(drawingFileInfo.FullName);
            if (Directory.Exists(drawingFilesDir))
                try
                {
                    Directory.Delete(drawingFilesDir, true);
                }
                catch (Exception)
                {
                }

            if (_allDsPagesCache is not null &&
                DrawingBase.IsDsPageFile(drawingFileInfo) &&
                FileSystemHelper.Compare(drawingFileInfo.Directory?.FullName, DsPagesDirectoryInfo!.FullName))
            {
                _allDsPagesCache.Remove(Path.GetFileNameWithoutExtension(drawingFileInfo.Name));
                _allDsPagesCacheIsChanged = true;
            }
            else if (_allDsShapeDrawingInfos is not null &&
                     DrawingBase.IsDsShapeFile(drawingFileInfo) &&
                     FileSystemHelper.Compare(drawingFileInfo.Directory?.FullName, DsShapesDirectoryInfo!.FullName))
            {
                _allDsShapeDrawingInfos.Remove(Path.GetFileNameWithoutExtension(drawingFileInfo.Name));
            }
        }

        public bool SaveUnconditionally(DrawingBase? drawing, IfFileExistsActions ifChangedOutsideSaveType,
            bool generateNewGuid = true, List<string>? errorMessages = null)
        {
            if (drawing is null) return false;
            if (!IsInitialized) return false;

            if (IsReadOnly)
            {
                DsProject.LoggersSet.Logger.LogWarning("Drawing cannot be saved in ReadOnly mode" + drawing.FileFullName);
                return false;
            }

            drawing.BeforeDrawingSave();

            if (File.Exists(drawing.FileFullName))
            {
                var onDriveDrawingInfo = ReadDrawingInfo(new FileInfo(drawing.FileFullName), true);
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
                        DsPagesDirectoryInfo!.FullName))
                {
                    DsPageDrawing cacheDsPageDrawing = PrepareForCache(dsPageDrawing);
                    _allDsPagesCache[dsPageDrawing.Name] = cacheDsPageDrawing;
                    _allDsPagesCacheIsChanged = true;
                }

                var dsShapeDrawing = drawing as DsShapeDrawing;
                if (_allDsShapeDrawingInfos is not null && dsShapeDrawing is not null &&
                    FileSystemHelper.Compare(Path.GetDirectoryName(dsShapeDrawing.FileFullName),
                        DsShapesDirectoryInfo?.FullName))
                    _allDsShapeDrawingInfos[dsShapeDrawing.Name] =
                        (DsShapeDrawingInfo) dsShapeDrawing.GetDrawingInfo();

                drawing.SetDataUnchanged();
            }
            catch (UnauthorizedAccessException ex)
            {
                string message = drawing.FileFullName + @": " +
                                 Resources.DrawingSavingUnauthorizedAccessError;
                DsProject.LoggersSet.Logger.LogError(ex, message);
                if (errorMessages is not null) errorMessages.Add(message + @" " + Resources.SeeErrorLogForDetails);
                return false;
            }
            catch (Exception ex)
            {
                string message = drawing.FileFullName + @": " +
                                 Resources.DrawingSavingError;
                DsProject.LoggersSet.Logger.LogError(ex, message);
                if (errorMessages is not null) errorMessages.Add(message + @" " + Resources.SeeErrorLogForDetails);
                return false;
            }

            //ExportToXamlAsync(drawing.FileInfo.FullName + ".xaml", drawing);

            return false;
        }

        public bool AskAndSetNewFileName(DrawingBase? drawing, string initialFileName)
        {
            var dlg = new SaveFileDialog
            {
                FileName = initialFileName
            };

            if (drawing is DsPageDrawing)
            {
                var dsPagesDirectoryInfo = DsPagesDirectoryInfo;
                if (dsPagesDirectoryInfo is null) return true;

                dlg.InitialDirectory = dsPagesDirectoryInfo.FullName;
                dlg.Filter = @"Save file (*" + DsPageFileExtension + ")|*" + DsPageFileExtension + "|All files (*.*)|*.*";
            }
            else if (drawing is DsShapeDrawing)
            {
                var dsShapesDirectoryInfo = DsShapesDirectoryInfo;
                if (dsShapesDirectoryInfo is null) return true;

                dlg.InitialDirectory = dsShapesDirectoryInfo.FullName;
                dlg.Filter = @"Save file (*" + DsProject.DsShapeFileExtension + ")|*" + DsProject.DsShapeFileExtension + "|All files (*.*)|*.*";
            }
            else
            {
                return true;
            }

            if (dlg.ShowDialog() != true) return true;

            if (!StringHelper.StartsWithIgnoreCase(dlg.FileName, DsPagesDirectoryInfo!.FullName + @"\"))
            {
                MessageBoxHelper.ShowError(Resources.FileMustBeInDsProjectDir);
                return true;
            }

            /*
                    if (String.Compare(dlg.FileName, drawing.FileInfo.FullName, true,
                        CultureInfo.InvariantCulture) == 0)
                    {
                        File.Copy(drawing.FileInfo.FullName, drawing.FileInfo.FullName + ".backup", true);
                        MessageBoxHelper.ShowInfo(Properties.Resources.BackupDrawingWasCreated + " " + drawing.FileInfo.FullName + ".backup");
                    }*/

            drawing.FileFullName = new FileInfo(dlg.FileName).FullName;

            return false;
        }

        public void SavePropsObject(string optionsNameToDisplay, IOwnedDataSerializable ownedDataSerializable)
        {
            try
            {
                string fileFullName = DsProjectPath + @"\" +
                                      optionsNameToDisplay + @".dsprops";
                using (var memoryStream = new MemoryStream(1024 * 1024))
                {
                    var isEmpty = false;
                    using (var writer = new SerializationWriter(memoryStream, true))
                    {
                        using (writer.EnterBlock(1))
                        {
                            writer.Write(Guid.Empty);

                            var beginPosition = memoryStream.Position;
                            ownedDataSerializable.SerializeOwnedData(writer, null);
                            isEmpty = beginPosition == memoryStream.Position;
                        }
                    }

                    if (!isEmpty)
                        using (FileStream fileStream = File.Create(fileFullName))
                        {
                            memoryStream.WriteTo(fileStream);
                        }
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, Resources.PropsSerializationError);
                if (Mode == DsProjectModeEnum.VisualDesignMode)
                    MessageBoxHelper.ShowError(Resources.PropsSerializationError + "\n" +
                                               Resources.SeeErrorLogForDetails);
            }
        }

        public void ReadPropsObject(string optionsNameToDisplay, IOwnedDataSerializable ownedDataSerializable)
        {
            var propsFileFullName = DsProjectPath + @"\" +
                                    optionsNameToDisplay + @".dsprops";
            var propsFileStream = GetStream(propsFileFullName);
            if (propsFileStream is not null)
            {
                using (var reader = new SerializationReader(propsFileStream))
                {
                    using (Block block = reader.EnterBlock())
                    {
                        switch (block.Version)
                        {
                            case 1:
                                var guid = reader.ReadGuid();
                                try
                                {
                                    reader.ReadOwnedData(ownedDataSerializable, this);
                                }
                                catch (BlockUnsupportedVersionException ex)
                                {
                                    DsProject.LoggersSet.Logger.LogError(ex,
                                        optionsNameToDisplay + " " +
                                        Resources.PropsDeserializationErrorUnsupportedVersion);
                                    if (Mode == DsProjectModeEnum.VisualDesignMode)
                                        MessageBoxHelper.ShowError(optionsNameToDisplay + "\n" +
                                                                   Resources
                                                                       .PropsDeserializationErrorUnsupportedVersion +
                                                                   "\n" + Resources.SeeErrorLogForDetails);
                                }
                                catch (Exception ex)
                                {
                                    DsProject.LoggersSet.Logger.LogError(ex, optionsNameToDisplay + " " + Resources.AddonDeserializationError);
                                    if (Mode == DsProjectModeEnum.VisualDesignMode)
                                        MessageBoxHelper.ShowError(optionsNameToDisplay + "\n" +
                                                                   Resources.PropsDeserializationError + "\n" +
                                                                   Resources.SeeErrorLogForDetails);
                                }

                                break;
                            default:
                                DsProject.LoggersSet.Logger.LogError(propsFileFullName + " " +
                                             Resources.PropsDeserializationErrorUnsupportedVersion);
                                if (Mode == DsProjectModeEnum.VisualDesignMode)
                                    MessageBoxHelper.ShowError(propsFileFullName + "\n" +
                                                               Resources
                                                                   .AddonDeserializationErrorUnsupportedFileVersion +
                                                               "\n" + Resources.SeeErrorLogForDetails);
                                break;
                        }
                    }
                }

                propsFileStream.Dispose();
            }
        }

        #endregion
    }
}