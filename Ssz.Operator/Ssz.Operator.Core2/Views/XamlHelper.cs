using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Gif;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using OxyPlot;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Svg;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using StringHelper = Ssz.Utils.StringHelper;

namespace Ssz.Operator.Core
{
    public static class XamlHelper
    {
        #region public functions

        public const string XamlDescBegin = @"<!--Desc:";
        public const string XamlDescV2Begin = @"<!--DescV2:";
        public const string XamlDescEnd = @"-->";

        public static string GetXamlWithAbsolutePaths(string? xamlWithRelativePaths, string? filesDirectoryFullName)
        {
            if (string.IsNullOrWhiteSpace(xamlWithRelativePaths) || string.IsNullOrEmpty(filesDirectoryFullName))
                return xamlWithRelativePaths ?? "";

            xamlWithRelativePaths = UpdateXamlWithRelativePathsVersion(xamlWithRelativePaths!);

            var xamlWithRelativePaths_Desc = GetXamlDesc(xamlWithRelativePaths);
            var xamlWithRelativePaths_WithoutDesc = GetXamlWithoutDesc(xamlWithRelativePaths);

            if (string.IsNullOrWhiteSpace(xamlWithRelativePaths_WithoutDesc))
            {
                var defaultValue = xamlWithRelativePaths_Desc.TryGetValue(@"");
                if (String.IsNullOrEmpty(defaultValue))
                    return @"";
                string relativePath = Uri.UnescapeDataString(defaultValue);
                if (Path.HasExtension(relativePath) && !IsPathFullyQualified(relativePath))
                {
                    string fileFullName = filesDirectoryFullName + @"/" + relativePath;
                    xamlWithRelativePaths_Desc[@""] = GetUriString(fileFullName);
                }

                return AddXamlDesc(null, xamlWithRelativePaths_Desc);
            }
            else
            {
                var result = new StringBuilder();

                var lastIndex = 0;
                for (; ; )
                {
                    if (lastIndex >= xamlWithRelativePaths_WithoutDesc!.Length) break;
                    var i = xamlWithRelativePaths_WithoutDesc.IndexOf(@"=""file:./", lastIndex,
                        StringComparison.InvariantCultureIgnoreCase);

                    if (i >= 0)
                    {
                        i = i + 9;
                        result.Append(xamlWithRelativePaths_WithoutDesc, lastIndex, i - 9 - lastIndex);
                        var quoteIndex = xamlWithRelativePaths_WithoutDesc.IndexOf('"', i);
                        if (quoteIndex == -1) break;
                        string relativePath = Uri.UnescapeDataString(xamlWithRelativePaths_WithoutDesc.Substring(i, quoteIndex - i));
                        lastIndex = quoteIndex + 1;
                        result.Append(@"=""");
                        string fileFullName = filesDirectoryFullName + @"/" + relativePath;
                        result.Append(GetFileUriStringWithAbsolutePath(fileFullName));
                        result.Append('"');
                    }
                    else
                    {
                        result.Append(xamlWithRelativePaths_WithoutDesc, lastIndex, xamlWithRelativePaths_WithoutDesc.Length - lastIndex);
                        break;
                    }
                }

                return AddXamlDesc(result.ToString(), xamlWithRelativePaths_Desc);
            }
        }

        public static string? GetXamlWithRelativePathsAndCopyFiles(
            string? xamlWithAbsolutePaths,
            string? filesDirectoryFullName)
        {
            if (string.IsNullOrWhiteSpace(xamlWithAbsolutePaths))
                return @"";

            Dictionary<byte[], FileInfo>? filesStoreInfo = null;

            //xamlWithAbsolutePaths = UpdateXamlWithAbsolutePathsVersion(xamlWithAbsolutePaths!);

            var xamlWithAbsolutePaths_Desc = GetXamlDesc(xamlWithAbsolutePaths);
            var xamlWithAbsolutePaths_WithoutDesc = GetXamlWithoutDesc(xamlWithAbsolutePaths);

            if (string.IsNullOrWhiteSpace(xamlWithAbsolutePaths_WithoutDesc))
            {
                var defaultValue = xamlWithAbsolutePaths_Desc.TryGetValue(@"");
                if (String.IsNullOrEmpty(defaultValue))
                    return @"";
                string absolutePath = Uri.UnescapeDataString(defaultValue);
                if (Path.HasExtension(absolutePath) && IsPathFullyQualified(absolutePath))
                {
                    if (string.IsNullOrEmpty(filesDirectoryFullName))
                        return null; // Failed to convert                    

                    var filesStoreDirectoryInfo = new DirectoryInfo(filesDirectoryFullName);
                    var sourceFileInfo = new FileInfo(absolutePath);
                    var destinationFileInfo =
                        GetDestinationFileInfoAndCopyFile(sourceFileInfo, filesStoreDirectoryInfo, ref filesStoreInfo);

                    xamlWithAbsolutePaths_Desc[@""] = GetUriString(destinationFileInfo.Name);
                }

                return AddXamlDesc(null, xamlWithAbsolutePaths_Desc);
            }
            else
            {
                var result = new StringBuilder();

                DirectoryInfo? filesStoreDirectoryInfo = null;

                var lastIndex = 0;
                for (; ; )
                {
                    if (lastIndex >= xamlWithAbsolutePaths_WithoutDesc!.Length) break;
                    var i = xamlWithAbsolutePaths_WithoutDesc.IndexOf(@"=""file:///", lastIndex,
                        StringComparison.InvariantCultureIgnoreCase);

                    if (i >= 0)
                    {
                        if (string.IsNullOrEmpty(filesDirectoryFullName))
                            return null; // Failed to convert                    

                        if (filesStoreDirectoryInfo is null)
                            filesStoreDirectoryInfo = new DirectoryInfo(filesDirectoryFullName);

                        result.Append(xamlWithAbsolutePaths_WithoutDesc, lastIndex, i - lastIndex);

                        i = i + 10;

                        var quoteIndex = xamlWithAbsolutePaths_WithoutDesc.IndexOf('"', i);
                        if (quoteIndex == -1) break;
                        string absolutePath = Uri.UnescapeDataString(xamlWithAbsolutePaths_WithoutDesc.Substring(i, quoteIndex - i));
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
                        result.Append(xamlWithAbsolutePaths_WithoutDesc, lastIndex, xamlWithAbsolutePaths_WithoutDesc.Length - lastIndex);
                        break;
                    }
                }

                return AddXamlDesc(result.ToString(), xamlWithAbsolutePaths_Desc);
            }
        }

