using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary
{
    /// <summary>
    ///     The thumbnails of a library, kept so that walking it a second time costs nothing.
    ///     <para>
    ///         A file is remembered by its name together with when it was created, so a file replaced
    ///         on disc is read again.
    ///     </para>
    /// </summary>
    public static class BitmapCache
    {
        #region public functions

        public static IImage? GetBitmapImage(string fileFullName, DateTime fileCreationTimeUtc)
        {
            try
            {
                Tuple<string, DateTime> key = Tuple.Create(fileFullName, fileCreationTimeUtc);
                if (Cache.TryGetValue(key, out IImage? image)) return image;

                if (fileFullName.EndsWith(@".xaml", StringComparison.InvariantCultureIgnoreCase))
                {
                    var xaml = XamlHelper.GetXamlWithAbsolutePaths(new FileInfo(fileFullName), Stretch.Uniform,
                        out _);

                    var contentPreview = XamlHelper.GetContentPreview(xaml, out _, out _) as Avalonia.Controls.Control;
                    if (contentPreview is null) return null;

                    byte[]? bytes = PreviewImageHelper.CreatePreviewImageBytes(contentPreview, ImageWidth, ImageWidth);
                    if (bytes is null) return null;

                    image = new Bitmap(new MemoryStream(bytes));
                }
                else
                {
                    using (FileStream stream = File.OpenRead(fileFullName))
                    {
                        image = Bitmap.DecodeToWidth(stream, ImageWidth);
                    }
                }

                Cache.Add(key, image);

                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region private fields

        private const int ImageWidth = 100;

        private static readonly Dictionary<Tuple<string, DateTime>, IImage> Cache = new();

        #endregion
    }
}
