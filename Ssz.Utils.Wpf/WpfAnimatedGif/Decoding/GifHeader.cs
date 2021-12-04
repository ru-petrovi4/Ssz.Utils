using System.IO;

namespace Ssz.Utils.Wpf.WpfAnimatedGif.Decoding
{
    internal class GifHeader : GifBlock
    {
        #region construction and destruction

        private GifHeader()
        {
        }

        #endregion

        #region public functions

        public string Signature { get; private set; } = @"";
        public string Version { get; private set; } = @"";
        public GifLogicalScreenDescriptor LogicalScreenDescriptor { get; private set; } = null!;

        #endregion

        #region internal functions

        internal static GifHeader ReadHeader(Stream stream)
        {
            var header = new GifHeader();
            header.Read(stream);
            return header;
        }

        internal override GifBlockKind Kind
        {
            get { return GifBlockKind.Other; }
        }

        #endregion

        #region private functions

        private void Read(Stream stream)
        {
            Signature = GifHelpers.ReadString(stream, 3);
            if (Signature != "GIF")
                throw GifHelpers.InvalidSignatureException(Signature);
            Version = GifHelpers.ReadString(stream, 3);
            if (Version != "87a" && Version != "89a")
                throw GifHelpers.UnsupportedVersionException(Version);
            LogicalScreenDescriptor = GifLogicalScreenDescriptor.ReadLogicalScreenDescriptor(stream);
        }

        #endregion
    }
}