        public static string GetXamlWithRelativePaths(string xamlWithAbsolutePaths, string filesDirectoryFullName)
        {
            if (string.IsNullOrWhiteSpace(xamlWithAbsolutePaths)) return "";

            xamlWithAbsolutePaths = UpdateXamlWithAbsolutePathsVersion(xamlWithAbsolutePaths!);

            var xamlWithAbsolutePaths_Desc = GetXamlDesc(xamlWithAbsolutePaths);
            string xamlWithAbsolutePaths_WithoutDesc = GetXamlWithoutDesc(xamlWithAbsolutePaths) ?? @"";

            if (string.IsNullOrWhiteSpace(xamlWithAbsolutePaths_WithoutDesc))
            {
                var defaultValue = xamlWithAbsolutePaths_Desc.TryGetValue(@"");
                if (String.IsNullOrEmpty(defaultValue))
                    return @"";
                string absolutePath = Uri.UnescapeDataString(defaultValue);
                if (Path.HasExtension(absolutePath) && IsPathFullyQualified(absolutePath))
                {
                    if (string.IsNullOrEmpty(filesDirectoryFullName))
                        return @""; // Failed to convert                    

                    filesDirectoryFullName = filesDirectoryFullName.Replace(Path.DirectorySeparatorChar, '/');
                    StringHelper.ReplaceIgnoreCase(ref absolutePath,
                        Uri.EscapeDataString(filesDirectoryFullName) + @"/",
                        @"");

                    xamlWithAbsolutePaths_Desc[@""] = GetUriString(absolutePath);
                }

                return AddXamlDesc(null, xamlWithAbsolutePaths_Desc);
            }
            else
            {
                filesDirectoryFullName = filesDirectoryFullName.Replace(Path.DirectorySeparatorChar, '/');
                StringHelper.ReplaceIgnoreCase(ref xamlWithAbsolutePaths_WithoutDesc,
                    @"file:///" + Uri.EscapeDataString(filesDirectoryFullName) + @"/",
                    @"file:./");
                StringHelper.ReplaceIgnoreCase(ref xamlWithAbsolutePaths_WithoutDesc,
                    @"file:///" + filesDirectoryFullName + @"/",
                    @"file:./");
                return AddXamlDesc(xamlWithAbsolutePaths_WithoutDesc, xamlWithAbsolutePaths_Desc);
            }
        }

        public static string Save(object obj)
        {
            //return WpfDispatcherInvoke(() =>
            //{
            //    using (var stream = new MemoryStream())
            //    {
            //        XamlWriter.Save(obj, stream);
            //        return Encoding.UTF8.GetString(stream.ToArray());
            //    }
            //}) ?? "";
            return @"";
        }

        public static void Save(object obj, XmlTextWriter xmlTextWriter)
        {
            //WpfDispatcherInvoke(() => XamlWriter.Save(obj, xmlTextWriter));
        }

        public static object? Load(string? xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml))
                return null;

