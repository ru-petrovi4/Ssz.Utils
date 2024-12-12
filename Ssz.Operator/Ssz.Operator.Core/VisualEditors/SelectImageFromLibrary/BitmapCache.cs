using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary
{
    public static class BitmapCache
    {
        #region private fields

        private static readonly Dictionary<Tuple<string, DateTime>, BitmapImage> Cache =
            new();

        #endregion

        #region public functions

        public static BitmapImage? GetBitmapImage(string fileFullName,
            DateTime fileCreationTimeUtc)
        {
            try
            {
                const int imageWidth = 100;

                BitmapImage? image;

                if (Cache.TryGetValue(Tuple.Create(fileFullName, fileCreationTimeUtc), out image)) return image;

                if (fileFullName.EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase))
                {
                    Size? contentOriginalSize;
                    string xaml = XamlHelper.GetXamlWithAbsolutePaths(new FileInfo(fileFullName), Stretch.Uniform,
                        out contentOriginalSize);

                    string contentDesc;
                    Stretch contentStretch;
                    var contentPreview =
                        XamlHelper.GetContentPreview(xaml, out contentDesc, out contentStretch) as FrameworkElement;

                    var bytes = XamlHelper.CreatePreviewImageBytes(contentPreview, imageWidth, imageWidth);
                    if (bytes is null) return null;

                    image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(bytes);
                    image.EndInit();
                }
                else
                {
                    image = new BitmapImage();
                    image.BeginInit();
                    image.DecodePixelWidth = imageWidth;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(fileFullName);
                    image.EndInit();
                }

                Cache.Add(Tuple.Create(fileFullName, fileCreationTimeUtc), image);

                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}