using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core
{
    public static class XamlHelper
    {
        #region internal functions

        internal static bool CheckXamlForDsShapeExtraction(string? xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml)) return false;
            xaml = GetXamlWithoutDesc(xaml)!.TrimStart();
            if (xaml.StartsWith("<Image")) return false;
            return true;
        }

        #endregion

        #region public functions

        public const string XamlDescBegin = @"<!--Desc:";
        public const string XamlDescEnd = @"-->";

        public static string GetXamlWithAbsolutePaths(string? xamlWithRelativePaths, string? filesDirectoryName)
        {
            if (string.IsNullOrWhiteSpace(xamlWithRelativePaths) || string.IsNullOrEmpty(filesDirectoryName))
                return xamlWithRelativePaths ?? "";

            var result = new StringBuilder();

            var lastIndex = 0;
            for (;;)
            {
                if (lastIndex >= xamlWithRelativePaths!.Length) break;
                var i = xamlWithRelativePaths.IndexOf(@"=""file:./", lastIndex,
                    StringComparison.InvariantCultureIgnoreCase);

                if (i >= 0)
                {
                    i = i + 9;
                    result.Append(xamlWithRelativePaths, lastIndex, i - 9 - lastIndex);
                    var quoteIndex = xamlWithRelativePaths.IndexOf('"', i);
                    if (quoteIndex == -1) break;
                    string relativePath = Uri.UnescapeDataString(xamlWithRelativePaths.Substring(i, quoteIndex - i));
                    lastIndex = quoteIndex + 1;
                    result.Append(@"=""");
                    string fileFullName = filesDirectoryName + @"/" + relativePath;
                    result.Append(GetFileUriStringWithAbsolutePath(fileFullName));
                    result.Append('"');
                }
                else
                {
                    result.Append(xamlWithRelativePaths, lastIndex, xamlWithRelativePaths.Length - lastIndex);
                    break;
                }
            }

            return result.ToString();
        }


        public static string? GetXamlWithRelativePathsAndCopyFiles(string? xamlWithAbsolutePaths,
            string? filesStoreDirectoryName)
        {
            if (string.IsNullOrWhiteSpace(xamlWithAbsolutePaths)) return @"";

            var result = new StringBuilder();

            DirectoryInfo? filesStoreDirectoryInfo = null;
            Dictionary<byte[], FileInfo>? filesStoreInfo = null;

            var lastIndex = 0;
            for (;;)
            {
                if (lastIndex >= xamlWithAbsolutePaths!.Length) break;
                var i = xamlWithAbsolutePaths.IndexOf(@"=""file:///", lastIndex,
                    StringComparison.InvariantCultureIgnoreCase);

                if (i >= 0)
                {
                    if (string.IsNullOrEmpty(filesStoreDirectoryName))
                        return null; // Failed to convert                    

                    if (filesStoreDirectoryInfo is null)
                        filesStoreDirectoryInfo = new DirectoryInfo(filesStoreDirectoryName);

                    result.Append(xamlWithAbsolutePaths, lastIndex, i - lastIndex);

                    i = i + 10;

                    var quoteIndex = xamlWithAbsolutePaths.IndexOf('"', i);
                    if (quoteIndex == -1) break;
                    string absolutePath = Uri.UnescapeDataString(xamlWithAbsolutePaths.Substring(i, quoteIndex - i));
                    var sourceFileInfo = new FileInfo(absolutePath);
                    var destinationFileInfo =
                        GetDestinationFileInfoAndCopyFile(sourceFileInfo, filesStoreDirectoryInfo, ref filesStoreInfo);

                    lastIndex = quoteIndex + 1;
                    result.Append(@"=""");
                    result.Append(GetFileUriStringWithRelativePath(destinationFileInfo.Name));
                    result.Append('"');
                }
                else
                {
                    result.Append(xamlWithAbsolutePaths, lastIndex, xamlWithAbsolutePaths.Length - lastIndex);
                    break;
                }
            }

            return result.ToString();
        }

        public static string GetXamlWithRelativePaths(string xamlWithAbsolutePaths, string filesDirectoryName)
        {
            if (string.IsNullOrWhiteSpace(xamlWithAbsolutePaths)) return "";
            filesDirectoryName = filesDirectoryName.Replace(Path.DirectorySeparatorChar, '/');                
            StringHelper.ReplaceIgnoreCase(ref xamlWithAbsolutePaths,
                @"file:///" + Uri.EscapeDataString(filesDirectoryName) + @"/",
                @"file:./");
            StringHelper.ReplaceIgnoreCase(ref xamlWithAbsolutePaths,
                @"file:///" + filesDirectoryName + @"/",
                @"file:./");
            return xamlWithAbsolutePaths;
        }

        public static string Save(object obj)
        {
            return WpfDispatcherInvoke(() =>
            {
                using (var stream = new MemoryStream())
                {
                    XamlWriter.Save(obj, stream);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }) ?? "";
        }

        public static void Save(object obj, XmlTextWriter xmlTextWriter)
        {
            WpfDispatcherInvoke(() => XamlWriter.Save(obj, xmlTextWriter));
        }

        public static object? Load(string xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml)) return null;

            return WpfDispatcherInvoke(() =>
            {
                try
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                    {
                        return XamlReader.Load(stream);
                    }
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                    return null;
                }
            });
        }

        public static void GetUsedFileNames(string xamlWithRelativePaths, HashSet<string> usedFileNames)
        {
            if (string.IsNullOrWhiteSpace(xamlWithRelativePaths)) return;

            var lastIndex = 0;
            for (;;)
            {
                if (lastIndex >= xamlWithRelativePaths.Length) break;
                var i = xamlWithRelativePaths.IndexOf(@"=""file:./", lastIndex,
                    StringComparison.InvariantCultureIgnoreCase);

                if (i >= 0)
                {
                    i = i + 9;
                    var quoteIndex = xamlWithRelativePaths.IndexOf('"', i);
                    if (quoteIndex == -1) break;
                    string relativePath = Uri.UnescapeDataString(xamlWithRelativePaths.Substring(i, quoteIndex - i));
                    lastIndex = quoteIndex + 1;
                    usedFileNames.Add(relativePath);
                }
                else
                {
                    break;
                }
            }
        }

        public static string SetXamlContentStretch(string xaml, Stretch stretch)
        {
            if (string.IsNullOrWhiteSpace(xaml)) return xaml;

            try
            {
                var desc = GetXamlDesc(xaml);

                var previewContent = Load(xaml) as UIElement;

                if (previewContent is Image)
                {
                    ((Image) previewContent).Stretch = stretch;
                    xaml = AddXamlDesc(Save(previewContent), desc);
                }
                //else if (previewContent is BrowserControl)
                //{
                //    ((BrowserControl) previewContent).Stretch = stretch;
                //    xaml = AddXamlDesc(Save(previewContent), desc);
                //}
                else if (previewContent is Viewbox)
                {
                    if (stretch == Stretch.None)
                    {
                        xaml = AddXamlDesc(Save(((Viewbox)previewContent).Child), desc);
                    }
                    else
                    {
                        ((Viewbox)previewContent).Stretch = stretch;
                        xaml = AddXamlDesc(Save(previewContent), desc);
                    }
                }
                else if (stretch != Stretch.None)
                {
                    var viewBox = new Viewbox
                    {
                        Child = previewContent
                    };
                    viewBox.Stretch = stretch;
                    xaml = AddXamlDesc(Save(viewBox), desc);
                }                

                return xaml;
            }
            catch (Exception)
            {
                return xaml;
            }
        }

        public static string Save(Brush? brush)
        {
            if (brush is null) return "";
            var settings = new XmlWriterSettings();
            /*
                settings.Indent = true;
                settings.NewLineOnAttributes = true;
                */
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            var sb = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(sb, settings);

            var manager = new XamlDesignerSerializationManager(writer);
            manager.XamlWriterMode = XamlWriterMode.Expression;
            XamlWriter.Save(brush, manager);

            //WpfDispatcherInvoke(() => XamlWriter.Save(brush, manager));

            //XamlWriter.Save(brush, writer);

            return sb.ToString();
        }


        public static string GetXamlWithAbsolutePaths(FileInfo fileInfo, Stretch stretch, out Size? contentOriginalSize)
        {
            string? result = null;
            contentOriginalSize = null;
            switch (fileInfo.Extension.ToUpper(CultureInfo.InvariantCulture))
            {
                case ".XAML":
                    try
                    {
                        result = GetXamlWithAbsolutePathsFromXamlFile(new List<FileInfo> {fileInfo},
                            fileInfo.DirectoryName ?? "",
                            stretch, out contentOriginalSize);
                        if (result is null)
                            MessageBoxHelper.ShowError(Resources.XamlFileReadError + @": " + fileInfo.FullName);
                    }
                    catch
                    {
                        MessageBoxHelper.ShowError(Resources.XamlFileReadError + @": " + fileInfo.FullName);
                    }

                    break;
                case ".XPS":
                case ".OXPS":
                    try
                    {
                        result = GetXamlWithAbsolutePathsFromXpsFile(fileInfo,
                            stretch, out contentOriginalSize);
                        if (result is null)
                            MessageBoxHelper.ShowError(Resources.XpsFileReadError + @": " + fileInfo.FullName);
                    }
                    catch
                    {
                        MessageBoxHelper.ShowError(Resources.XpsFileReadError + @": " + fileInfo.FullName);
                    }

                    break;
                case ".HTM":
                case ".HTML":
                    result =
                        "<controlsCommon:BrowserControl xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:controlsCommon=\"clr-namespace:Ssz.Operator.Core.ControlsCommon;assembly=Ssz.Operator.Core\" Url=\"" +
                        GetUriString(DsProject.Instance.GetFileRelativePath(fileInfo.FullName)) + "\" Stretch=\"" +
                        stretch + "\" />";
                    break;
                case ".GIF":
                    result =
                        "<Image xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:wpfAnimatedGif=\"clr-namespace:Ssz.Utils.Wpf.WpfAnimatedGif;assembly=Ssz.Utils.Wpf\" wpfAnimatedGif:ImageBehavior.AnimatedSource=\"" +
                        GetFileUriStringWithAbsolutePath(fileInfo.FullName) + "\" Stretch=\"" + stretch + "\" />";
                    try
                    {
                        System.Drawing.Image img = System.Drawing.Image.FromFile(fileInfo.FullName);
                        contentOriginalSize = new Size(img.Width, img.Height);
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, "Cannot get image size from file " + fileInfo.FullName);
                    }

                    break;
                default:
                    result =
                        "<Image xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Source=\"" +
                        GetFileUriStringWithAbsolutePath(fileInfo.FullName) + "\" Stretch=\"" + stretch + "\" />";
                    try
                    {
                        System.Drawing.Image img = System.Drawing.Image.FromFile(fileInfo.FullName);
                        contentOriginalSize = new Size(img.Width, img.Height);
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, "Cannot get image size from file " + fileInfo.FullName);
                    }

                    break;
            }

            return AddXamlDesc(result, fileInfo.Name);
        }

        public static object? GetContentPreview(string xaml, out string contentDesc, out Stretch contentStretch)
        {
            if (string.IsNullOrWhiteSpace(xaml))
            {
                contentDesc = "<null>";
                contentStretch = Stretch.None;
                return null;
            }

            var desc = GetXamlDesc(xaml);

            try
            {
                var contentPreview = Load(xaml) as UIElement;

                if (contentPreview is null)
                {
                    contentDesc = @"<null>";
                    contentStretch = Stretch.None;
                    return null;
                }

                if (contentPreview is Image)
                {
                    contentDesc = "Image File" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
                    contentStretch = ((Image) contentPreview).Stretch;
                    return contentPreview;
                }
                //if (contentPreview is BrowserControl)
                //{
                //    contentDesc = "HTML" + (!String.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
                //    contentStretch = ((BrowserControl) contentPreview).Stretch;
                //    return contentPreview;
                //}

                var border = new Border();
                border.Background = (Brush) border.FindResource("CheckerBrush");
                border.Child = contentPreview;

                if (contentPreview is Viewbox)
                {
                    contentDesc = "XAML" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
                    contentStretch = ((Viewbox) contentPreview).Stretch;
                    return border;
                }

                contentDesc = "XAML" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
                contentStretch = Stretch.None;
                return border;
            }
            catch (Exception)
            {
                contentDesc = "XAML with Error" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
                contentStretch = Stretch.None;
                return null;
            }
        }

        public static object? GetContentPreviewSmall(string? xamlWithAbsolutePaths)
        {
            if (string.IsNullOrWhiteSpace(xamlWithAbsolutePaths)) return "<null>";

            if (xamlWithAbsolutePaths!.Length >= 64 * 1024)
            {
                var desc = GetXamlDesc(xamlWithAbsolutePaths);
                return "XAML" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
            }

            string contentDesc;
            Stretch contentStretch;
            var contentPreview = GetContentPreview(xamlWithAbsolutePaths, out contentDesc, out contentStretch);

            //if (contentPreview is BrowserControl)
            //{
            //    return contentDesc;
            //}

            var image = contentPreview as Image;
            if (image is not null)
            {
                if (image.Source is not null && (image.Source.Width > 640 || image.Source.Height > 640)) return contentDesc;
                image.Stretch = Stretch.Uniform;
            }

            return contentPreview;
        }


        public static string? GetXamlDesc(string xaml)
        {
            string? desc = null;
            if (xaml is not null && xaml.StartsWith(XamlDescBegin))
            {
                var i = xaml.IndexOf(XamlDescEnd);
                var xamlDescBeginLength = XamlDescBegin.Length;
                if (i > xamlDescBeginLength) desc = xaml.Substring(xamlDescBeginLength, i - xamlDescBeginLength);
            }

            return desc;
        }