            return UIDispatcherInvoke(() =>
            {
                try
                {
                    xaml = PrepareObsoleteXaml(xaml);
                    if (string.IsNullOrWhiteSpace(xaml))
                        return null;
                    return AvaloniaRuntimeXamlLoader.Parse(xaml);                    
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                    return null;
                }
            });
        }

        /// <summary>
        ///    Может быть:
        ///    <para>XAML</para>
        ///    <para>file_name.ext&Stretch=stretch_type</para>
        /// </summary>
        /// <param name="xamlWithDesc"></param>
        /// <param name="filesDirectoryFullName"></param>
        /// <returns></returns>
        public static async Task<object?> LoadFromXamlWithDescAsync(string xamlWithDesc, string? filesDirectoryFullName)
        {
            if (string.IsNullOrWhiteSpace(xamlWithDesc))
                return null;

            try
            {
                // XAML
                string? xamlWithoutDesc = GetXamlWithoutDesc(xamlWithDesc);
                if (!string.IsNullOrWhiteSpace(xamlWithoutDesc))
                {
                    xamlWithoutDesc = PrepareObsoleteXaml(xamlWithoutDesc);
                    if (string.IsNullOrWhiteSpace(xamlWithoutDesc))
                        return null;

                    return UIDispatcherInvoke<object?>(() =>
                    {
                        return AvaloniaRuntimeXamlLoader.Parse(xamlWithoutDesc);
                    });
                }

                var xamlDesc = GetXamlDesc(xamlWithDesc);
                var defaultValue = xamlDesc.TryGetValue(@"");
                if (string.IsNullOrWhiteSpace(defaultValue))
                    return null;

                var stream = await DsProject.GetStreamAsync(Path.Combine(filesDirectoryFullName ?? @"", defaultValue));
                if (stream is null)
                    return null;

                return UIDispatcherInvoke<object?>(() =>
                {
                    try
                    {
                        Stretch stretch = Stretch.Fill;
                        if (xamlDesc.TryGetValue(@"Stretch", out string? stretchString) && !String.IsNullOrEmpty(stretchString))
                            stretch = new Any(stretchString).ValueAs<Stretch>(false);
                        string extension = Path.GetExtension(defaultValue) ?? @"";
                        switch (extension.ToUpperInvariant())
                        {
                            case ".GIF":
                                var image = new GifImage
                                {
                                    Stretch = stretch,
                                    Source = stream
                                };
                                return image;
                            case ".SVG":
                                var svgSource = SvgSource.LoadFromStream(stream);
                                stream.Dispose();
                                return new Image()
                                {
                                    Source = new Avalonia.Svg.Skia.SvgImage() { Source = svgSource },
                                    Stretch = stretch
                                };
                            default:
                                var bitmap = new Bitmap(stream);
                                stream.Dispose();
                                return new Image()
                                {
                                    Source = bitmap,
                                    Stretch = stretch
                                };
                        }
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, @"");
                        return null;
                    }
                });
                
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
                return null;
            }            
        }

        public static void GetUsedFileNames(string xamlWithRelativePaths, HashSet<string> usedFileNames)
        {
            if (string.IsNullOrWhiteSpace(xamlWithRelativePaths))
                return;

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
                    string fileName = Uri.UnescapeDataString(xamlWithRelativePaths.Substring(i, quoteIndex - i));
                    lastIndex = quoteIndex + 1;
                    usedFileNames.Add(fileName);
                }
                else
                {
                    break;
                }
            }
        }

        //public static string SetXamlContentStretch(string xaml, Stretch stretch)
        //{
        //    if (string.IsNullOrWhiteSpace(xaml)) return xaml;

        //    try
        //    {
        //        var desc = GetXamlDesc(xaml);

        //        desc["Stretch"] = new Any(stretch).ValueAsString(false);

        //        var previewContent = Load(xaml) as UIElement;

        //        if (previewContent is Image)
        //        {
        //            ((Image) previewContent).Stretch = stretch;
        //            xaml = AddXamlDesc(Save(previewContent), desc);
        //        }
        //        //else if (previewContent is BrowserControl)
        //        //{
        //        //    ((BrowserControl) previewContent).Stretch = stretch;
        //        //    xaml = AddXamlDesc(Save(previewContent), desc);
        //        //}
        //        else if (previewContent is Viewbox)
        //        {
        //            if (stretch == Stretch.None)
        //            {
        //                xaml = AddXamlDesc(Save(((Viewbox)previewContent).Child), desc);
        //            }
        //            else
        //            {
        //                ((Viewbox)previewContent).Stretch = stretch;
        //                xaml = AddXamlDesc(Save(previewContent), desc);
        //            }
        //        }
        //        else if (stretch != Stretch.None)
        //        {
        //            var viewBox = new Viewbox
        //            {
        //                Child = previewContent
        //            };
        //            viewBox.Stretch = stretch;
        //            xaml = AddXamlDesc(Save(viewBox), desc);
        //        }                

        //        return xaml;
        //    }
        //    catch (Exception)
        //    {
        //        return xaml;
        //    }
        //}

        public static string Save(Brush? brush)
        {
            return "";

            //if (brush is null) return "";
            //var settings = new XmlWriterSettings();
            ///*
            //    settings.Indent = true;
            //    settings.NewLineOnAttributes = true;
            //    */
            //settings.ConformanceLevel = ConformanceLevel.Fragment;
            //var sb = new StringBuilder();
            //XmlWriter writer = XmlWriter.Create(sb, settings);

            //var manager = new XamlDesignerSerializationManager(writer);
            //manager.XamlWriterMode = XamlWriterMode.Expression;
            //XamlWriter.Save(brush, manager);

            ////WpfDispatcherInvoke(() => XamlWriter.Save(brush, manager));

            ////XamlWriter.Save(brush, writer);

            //return sb.ToString();
        }

        public static string GetXamlWithAbsolutePaths(FileInfo fileInfo, Stretch stretch, out Size? contentOriginalSize)
        {
            contentOriginalSize = null;

            return @"";

            //string? result = null;
            //contentOriginalSize = null;
            //switch (fileInfo.Extension.ToUpper(CultureInfo.InvariantCulture))
            //{
            //    case ".XAML":
            //        try
            //        {
            //            result = GetXamlWithAbsolutePathsFromXamlFile(new List<FileInfo> {fileInfo},
            //                fileInfo.DirectoryName ?? "",
            //                stretch, out contentOriginalSize);
            //            if (result is null)
            //                MessageBoxHelper.ShowError(Resources.XamlFileReadError + @": " + fileInfo.FullName);
            //        }
            //        catch
            //        {
            //            MessageBoxHelper.ShowError(Resources.XamlFileReadError + @": " + fileInfo.FullName);
            //        }

            //        break;
            //    case ".XPS":
            //    case ".OXPS":
            //        try
            //        {
            //            result = GetXamlWithAbsolutePathsFromXpsFile(fileInfo,
            //                stretch, out contentOriginalSize);
            //            if (result is null)
            //                MessageBoxHelper.ShowError(Resources.XpsFileReadError + @": " + fileInfo.FullName);
            //        }
            //        catch
            //        {
            //            MessageBoxHelper.ShowError(Resources.XpsFileReadError + @": " + fileInfo.FullName);
            //        }

            //        break;
            //    case ".HTM":
            //    case ".HTML":
            //        result =
            //            "<controlsCommon:BrowserControl xmlns=\"https://github.com/avaloniaui\" xmlns:controlsCommon=\"clr-namespace:Ssz.Operator.Core.ControlsCommon;assembly=Ssz.Operator.Core\" Url=\"" +
            //            GetUriString(DsProject.Instance.GetFileRelativePath(fileInfo.FullName)) + "\" Stretch=\"" +
            //            stretch + "\" />";
            //        break;
            //    case ".GIF":
            //        result =
            //            "<Image xmlns=\"https://github.com/avaloniaui\" xmlns:wpfAnimatedGif=\"clr-namespace:Ssz.Utils.Wpf.WpfAnimatedGif;assembly=Ssz.Utils.Wpf\" wpfAnimatedGif:ImageBehavior.AnimatedSource=\"" +
            //            GetFileUriStringWithAbsolutePath(fileInfo.FullName) + "\" Stretch=\"" + stretch + "\" />";
            //        try
            //        {
            //            System.Drawing.Image img = System.Drawing.Image.FromFile(fileInfo.FullName);
            //            contentOriginalSize = new Size(img.Width, img.Height);
            //        }
            //        catch (Exception ex)
            //        {
            //            DsProject.LoggersSet.Logger.LogError(ex, "Cannot get image size from file " + fileInfo.FullName);
            //        }

            //        break;
            //    default:
            //        result =
            //            "<Image xmlns=\"https://github.com/avaloniaui\" Source=\"" +
            //            GetFileUriStringWithAbsolutePath(fileInfo.FullName) + "\" Stretch=\"" + stretch + "\" />";
            //        try
            //        {
            //            System.Drawing.Image img = System.Drawing.Image.FromFile(fileInfo.FullName);
            //            contentOriginalSize = new Size(img.Width, img.Height);
            //        }
            //        catch (Exception ex)
            //        {
            //            DsProject.LoggersSet.Logger.LogError(ex, "Cannot get image size from file " + fileInfo.FullName);
            //        }

            //        break;
            //}

            //return AddXamlDesc(result, new CaseInsensitiveOrderedDictionary<string?>()
            //    {
            //        { @"", fileInfo.Name },
            //        { @"Stretch", new Any(stretch).ValueAsString(false) }
            //    });
        }

        //public static object? GetContentPreview(string xaml, out string contentDesc, out Stretch contentStretch)
        //{
        //    if (string.IsNullOrWhiteSpace(xaml))
        //    {
        //        contentDesc = "<null>";
        //        contentStretch = Stretch.None;
        //        return null;
        //    }

        //    var desc = NameValueCollectionHelper.GetNameValueCollectionStringToDisplay(GetXamlDesc(xaml));

        //    try
        //    {
        //        var contentPreview = Load(xaml) as UIElement;

        //        if (contentPreview is null)
        //        {
        //            contentDesc = @"<null>";
        //            contentStretch = Stretch.None;
        //            return null;
        //        }

        //        if (contentPreview is Image)
        //        {
        //            contentDesc = "Image File" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
        //            contentStretch = ((Image) contentPreview).Stretch;
        //            return contentPreview;
        //        }
        //        //if (contentPreview is BrowserControl)
        //        //{
        //        //    contentDesc = "HTML" + (!String.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
        //        //    contentStretch = ((BrowserControl) contentPreview).Stretch;
        //        //    return contentPreview;
        //        //}

        //        var border = new Border();
        //        border.Background = (Brush) border.FindResource("CheckerBrush");
        //        border.Child = contentPreview;

        //        if (contentPreview is Viewbox)
        //        {
        //            contentDesc = "XAML" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
        //            contentStretch = ((Viewbox) contentPreview).Stretch;
        //            return border;
        //        }

        //        contentDesc = "XAML" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
        //        contentStretch = Stretch.None;
        //        return border;
        //    }
        //    catch (Exception)
        //    {
        //        contentDesc = "XAML with Error" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
        //        contentStretch = Stretch.None;
        //        return null;
        //    }
        //}

        public static object? GetContentPreviewSmall(string? xamlWithAbsolutePaths)
        {
            if (string.IsNullOrWhiteSpace(xamlWithAbsolutePaths)) 
                return "<null>";

            var desc = NameValueCollectionHelper.GetNameValueCollectionStringToDisplay(GetXamlDesc(xamlWithAbsolutePaths));
            return "XAML" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");

            //if (xamlWithAbsolutePaths!.Length >= 64 * 1024)
            //{
            //    var desc = NameValueCollectionHelper.GetNameValueCollectionStringToDisplay(GetXamlDesc(xamlWithAbsolutePaths));
            //    return "XAML" + (!string.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
            //}

            //string contentDesc;
            //Stretch contentStretch;
            //var contentPreview = GetContentPreview(xamlWithAbsolutePaths, out contentDesc, out contentStretch);

            ////if (contentPreview is BrowserControl)
            ////{
            ////    return contentDesc;
            ////}

            //var image = contentPreview as Image;
            //if (image is not null)
            //{
            //    if (image.Source is not null && (image.Source.Width > 640 || image.Source.Height > 640)) return contentDesc;
            //    image.Stretch = Stretch.Uniform;
            //}

            //return contentPreview;
        }

        public static CaseInsensitiveOrderedDictionary<string?> GetXamlDesc(string xaml)
        {
            if (String.IsNullOrEmpty(xaml))
            {
                return new CaseInsensitiveOrderedDictionary<string?>();
            }
            if (xaml.StartsWith(XamlDescV2Begin))
            {
                string desc = @"";
                var i = xaml.IndexOf(XamlDescEnd);
                var xamlDescV2BeginLength = XamlDescV2Begin.Length;
                if (i > xamlDescV2BeginLength)
                    desc = xaml.Substring(xamlDescV2BeginLength, i - xamlDescV2BeginLength);
                return NameValueCollectionHelper.Parse(desc);
            }
            if (xaml.StartsWith(XamlDescBegin))
            {
                string desc = @"";
                var i = xaml.IndexOf(XamlDescEnd);
                var xamlDescBeginLength = XamlDescBegin.Length;
                if (i > xamlDescBeginLength)
                    desc = xaml.Substring(xamlDescBeginLength, i - xamlDescBeginLength);
                return new CaseInsensitiveOrderedDictionary<string?> { { @"", desc } };
            }
            if (xaml.StartsWith("<!--"))
            {
                string desc = @"";
                var i = xaml.IndexOf(XamlDescEnd);
                var xamlDescBeginLength = "<!--".Length;
                if (i > xamlDescBeginLength)
                    desc = xaml.Substring(xamlDescBeginLength, i - xamlDescBeginLength);
                var descParts = desc.Split(',');
                var result = new CaseInsensitiveOrderedDictionary<string?> { { @"", descParts[0] } };
                if (descParts.Length > 1)
                    result["Stretch"] = descParts[1];
                return result;
            }
            return new CaseInsensitiveOrderedDictionary<string?>();
        }

