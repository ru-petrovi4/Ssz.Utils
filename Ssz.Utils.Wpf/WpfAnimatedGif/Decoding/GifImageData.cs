using System.IO;

namespace Ssz.Utils.Wpf.WpfAnimatedGif.Decoding
{
    internal class GifImageData
    {
        #region construction and destruction

        private GifImageData()
        {
        }

        #endregion

        #region public functions

        public byte LzwMinimumCodeSize { get; set; }
        public byte[] CompressedData { get; set; } = null!;

        #endregion

        #region internal functions

        internal static GifImageData ReadImageData(Stream stream, bool metadataOnly)
        {
            var imgData = new GifImageData();
            imgData.Read(stream, metadataOnly);
            return imgData;
        }

        #endregion

        #region private functions

        private void Read(Stream stream, bool metadataOnly)
        {
            LzwMinimumCodeSize = (byte) stream.ReadByte();
            CompressedData = GifHelpers.ReadDataBlocks(stream, metadataOnly)!;
        }

        #endregion
    }
}