#if NET5_0_OR_GREATER
        [return: NotNullIfNotNull("xaml")]
#endif
        public static string? GetXamlWithoutDesc(string? xaml)
        {
            if (xaml is not null && xaml.StartsWith(XamlDescBegin))
            {
                var i = xaml.IndexOf(XamlDescEnd);
                if (i != -1) return xaml.Substring(i + XamlDescEnd.Length);
            }

            return xaml;
        }


        public static string AddXamlDesc(string? xamlWithNoDesc, string? desc)
        {
            if (string.IsNullOrWhiteSpace(xamlWithNoDesc)) return "";
            if (string.IsNullOrWhiteSpace(desc)) return xamlWithNoDesc!;
            return XamlDescBegin + desc + XamlDescEnd + xamlWithNoDesc;
        }


        public static byte[]? CreatePreviewImageBytes(FrameworkElement? frameworkElement, double previewWidth,
            double previewHeight)
        {
            if (frameworkElement is null) return null;

            byte[] result;
            try
            {
                Window? w = null;
                if (!frameworkElement.IsLoaded)
                {
                    w = new Window
                    {
                        AllowsTransparency = true,
                        Background = Brushes.Transparent,
                        WindowStyle = WindowStyle.None,
                        Top = 0,
                        Left = 0,
                        Width = 1,
                        Height = 1,
                        ShowInTaskbar = false,
                        ShowActivated = false
                    };
                    w.Content = frameworkElement;
                    w.Show();
                }

                var actualWidth = frameworkElement.ActualWidth;
                var actualHeight = frameworkElement.ActualHeight;
                var scaleX = previewWidth / actualWidth;
                var scaleY = previewHeight / actualHeight;

                var renderTargetBitmap = new RenderTargetBitmap((int) previewWidth, (int) previewHeight, 96,
                    96,
                    PixelFormats.Pbgra32);

                var drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();
                using (drawingContext)
                {
                    drawingContext.PushTransform(new ScaleTransform(scaleX, scaleY));
                    drawingContext.DrawRectangle(new VisualBrush(frameworkElement), null,
                        new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
                }

                renderTargetBitmap.Render(drawingVisual);

                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                    encoder.Save(stream);
                    result = stream.ToArray();
                }

                if (w is not null) w.Close();
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, "Cannot create preview image");
                return null;
            }

            return result;
        }

        public static void SaveToXamlOrImageFile(string xaml)
        {
            try
            {
                var image = Load(xaml) as Image;

                if (image is not null && image.Source is not null)
                {
                    string imageAbsolutePath =
                        Uri.UnescapeDataString(
                            new Uri(((BitmapFrame) image.Source).Decoder.ToString(), UriKind.Absolute).AbsolutePath);
                    var imageFileName =
                        new FileInfo(imageAbsolutePath);

                    var dlg = new SaveFileDialog
                    {
                        Filter =
                            imageFileName.Extension + @" files (*" + imageFileName.Extension + @")|*" +
                            imageFileName.Extension,
                        FileName = imageFileName.Name
                    };
                    if (dlg.ShowDialog() != true)
                        return;
                    var newFileInfo = new FileInfo(dlg.FileName);

                    try
                    {
                        File.Copy(imageFileName.FullName, newFileInfo.FullName, true);
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, Resources.CannotSaveFileMessage);
                        MessageBoxHelper.ShowError(Resources.CannotSaveFileMessage + @" " +
                                                   Resources.SeeErrorLogForDetails);
                    }

                    return;
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }

            var desc = GetXamlDesc(xaml);

            var dlg2 = new SaveFileDialog
            {
                Filter = @"XAML files (*.xaml)|*.xaml"
            };
            if (desc is not null && desc.EndsWith(@".xaml"))
                dlg2.FileName = desc;
            if (dlg2.ShowDialog() != true)
                return;
            var file2 = new FileInfo(dlg2.FileName);

            try
            {
                using (var textWriter = new StreamWriter(File.Create(file2.FullName), Encoding.UTF8))
                {
                    textWriter.Write(xaml);
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }
        }

        public static void SaveAsPngFile(string xaml)
        {
            var dlg = new SaveFileDialog
            {
                Filter = @"PNG files (*.png)|*.png"
            };
            if (dlg.ShowDialog() != true)
                return;
            var file = new FileInfo(dlg.FileName);

            var content = Load(xaml) as FrameworkElement;
            var width = double.NaN;
            var height = double.NaN;

            var viewBox = content as Viewbox;
            if (viewBox is not null)
            {
                var fe = viewBox.Child as FrameworkElement;
                if (fe is not null)
                {
                    width = fe.Width;
                    height = fe.Height;
                }
            }

            if (double.IsNaN(width) || double.IsNaN(height))
            {
                string widthHeight = Interaction.InputBox(Resources.SaveAsImageFileInputImageDimensions,
                    "", "1024,768");
                if (string.IsNullOrWhiteSpace(widthHeight)) return;
                string[] widthHeightArray = widthHeight.Split(',');
                if (widthHeightArray.Length != 2) return;
                if (!double.TryParse(widthHeightArray[0], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out width)) return;
                if (width < 1 || width > 65534) return;
                if (!double.TryParse(widthHeightArray[1], NumberStyles.Any, CultureInfo.InvariantCulture,
                    out height)) return;
                if (height < 1 || height > 65534) return;
            }

            var bytes = CreatePreviewImageBytes(content, width, height);
            if (bytes is not null)
                try
                {
                    using (FileStream fileStream = File.Create(file.FullName))
                    {
                        fileStream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                }
        }

        public static void SaveAsEmfFile(string xaml)
        {
            var dialog = new SaveFileDialog
            {
                Filter = @"EMF files (*.emf)|*.emf"
            };
            if (dialog.ShowDialog() != true)
                return;
            var fileInfo = new FileInfo(dialog.FileName);

            var content = Load(xaml) as UIElement;
            var viewBox = content as Viewbox;
            if (viewBox is not null) content = viewBox.Child;

            try
            {
                var drawing = Xaml2Emf.GetDrawingFromObj(content);
                if (drawing is null)
                {
                    DsProject.LoggersSet.Logger.LogError("Error: Unable to create Drawing from Xaml");
                    return;
                }

                Xaml2Emf.CreateEmf(fileInfo.FullName, drawing);
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }
        }

        public static bool IsEmptyObject(object obj)
        {
            if (obj is null) return true;

            return WpfDispatcherInvoke(() =>
            {
                var underlyingPanel = obj as Panel;
                if (underlyingPanel is not null && underlyingPanel.Background is null &&
                    underlyingPanel.Children.Count == 0) return true;

                var underlyingDecorator = obj as Decorator;
                if (underlyingDecorator is not null && underlyingDecorator.Child is null) return true;

                return false;
            });
        }

        #endregion


        #region private functions

        private static T? WpfDispatcherInvoke<T>(Func<T> func)
        {
            Application applicationCurrent = Application.Current;
            if (applicationCurrent is null || applicationCurrent.Dispatcher is null) return default;
            return applicationCurrent.Dispatcher.Invoke(func);
        }

        private static void WpfDispatcherInvoke(Action action)
        {
            Application applicationCurrent = Application.Current;
            if (applicationCurrent is null || applicationCurrent.Dispatcher is null) return;
            applicationCurrent.Dispatcher.Invoke(action);
        }

        private static string GetUriString(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, '/'); // no need to escape
        }

        private static string GetFileUriStringWithAbsolutePath(string fileFullName)
        {
            if (fileFullName.StartsWith("pack:")) return fileFullName;
            return @"file:///" + GetUriString(fileFullName);
        }

        private static string GetFileUriStringWithRelativePath(string fileRelativePath)
        {
            return @"file:./" + GetUriString(fileRelativePath);
        }


        private static FileInfo GetDestinationFileInfoAndCopyFile(FileInfo sourceFileInfo,
            DirectoryInfo filesStoreDirectoryInfo, ref Dictionary<byte[], FileInfo>? filesStoreInfo)
        {
            if (filesStoreDirectoryInfo is null ||
                FileSystemHelper.Compare(sourceFileInfo.Directory?.FullName, filesStoreDirectoryInfo?.FullName)) return sourceFileInfo;

            var destinationFileInfo = new FileInfo(filesStoreDirectoryInfo!.FullName + @"\" + sourceFileInfo.Name);
            try
            {
                if (!filesStoreDirectoryInfo.Exists)
                {
                    filesStoreInfo = null;
                    filesStoreDirectoryInfo.Create();
                    sourceFileInfo.CopyTo(destinationFileInfo.FullName, false);
                    return destinationFileInfo;
                }

                byte[]? sourceFileHash = null;
                using (HashAlgorithm hashAlgorithm = SHA256.Create())
                {
                    if (filesStoreInfo is null)
                    {
                        filesStoreInfo = new Dictionary<byte[], FileInfo>(BytesArrayEqualityComparer.Instance);
                        foreach (FileInfo fi in filesStoreDirectoryInfo.GetFiles())
                            using (FileStream stream = File.OpenRead(fi.FullName))
                            {
                                filesStoreInfo[hashAlgorithm.ComputeHash(stream)] = fi;
                            }
                    }

                    using (FileStream stream = File.OpenRead(sourceFileInfo.FullName))
                    {
                        sourceFileHash = hashAlgorithm.ComputeHash(stream);
                    }
                }

                FileInfo? existingFileInfo;
                if (filesStoreInfo.TryGetValue(sourceFileHash, out existingFileInfo)) return existingFileInfo;
                for (var fileIndex = 2;; fileIndex += 1)
                {
                    if (!destinationFileInfo.Exists)
                    {
                        sourceFileInfo.CopyTo(destinationFileInfo.FullName, false);
                        filesStoreInfo[sourceFileHash] = destinationFileInfo;
                        return destinationFileInfo;
                    }

                    destinationFileInfo =
                        new FileInfo(filesStoreDirectoryInfo.FullName + @"\" +
                                     Path.GetFileNameWithoutExtension(sourceFileInfo.Name) +
                                     @"." +
                                     fileIndex + sourceFileInfo.Extension);
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"Copy content file to local drawing directory error: ");
            }

            return destinationFileInfo;
        }


        private static string GetXamlWithAbsolutePathsFromXamlFile(List<FileInfo> fileInfos,
            string parserContextDirectoryName, Stretch stretch,
            out Size? contentOriginalSize)
        {
            Size? contentOriginalSize_ = null;

            string result = WpfDispatcherInvoke(() =>
            {
                var resultUIElements = new List<UIElement>();
                foreach (FileInfo fileInfo in fileInfos)
                    try
                    {
                        using (var xmlTextReader = new XmlTextReader(File.OpenRead(fileInfo.FullName)))
                        {
                            xmlTextReader.MoveToContent();

                            string xaml = xmlTextReader.ReadOuterXml();
                            if (StringHelper.CompareIgnoreCase(fileInfo.Extension, @".fpage"))
                            {
                                string fileDirectoryFullName = fileInfo.DirectoryName + @"\";

                                StringHelper.ReplaceIgnoreCase(ref xaml, @".dict""", @".xaml""");
                                StringHelper.ReplaceIgnoreCase(ref xaml, @".odttf""", @".ttf""");

                                xaml = xaml.Replace(@"Source=""../../../", @"Source=""" +
                                                                           GetFileUriStringWithAbsolutePath(
                                                                               Path.Combine(fileDirectoryFullName,
                                                                                   @"..\..\..")
                                                                           ) + @"/");
                                xaml = xaml.Replace(@"Source=""../../", @"Source=""" + GetFileUriStringWithAbsolutePath(
                                    Path.Combine(fileDirectoryFullName, @"..\..")
                                ) + @"/");
                                xaml = xaml.Replace(@"Source=""../", @"Source=""" + GetFileUriStringWithAbsolutePath(
                                    Path.Combine(fileDirectoryFullName, @"..")
                                ) + @"/");
                                xaml = xaml.Replace(@"Source=""/",
                                    @"Source=""" + GetFileUriStringWithAbsolutePath(parserContextDirectoryName) + @"/");

                                xaml = xaml.Replace(@"Uri=""../../../", @"Uri=""" + GetFileUriStringWithAbsolutePath(
                                    Path.Combine(fileDirectoryFullName, @"..\..\..")
                                ) + @"/");
                                xaml = xaml.Replace(@"Uri=""../../", @"Uri=""" + GetFileUriStringWithAbsolutePath(
                                    Path.Combine(fileDirectoryFullName, @"..\..")
                                ) + @"/");
                                xaml = xaml.Replace(@"Uri=""../", @"Uri=""" + GetFileUriStringWithAbsolutePath(
                                    Path.Combine(fileDirectoryFullName, @"..")
                                ) + @"/");
                                xaml = xaml.Replace(@"Uri=""/",
                                    @"Uri=""" + GetFileUriStringWithAbsolutePath(parserContextDirectoryName) + @"/");

                                xaml = xaml.Replace(@"http://schemas.openxps.org/oxps/v1.0",
                                    @"http://schemas.microsoft.com/xps/2005/06");
                            }

                            var parserContext = new ParserContext
                            {
                                BaseUri =
                                    new Uri(GetUriString(parserContextDirectoryName) + @"/",
                                        UriKind.Absolute)
                            };
                            UIElement? content;
                            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                            {
                                var xamlReader = new XamlReader();
                                content = xamlReader.LoadAsync(stream, parserContext) as UIElement;
                            }

                            if (content is not null) resultUIElements.Add(content);
                        }
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, "XAML parse error");
                    }

                if (resultUIElements.Count == 0)
                {
                    return null;
                }

                if (resultUIElements.Count == 1)
                {
                    if (stretch == Stretch.None)
                    {
                        return Save(resultUIElements[0]);
                    }
                    else
                    {
                        var viewBox = resultUIElements[0] as Viewbox;
                        if (viewBox is null)
                        {
                            var frameworkElement = resultUIElements[0] as FrameworkElement;
                            if (frameworkElement is not null && !double.IsNaN(frameworkElement.Width) &&
                                !double.IsNaN(frameworkElement.Height))
                                contentOriginalSize_ = new Size(frameworkElement.Width, frameworkElement.Height);

                            viewBox = new Viewbox
                            {
                                Child = resultUIElements[0]
                            };
                        }
                        else
                        {
                            var frameworkElement = viewBox.Child as FrameworkElement;
                            if (frameworkElement is not null && !double.IsNaN(frameworkElement.Width) &&
                                !double.IsNaN(frameworkElement.Height))
                                contentOriginalSize_ = new Size(frameworkElement.Width, frameworkElement.Height);
                        }

                        viewBox.Stretch = stretch;

                        return Save(viewBox);
                    }
                }
                else
                {
                    var stackPanel = new StackPanel {Orientation = Orientation.Vertical};
                    foreach (var uiElement in resultUIElements) stackPanel.Children.Add(uiElement);
                    var viewBox = new Viewbox
                    {
                        Child = stackPanel,
                        Stretch = stretch
                    };
                    return Save(viewBox);
                }
            }) ?? "";
            contentOriginalSize = contentOriginalSize_;

            return result;
        }

        private static string GetXamlWithAbsolutePathsFromXpsFile(FileInfo fileInfo, Stretch stretch,
            out Size? contentOriginalSize)
        {
            var dsPageFileInfos = new List<FileInfo>();
            string tempDirectoryName = Path.GetTempPath() + Guid.NewGuid();

            using (var fileStream = File.OpenRead(fileInfo.FullName))
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    if (entry.FullName.EndsWith("/")) continue;
                    using (Stream iStream = entry.Open())
                    {
                        string newExtension = "";
                        if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".dict")) newExtension = @".xaml";
                        if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".odttf")) newExtension = @".ttf";
                        FileInfo oFileInfo;
                        if (newExtension != "")
                            oFileInfo = new FileInfo(tempDirectoryName + @"\" + Path.GetDirectoryName(entry.FullName) +
                                                     @"/" + Path.GetFileNameWithoutExtension(entry.FullName) +
                                                     newExtension);
                        else
                            oFileInfo = new FileInfo(tempDirectoryName + @"\" + entry.FullName);
                        oFileInfo.Directory?.Create();
                        using (var oStream = File.Create(oFileInfo.FullName))
                        {
                            if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".odttf"))
                            {
                                byte[] data;
                                using (var memoryStream = new MemoryStream())
                                {
                                    iStream.CopyTo(memoryStream);
                                    data = memoryStream.ToArray();
                                }

                                //if (font.IsObfuscated)
                                {
                                    string guid =
                                        new Guid(Path.GetFileNameWithoutExtension(entry.FullName)).ToString("N");
                                    byte[] guidBytes = new byte[16];
                                    for (var i = 0; i < guidBytes.Length; i += 1)
                                        guidBytes[i] = Convert.ToByte(guid.Substring(i * 2, 2), 16);

                                    for (var i = 0; i < 32; i += 1)
                                    {
                                        var gi = guidBytes.Length - i % guidBytes.Length - 1;
                                        data[i] ^= guidBytes[gi];
                                    }
                                }
                                oStream.Write(data, 0, data.Length);
                            }
                            else if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".fpage"))
                            {
                                dsPageFileInfos.Add(oFileInfo);
                                iStream.CopyTo(oStream);
                            }
                            else if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".dict"))
                            {
                                string text;
                                using (var sr = new StreamReader(iStream))
                                {
                                    text = sr.ReadToEnd();
                                }

                                text = text.Replace(@"http://schemas.openxps.org/oxps/v1.0",
                                    @"http://schemas.microsoft.com/xps/2005/06");
                                var bytes = Encoding.UTF8.GetBytes(text);
                                oStream.Write(bytes, 0, bytes.Length);
                            }
                            else
                            {
                                iStream.CopyTo(oStream);
                            }
                        }
                    }
                }
            }

            if (dsPageFileInfos.Count == 0)
            {
                contentOriginalSize = new Size();
                return "";
            }

            return GetXamlWithAbsolutePathsFromXamlFile(dsPageFileInfos, tempDirectoryName, stretch,
                out contentOriginalSize);
        }

        #endregion
    }
}