#if NET5_0_OR_GREATER
        [return: NotNullIfNotNull("xaml")]
#endif
        public static string? GetXamlWithoutDesc(string? xaml)
        {
            if (String.IsNullOrEmpty(xaml))
                return xaml;
            if (xaml!.StartsWith("<!--"))
            {
                var i = xaml.IndexOf(XamlDescEnd);
                if (i != -1)
                    return GetXamlWithoutDesc(xaml.Substring(i + XamlDescEnd.Length).Trim());
            }
            return xaml.Trim();
        }

        public static string AddXamlDesc(string? xamlWithNoDesc, IReadOnlyDictionary<string, string?> nameValueCollection)
        {
            if (nameValueCollection.Count == 0 ||
                    (nameValueCollection.Count == 1 && nameValueCollection.ContainsKey("") && String.IsNullOrEmpty(nameValueCollection[@""])))
                return xamlWithNoDesc ?? @"";
            return XamlDescV2Begin + NameValueCollectionHelper.GetNameValueCollectionString(nameValueCollection) + XamlDescEnd + xamlWithNoDesc;
        }

        //public static byte[]? CreatePreviewImageBytes(Control? frameworkElement, double previewWidth,
        //    double previewHeight)
        //{
        //    if (frameworkElement is null) return null;

        //    byte[] result;
        //    try
        //    {
        //        Window? w = null;
        //        if (!frameworkElement.IsLoaded)
        //        {
        //            w = new Window
        //            {
        //                AllowsTransparency = true,
        //                Background = Brushes.Transparent,
        //                WindowStyle = WindowStyle.None,
        //                Top = 0,
        //                Left = 0,
        //                Width = 1,
        //                Height = 1,
        //                ShowInTaskbar = false,
        //                ShowActivated = false
        //            };
        //            w.Content = frameworkElement;
        //            w.Show();
        //        }

        //        var actualWidth = frameworkElement.Bounds.Width;
        //        var actualHeight = frameworkElement.Bounds.Height;
        //        var scaleX = previewWidth / actualWidth;
        //        var scaleY = previewHeight / actualHeight;

        //        var renderTargetBitmap = new RenderTargetBitmap((int) previewWidth, (int) previewHeight, 96,
        //            96,
        //            PixelFormats.Pbgra32);

        //        var drawingVisual = new DrawingVisual();
        //        DrawingContext drawingContext = drawingVisual.RenderOpen();
        //        using (drawingContext)
        //        {
        //            drawingContext.PushTransform(new ScaleTransform(scaleX, scaleY));
        //            drawingContext.DrawRectangle(new VisualBrush(frameworkElement), null,
        //                new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
        //        }

        //        renderTargetBitmap.Render(drawingVisual);

        //        using (var stream = new MemoryStream())
        //        {
        //            var encoder = new PngBitmapEncoder();
        //            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
        //            encoder.Save(stream);
        //            result = stream.ToArray();
        //        }

        //        if (w is not null) w.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        DsProject.LoggersSet.Logger.LogError(ex, "Cannot create preview image");
        //        return null;
        //    }

        //    return result;
        //}

        //public static void SaveToXamlOrImageFile(string xaml)
        //{
        //    try
        //    {
        //        var image = Load(xaml) as Image;

        //        if (image is not null && image.Source is not null)
        //        {
        //            string imageAbsolutePath =
        //                Uri.UnescapeDataString(
        //                    new Uri(((BitmapFrame) image.Source).Decoder.ToString(), UriKind.Absolute).AbsolutePath);
        //            var imageFileName =
        //                new FileInfo(imageAbsolutePath);

        //            var dlg = new SaveFileDialog
        //            {
        //                Filter =
        //                    imageFileName.Extension + @" files (*" + imageFileName.Extension + @")|*" +
        //                    imageFileName.Extension,
        //                FileName = imageFileName.Name
        //            };
        //            if (dlg.ShowDialog() != true)
        //                return;
        //            var newFileInfo = new FileInfo(dlg.FileName);

        //            try
        //            {
        //                File.Copy(imageFileName.FullName, newFileInfo.FullName, true);
        //            }
        //            catch (Exception ex)
        //            {
        //                DsProject.LoggersSet.Logger.LogError(ex, Resources.CannotSaveFileMessage);
        //                MessageBoxHelper.ShowError(Resources.CannotSaveFileMessage + @" " +
        //                                           Resources.SeeErrorLogForDetails);
        //            }

        //            return;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        DsProject.LoggersSet.Logger.LogError(ex, @"");
        //    }

        //    var desc = GetXamlDesc(xaml).TryGetValue(@"");

        //    var dlg2 = new SaveFileDialog
        //    {
        //        Filter = @"XAML files (*.axaml)|*.axaml"
        //    };
        //    if (desc is not null && desc.EndsWith(@".axaml"))
        //        dlg2.FileName = desc;
        //    if (dlg2.ShowDialog() != true)
        //        return;
        //    var file2 = new FileInfo(dlg2.FileName);

        //    try
        //    {
        //        using (var textWriter = new StreamWriter(File.Create(file2.FullName), Encoding.UTF8))
        //        {
        //            textWriter.Write(xaml);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        DsProject.LoggersSet.Logger.LogError(ex, @"");
        //    }
        //}

        //public static void SaveAsPngFile(string xaml)
        //{
        //    var dlg = new SaveFileDialog
        //    {
        //        Filter = @"PNG files (*.png)|*.png"
        //    };
        //    if (dlg.ShowDialog() != true)
        //        return;
        //    var file = new FileInfo(dlg.FileName);

        //    var content = Load(xaml) as Control;
        //    var width = double.NaN;
        //    var height = double.NaN;

        //    var viewBox = content as Viewbox;
        //    if (viewBox is not null)
        //    {
        //        var fe = viewBox.Child as Control;
        //        if (fe is not null)
        //        {
        //            width = fe.Width;
        //            height = fe.Height;
        //        }
        //    }

        //    if (double.IsNaN(width) || double.IsNaN(height))
        //    {
        //        string widthHeight = Interaction.InputBox(Resources.SaveAsImageFileInputImageDimensions,
        //            "", "1024,768");
        //        if (string.IsNullOrWhiteSpace(widthHeight)) return;
        //        string[] widthHeightArray = widthHeight.Split(',');
        //        if (widthHeightArray.Length != 2) return;
        //        if (!double.TryParse(widthHeightArray[0], NumberStyles.Any, CultureInfo.InvariantCulture,
        //            out width)) return;
        //        if (width < 1 || width > 65534) return;
        //        if (!double.TryParse(widthHeightArray[1], NumberStyles.Any, CultureInfo.InvariantCulture,
        //            out height)) return;
        //        if (height < 1 || height > 65534) return;
        //    }

        //    var bytes = CreatePreviewImageBytes(content, width, height);
        //    if (bytes is not null)
        //        try
        //        {
        //            using (FileStream fileStream = File.Create(file.FullName))
        //            {
        //                fileStream.Write(bytes, 0, bytes.Length);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            DsProject.LoggersSet.Logger.LogError(ex, @"");
        //        }
        //}

        //public static void SaveAsEmfFile(string xaml)
        //{
        //    var dialog = new SaveFileDialog
        //    {
        //        Filter = @"EMF files (*.emf)|*.emf"
        //    };
        //    if (dialog.ShowDialog() != true)
        //        return;
        //    var fileInfo = new FileInfo(dialog.FileName);

        //    var content = Load(xaml) as UIElement;
        //    var viewBox = content as Viewbox;
        //    if (viewBox is not null) content = viewBox.Child;

        //    try
        //    {
        //        var drawing = Xaml2Emf.GetDrawingFromObj(content);
        //        if (drawing is null)
        //        {
        //            DsProject.LoggersSet.Logger.LogError("Error: Unable to create Drawing from Xaml");
        //            return;
        //        }

        //        Xaml2Emf.CreateEmf(fileInfo.FullName, drawing);
        //    }
        //    catch (Exception ex)
        //    {
        //        DsProject.LoggersSet.Logger.LogError(ex, @"");
        //    }
        //}

        public static bool IsEmptyObject(object obj)
        {
            if (obj is null) return true;

            return UIDispatcherInvoke(() =>
            {
                var underlyingPanel = obj as Panel;
                if (underlyingPanel is not null && underlyingPanel.Background is null &&
                    underlyingPanel.Children.Count == 0) return true;

                var underlyingDecorator = obj as Decorator;
                if (underlyingDecorator is not null && underlyingDecorator.Child is null) return true;

                return false;
            });
        }

        /// <summary>
        ///     Preconditions: xaml must have an absolute paths.
        /// </summary>
        /// <param name="xaml"></param>
        /// <returns></returns>
        public static string UpdateXamlWithAbsolutePathsVersion(string xaml)
        {
            try
            {
                var xamlDesc = GetXamlDesc(xaml);
                var xamlWithoutDesc = GetXamlWithoutDesc(xaml) ?? @"";

                if (string.IsNullOrWhiteSpace(xamlWithoutDesc) || !xamlWithoutDesc.StartsWith(@"<"))
                {
                    return AddXamlDesc(xamlWithoutDesc, xamlDesc);
                }
                else
                {
                    return xaml;

                    //var content = Load(xamlWithoutDesc) as UIElement;

                    //if (content is null)
                    //    return @"";

                    //if (content is Image image)
                    //{
                    //    var bitmapImage = image.Source as BitmapImage;
                    //    if (bitmapImage is not null)
                    //    {
                    //        string? imagePath = bitmapImage!.UriSource?.LocalPath;
                    //        if (imagePath is not null)
                    //        {
                    //            xamlDesc[@""] = Path.GetFileName(imagePath);
                    //            xamlDesc[@"Stretch"] = new Any(image.Stretch).ValueAsString(false);
                    //        }
                    //        return AddXamlDesc(null, xamlDesc);
                    //    }

                    //    var animatedSource = ImageBehavior.GetAnimatedSource(image) as BitmapFrame;
                    //    if (animatedSource is not null)
                    //    {
                    //        string? imagePath = (animatedSource.Decoder.GetType().GetField("_uri", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(animatedSource.Decoder) as Uri)?.LocalPath;
                    //        if (imagePath is not null)
                    //        {
                    //            xamlDesc[@""] = Path.GetFileName(imagePath);
                    //            xamlDesc[@"Stretch"] = new Any(image.Stretch).ValueAsString(false);
                    //        }
                    //        return AddXamlDesc(null, xamlDesc);
                    //    }

                    //    xamlDesc[@"Stretch"] = new Any(image.Stretch).ValueAsString(false);
                    //    return AddXamlDesc(xamlWithoutDesc, xamlDesc);
                    //}
                    //if (content is SvgViewbox svgViewbox)
                    //{
                    //    string? imagePath = svgViewbox.Source?.LocalPath;
                    //    if (imagePath is not null)
                    //    {
                    //        xamlDesc[@""] = Path.GetFileName(imagePath);
                    //        xamlDesc[@"Stretch"] = new Any(svgViewbox.Stretch).ValueAsString(false);
                    //    }
                    //    return AddXamlDesc(null, xamlDesc);
                    //}
                    ////if (contentPreview is BrowserControl)
                    ////{
                    ////    contentDesc = "HTML" + (!String.IsNullOrWhiteSpace(desc) ? @": " + desc : "");
                    ////    contentStretch = ((BrowserControl) contentPreview).Stretch;
                    ////    return contentPreview;
                    ////}                

                    //if (content is Viewbox viewbox)
                    //{
                    //    xamlDesc[@""] = @"XAML";
                    //    xamlDesc[@"Stretch"] = new Any(viewbox.Stretch).ValueAsString(false);

                    //    return AddXamlDesc(xamlWithoutDesc, xamlDesc);
                    //}

                    //xamlDesc[@""] = @"XAML";
                    //xamlDesc[@"Stretch"] = new Any(Stretch.None).ValueAsString(false);
                    //return AddXamlDesc(xamlWithoutDesc, xamlDesc);
                }
            }
            catch (Exception)
            {
            }

            return xaml;
        }

        /// <summary>
        ///     Preconditions: xaml must have an relative paths.
        /// </summary>
        /// <param name="xaml"></param>
        /// <returns></returns>
        public static string UpdateXamlWithRelativePathsVersion(string xaml)
        {
            try
            {
                var xamlDesc = GetXamlDesc(xaml);
                var xamlWithoutDesc = GetXamlWithoutDesc(xaml) ?? @"";

                if (string.IsNullOrWhiteSpace(xamlWithoutDesc) || !xamlWithoutDesc.StartsWith(@"<"))
                {
                    return AddXamlDesc(xamlWithoutDesc, xamlDesc);
                }
                else
                {
                    var defaultValue = xamlDesc.TryGetValue(@"");
                    if (!String.IsNullOrEmpty(defaultValue))
                    {
                        var extensionLower = Path.GetExtension(defaultValue).ToLower();
                        if (!String.IsNullOrEmpty(extensionLower) && extensionLower != @".xaml")
                            return AddXamlDesc(null, xamlDesc);
                    }

                    return AddXamlDesc(xamlWithoutDesc, xamlDesc);
                }
            }
            catch (Exception)
            {
            }

            return xaml;
        }

        #endregion

        #region internal functions

        internal static bool CheckXamlForDsShapeExtraction(string? xaml)
        {
            if (string.IsNullOrWhiteSpace(xaml)) return false;
            xaml = GetXamlWithoutDesc(xaml)!.TrimStart();
            if (xaml.StartsWith("<Image")) return false;
            return true;
        }

        #endregion

        #region private functions

        private static T? UIDispatcherInvoke<T>(Func<T> func)
        {
            if (OperatingSystem.IsBrowser())
                return func();
            else
                return Dispatcher.UIThread.Invoke(func);
        }

        private static void UIDispatcherInvoke(Action action)
        {
            if (OperatingSystem.IsBrowser())
                action();
            else
                Dispatcher.UIThread.Invoke(action);
        }            

        private static string GetDestinationFileNameAndCopyFile(string sourceFileFullName,
            string? filesDirectoryFullName, ref Dictionary<byte[], string>? filesStoreInfo)
        {
            if (filesDirectoryFullName is null ||
                    FileSystemHelper.Compare(Path.GetDirectoryName(sourceFileFullName), filesDirectoryFullName)) 
                return Path.GetFileName(sourceFileFullName);

            if (!DsProject.Instance.IsReadOnly && DsProject.Instance.Mode != DsProject.DsProjectModeEnum.BrowserPlayMode)
            {
                var sourceFileInfo = new FileInfo(sourceFileFullName);
                var destinationFileInfo = new FileInfo(Path.Combine(filesDirectoryFullName, Path.GetFileName(sourceFileFullName)));
                DirectoryInfo filesDirectoryInfo = new(filesDirectoryFullName);

                try
                {
                    if (!filesDirectoryInfo.Exists)
                    {
                        filesStoreInfo = null;
                        filesDirectoryInfo.Create();
                        sourceFileInfo.CopyTo(destinationFileInfo.FullName, false);
                        return destinationFileInfo.Name;
                    }

                    byte[]? sourceFileHash = null;
                    using (HashAlgorithm hashAlgorithm = SHA256.Create())
                    {
                        if (filesStoreInfo is null)
                        {
                            filesStoreInfo = new Dictionary<byte[], string>(BytesArrayEqualityComparer.Instance);
                            foreach (FileInfo fi in filesDirectoryInfo.GetFiles())
                                using (FileStream stream = File.OpenRead(fi.FullName))
                                {
                                    filesStoreInfo[hashAlgorithm.ComputeHash(stream)] = fi.FullName;
                                }
                        }

                        using (FileStream stream = File.OpenRead(sourceFileInfo.FullName))
                        {
                            sourceFileHash = hashAlgorithm.ComputeHash(stream);
                        }
                    }
                    
                    if (filesStoreInfo.TryGetValue(sourceFileHash, out string? existingFileFullName)) 
                        return Path.GetFileName(existingFileFullName);
                    for (var fileIndex = 2; ; fileIndex += 1)
                    {
                        if (!destinationFileInfo.Exists)
                        {
                            sourceFileInfo.CopyTo(destinationFileInfo.FullName, false);
                            filesStoreInfo[sourceFileHash] = destinationFileInfo.FullName;
                            return destinationFileInfo.Name;
                        }

                        destinationFileInfo =
                            new FileInfo(Path.Combine(filesDirectoryInfo.FullName,
                                         Path.GetFileNameWithoutExtension(sourceFileInfo.Name) +
                                         @"." +
                                         fileIndex + sourceFileInfo.Extension));
                    }
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"Copy content file to local drawing directory error: ");
                }

                return destinationFileInfo.Name;
            }            
            else
            {
                return @"";
            }
        }
        
        private static string? PrepareObsoleteXaml(string? xaml)
        {
            if (string.IsNullOrEmpty(xaml))
                return null;

            if (xaml.StartsWith("<Style "))
                return null;            

            xaml = xaml.Replace("xmlns=\"http://schemas.microsoft.com/" + "winfx/2006/xaml/presentation\"", "xmlns=\"https://github.com/avaloniaui\""); // Splitted because of replace issues in VS Editor

            //if (xaml.Contains("<SolidColorBrush") || 
            //    xaml.Contains("<LinearGradientBrush") || 
            //    xaml.Contains("<RadialGradientBrush") ||
            //    xaml.Contains("<ConicGradientBrush") ||
            //    xaml.Contains("LineCap")) // Not full list
            {
                XDocument doc = XDocument.Parse(xaml);
                XNamespace ns = "https://github.com/avaloniaui";

                foreach (var brush in doc.Descendants(ns + "SolidColorBrush"))
                {
                    if (!String.IsNullOrEmpty(brush.Value))
                    {
                        brush.SetAttributeValue("Color", brush.Value);
                        brush.Value = string.Empty; // Убираем текстовое содержимое
                    }
                }

                foreach (var brush in doc.Descendants(ns + "LinearGradientBrush"))
                {
                    ConvertToPercentage(brush, "StartPoint");
                    ConvertToPercentage(brush, "EndPoint");

                    Vector2 startPoint = ParsePoint(brush.Attribute("StartPoint")?.Value ?? @"");
                    Vector2 endPoint = ParsePoint(brush.Attribute("EndPoint")?.Value ?? @"");

                    foreach (var transform in brush.Descendants(ns + "SkewTransform").Concat(brush.Descendants(ns + "RotateTransform")))
                    {
                        string type = transform.Name.LocalName;
                        float centerX = float.Parse(transform.Attribute("CenterX")?.Value ?? @"", CultureInfo.InvariantCulture);
                        float centerY = float.Parse(transform.Attribute("CenterY")?.Value ?? @"", CultureInfo.InvariantCulture);

                        if (type == "SkewTransform")
                        {
                            float angleX = float.Parse(transform.Attribute("AngleX")?.Value ?? @"", CultureInfo.InvariantCulture);
                            startPoint = ApplySkew(startPoint, angleX, centerX, centerY);
                            endPoint = ApplySkew(endPoint, angleX, centerX, centerY);
                        }
                        else if (type == "RotateTransform")
                        {
                            float angle = float.Parse(transform.Attribute("Angle")?.Value ?? @"", CultureInfo.InvariantCulture);
                            startPoint = ApplyRotation(startPoint, angle, centerX, centerY);
                            endPoint = ApplyRotation(endPoint, angle, centerX, centerY);
                        }
                    }

                    brush.SetAttributeValue("StartPoint", FormatPoint(startPoint));
                    brush.SetAttributeValue("EndPoint", FormatPoint(endPoint));
                    brush.Descendants(ns + "LinearGradientBrush.RelativeTransform").Remove();
                }

                foreach (var brush in doc.Descendants(ns + "RadialGradientBrush"))
                {
                    ConvertToPercentage(brush, "Center");
                    ConvertToPercentage(brush, "GradientOrigin");                    

                    brush.Descendants(ns + "RadialGradientBrush.RelativeTransform").Remove();
                }

                //foreach (var brush in doc.Descendants(ns + "RadialGradientBrush"))
                //{
                //    ConvertToPercentage(brush, "Center");
                //    ConvertToPercentage(brush, "GradientOrigin");

                //    XAttribute? attr = brush.Attribute("RadiusX");
                //    if (attr != null)
                //    {
                //        brush.SetAttributeValue("Radius", attr.Value);
                //        brush.SetAttributeValue("RadiusX", null);
                //        brush.SetAttributeValue("RadiusY", null);
                //    }

                //    brush.Descendants(ns + "RadialGradientBrush.RelativeTransform").Remove();
                //}

                foreach (var element in doc.Descendants())
                {
                    // Переименовываем атрибут StrokeStartLineCap в StrokeLineCap
                    var strokeStartLineCapAttr = element.Attribute("StrokeStartLineCap");
                    if (strokeStartLineCapAttr != null)
                    {
                        // Создаем новый атрибут с нужным именем и значением
                        element.SetAttributeValue("StrokeLineCap", strokeStartLineCapAttr.Value);
                        // Удаляем старый атрибут
                        strokeStartLineCapAttr.Remove();
                    }

                    // Переименовываем атрибут StrokeLineJoin в StrokeJoin
                    var strokeLineJoinAttr = element.Attribute("StrokeLineJoin");
                    if (strokeLineJoinAttr != null)
                    {
                        // Создаем новый атрибут с нужным именем и значением
                        element.SetAttributeValue("StrokeJoin", strokeLineJoinAttr.Value);
                        // Удаляем старый атрибут
                        strokeLineJoinAttr.Remove();
                    }

                    // Удаляем атрибут StrokeEndLineCap
                    var strokeEndLineCapAttr = element.Attribute("StrokeEndLineCap");
                    if (strokeEndLineCapAttr != null)
                    {
                        strokeEndLineCapAttr.Remove();
                    }
                }

                xaml = doc.ToString(SaveOptions.DisableFormatting) ;
            }

            return xaml;
        }

        static void ConvertToPercentage(XElement element, string attributeName)
        {
            XAttribute? attr = element.Attribute(attributeName);
            if (attr != null)
            {
                string[] parts = attr.Value.Split(',');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                {
                    string newValue = $"{x * 100}%,{y * 100}%";
                    attr.Value = newValue;
                }
            }
        }

        static Vector2 ParsePoint(string value)
        {
            var parts = value.Split(',').Select(p => float.Parse(p.TrimEnd('%'), CultureInfo.InvariantCulture) / 100f).ToArray();
            return new Vector2(parts[0], parts[1]);
        }

        static string FormatPoint(Vector2 point) => $"{point.X * 100}%,{point.Y * 100}%";

        static Vector2 ApplySkew(Vector2 point, float angleX, float centerX, float centerY)
        {
            float skewRad = angleX * (float)(Math.PI / 180);
            float offsetX = (point.Y - centerY) * (float)Math.Tan(skewRad);
            return new Vector2(point.X + offsetX, point.Y);
        }

        static Vector2 ApplyRotation(Vector2 point, float angle, float centerX, float centerY)
        {
            float rad = angle * (float)(Math.PI / 180);
            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);

            float x = point.X - centerX;
            float y = point.Y - centerY;

            return new Vector2(
                centerX + (x * cos - y * sin),
                centerY + (x * sin + y * cos)
            );
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

            var destinationFileInfo = new FileInfo(Path.Combine(filesStoreDirectoryInfo!.FullName, sourceFileInfo.Name));
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
                for (var fileIndex = 2; ; fileIndex += 1)
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

        private static bool IsPathFullyQualified(string? defaultValue)
        {
            if (String.IsNullOrEmpty(defaultValue))
                return false;
#if NET10_0_OR_GREATER
            return Path.IsPathFullyQualified(defaultValue);
#else
            // TODO
            return defaultValue!.Contains(@"\");
#endif            
        }

        //private static string GetXamlWithAbsolutePathsFromXamlFile(List<FileInfo> fileInfos,
        //    string parserContextDirectoryName, Stretch stretch,
        //    out Size? contentOriginalSize)
        //{
        //    Size? contentOriginalSize_ = null;

        //    string result = WpfDispatcherInvoke(() =>
        //    {
        //        var resultUIElements = new List<UIElement>();
        //        foreach (FileInfo fileInfo in fileInfos)
        //            try
        //            {
        //                using (var xmlTextReader = new XmlTextReader(File.OpenRead(fileInfo.FullName)))
        //                {
        //                    xmlTextReader.MoveToContent();

        //                    string xaml = xmlTextReader.ReadOuterXml();
        //                    if (StringHelper.CompareIgnoreCase(fileInfo.Extension, @".fpage"))
        //                    {
        //                        string fileDirectoryFullName = fileInfo.DirectoryName + @"\";

        //                        StringHelper.ReplaceIgnoreCase(ref xaml, @".dict""", @".axaml""");
        //                        StringHelper.ReplaceIgnoreCase(ref xaml, @".odttf""", @".ttf""");

        //                        xaml = xaml.Replace(@"Source=""../../../", @"Source=""" +
        //                                                                   GetFileUriStringWithAbsolutePath(
        //                                                                       Path.Combine(fileDirectoryFullName,
        //                                                                           @"..\..\..")
        //                                                                   ) + @"/");
        //                        xaml = xaml.Replace(@"Source=""../../", @"Source=""" + GetFileUriStringWithAbsolutePath(
        //                            Path.Combine(fileDirectoryFullName, @"..\..")
        //                        ) + @"/");
        //                        xaml = xaml.Replace(@"Source=""../", @"Source=""" + GetFileUriStringWithAbsolutePath(
        //                            Path.Combine(fileDirectoryFullName, @"..")
        //                        ) + @"/");
        //                        xaml = xaml.Replace(@"Source=""/",
        //                            @"Source=""" + GetFileUriStringWithAbsolutePath(parserContextDirectoryName) + @"/");

        //                        xaml = xaml.Replace(@"Uri=""../../../", @"Uri=""" + GetFileUriStringWithAbsolutePath(
        //                            Path.Combine(fileDirectoryFullName, @"..\..\..")
        //                        ) + @"/");
        //                        xaml = xaml.Replace(@"Uri=""../../", @"Uri=""" + GetFileUriStringWithAbsolutePath(
        //                            Path.Combine(fileDirectoryFullName, @"..\..")
        //                        ) + @"/");
        //                        xaml = xaml.Replace(@"Uri=""../", @"Uri=""" + GetFileUriStringWithAbsolutePath(
        //                            Path.Combine(fileDirectoryFullName, @"..")
        //                        ) + @"/");
        //                        xaml = xaml.Replace(@"Uri=""/",
        //                            @"Uri=""" + GetFileUriStringWithAbsolutePath(parserContextDirectoryName) + @"/");

        //                        xaml = xaml.Replace(@"http://schemas.openxps.org/oxps/v1.0",
        //                            @"http://schemas.microsoft.com/xps/2005/06");
        //                    }

        //                    var parserContext = new ParserContext
        //                    {
        //                        BaseUri =
        //                            new Uri(GetUriString(parserContextDirectoryName) + @"/",
        //                                UriKind.Absolute)
        //                    };
        //                    UIElement? content;
        //                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
        //                    {
        //                        var xamlReader = new AvaloniaXamlLoader();
        //                        content = xamlReader.LoadAsync(stream, parserContext) as UIElement;
        //                    }

        //                    if (content is not null) resultUIElements.Add(content);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                DsProject.LoggersSet.Logger.LogError(ex, "XAML parse error");
        //            }

        //        if (resultUIElements.Count == 0)
        //        {
        //            return null;
        //        }

        //        if (resultUIElements.Count == 1)
        //        {
        //            if (stretch == Stretch.None)
        //            {
        //                return Save(resultUIElements[0]);
        //            }
        //            else
        //            {
        //                var viewBox = resultUIElements[0] as Viewbox;
        //                if (viewBox is null)
        //                {
        //                    var frameworkElement = resultUIElements[0] as Control;
        //                    if (frameworkElement is not null && !double.IsNaN(frameworkElement.Width) &&
        //                        !double.IsNaN(frameworkElement.Height))
        //                        contentOriginalSize_ = new Size(frameworkElement.Width, frameworkElement.Height);

        //                    viewBox = new Viewbox
        //                    {
        //                        Child = resultUIElements[0]
        //                    };
        //                }
        //                else
        //                {
        //                    var frameworkElement = viewBox.Child as Control;
        //                    if (frameworkElement is not null && !double.IsNaN(frameworkElement.Width) &&
        //                        !double.IsNaN(frameworkElement.Height))
        //                        contentOriginalSize_ = new Size(frameworkElement.Width, frameworkElement.Height);
        //                }

        //                viewBox.Stretch = stretch;

        //                return Save(viewBox);
        //            }
        //        }
        //        else
        //        {
        //            var stackPanel = new StackPanel {Orientation = Orientation.Vertical};
        //            foreach (var uiElement in resultUIElements) stackPanel.Children.Add(uiElement);
        //            var viewBox = new Viewbox
        //            {
        //                Child = stackPanel,
        //                Stretch = stretch
        //            };
        //            return Save(viewBox);
        //        }
        //    }) ?? "";
        //    contentOriginalSize = contentOriginalSize_;

        //    return result;
        //}

        //private static string GetXamlWithAbsolutePathsFromXpsFile(FileInfo fileInfo, Stretch stretch,
        //    out Size? contentOriginalSize)
        //{
        //    var dsPageFileInfos = new List<FileInfo>();
        //    string tempDirectoryName = Path.GetTempPath() + Guid.NewGuid();

        //    using (var fileStream = File.OpenRead(fileInfo.FullName))
        //    using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        //    {
        //        foreach (var entry in zipArchive.Entries)
        //        {
        //            if (entry.FullName.EndsWith("/")) continue;
        //            using (Stream iStream = entry.Open())
        //            {
        //                string newExtension = "";
        //                if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".dict")) newExtension = @".axaml";
        //                if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".odttf")) newExtension = @".ttf";
        //                FileInfo oFileInfo;
        //                if (newExtension != "")
        //                    oFileInfo = new FileInfo(tempDirectoryName + @"\" + Path.GetDirectoryName(entry.FullName) +
        //                                             @"/" + Path.GetFileNameWithoutExtension(entry.FullName) +
        //                                             newExtension);
        //                else
        //                    oFileInfo = new FileInfo(tempDirectoryName + @"\" + entry.FullName);
        //                oFileInfo.Directory?.Create();
        //                using (var oStream = File.Create(oFileInfo.FullName))
        //                {
        //                    if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".odttf"))
        //                    {
        //                        byte[] data;
        //                        using (var memoryStream = new MemoryStream())
        //                        {
        //                            iStream.CopyTo(memoryStream);
        //                            data = memoryStream.ToArray();
        //                        }

        //                        //if (font.IsObfuscated)
        //                        {
        //                            string guid =
        //                                new Guid(Path.GetFileNameWithoutExtension(entry.FullName)).ToString("N");
        //                            byte[] guidBytes = new byte[16];
        //                            for (var i = 0; i < guidBytes.Length; i += 1)
        //                                guidBytes[i] = Convert.ToByte(guid.Substring(i * 2, 2), 16);

        //                            for (var i = 0; i < 32; i += 1)
        //                            {
        //                                var gi = guidBytes.Length - i % guidBytes.Length - 1;
        //                                data[i] ^= guidBytes[gi];
        //                            }
        //                        }
        //                        oStream.Write(data, 0, data.Length);
        //                    }
        //                    else if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".fpage"))
        //                    {
        //                        dsPageFileInfos.Add(oFileInfo);
        //                        iStream.CopyTo(oStream);
        //                    }
        //                    else if (StringHelper.EndsWithIgnoreCase(entry.FullName, @".dict"))
        //                    {
        //                        string text;
        //                        using (var sr = new StreamReader(iStream))
        //                        {
        //                            text = sr.ReadToEnd();
        //                        }

        //                        text = text.Replace(@"http://schemas.openxps.org/oxps/v1.0",
        //                            @"http://schemas.microsoft.com/xps/2005/06");
        //                        var bytes = Encoding.UTF8.GetBytes(text);
        //                        oStream.Write(bytes, 0, bytes.Length);
        //                    }
        //                    else
        //                    {
        //                        iStream.CopyTo(oStream);
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (dsPageFileInfos.Count == 0)
        //    {
        //        contentOriginalSize = new Size();
        //        return "";
        //    }

        //    return GetXamlWithAbsolutePathsFromXamlFile(dsPageFileInfos, tempDirectoryName, stretch,
        //        out contentOriginalSize);
        //}

        #endregion
    }
}