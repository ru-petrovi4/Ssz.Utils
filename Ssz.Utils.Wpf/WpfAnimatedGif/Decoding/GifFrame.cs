using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ssz.Utils.Wpf.WpfAnimatedGif.Decoding
{
    internal class GifFrame : GifBlock
    {
        #region construction and destruction

        private GifFrame()
        {
        }

        #endregion

        #region public functions

        public GifImageDescriptor Descriptor { get; private set; } = null!;
        public GifColor[] LocalColorTable { get; private set; } = null!;
        public IList<GifExtension> Extensions { get; private set; } = null!;
        public GifImageData ImageData { get; private set; } = null!;

        #endregion

        #region internal functions

        internal static GifFrame ReadFrame(Stream stream, IEnumerable<GifExtension> controlExtensions, bool metadataOnly)
        {
            var frame = new GifFrame();

            frame.Read(stream, controlExtensions, metadataOnly);

            return frame;
        }

        internal const int ImageSeparator = 0x2C;

        internal override GifBlockKind Kind
        {
            get { return GifBlockKind.GraphicRendering; }
        }

        #endregion

        #region private functions

        private void Read(Stream stream, IEnumerable<GifExtension> controlExtensions, bool metadataOnly)
        {
            // Note: at this point, the Image Separator (0x2C) has already been read

            Descriptor = GifImageDescriptor.ReadImageDescriptor(stream);
            if (Descriptor.HasLocalColorTable)
            {
                LocalColorTable = GifHelpers.ReadColorTable(stream, Descriptor.LocalColorTableSize);
            }
            ImageData = GifImageData.ReadImageData(stream, metadataOnly);
            Extensions = controlExtensions.ToList().AsReadOnly();
        }

        #endregion
    }
}