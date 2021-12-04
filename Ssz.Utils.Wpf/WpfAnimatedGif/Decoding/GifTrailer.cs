namespace Ssz.Utils.Wpf.WpfAnimatedGif.Decoding
{
    internal class GifTrailer : GifBlock
    {
        #region construction and destruction

        private GifTrailer()
        {
        }

        #endregion

        #region internal functions

        internal static GifTrailer ReadTrailer()
        {
            return new GifTrailer();
        }

        internal const int TrailerByte = 0x3B;

        internal override GifBlockKind Kind
        {
            get { return GifBlockKind.Other; }
        }

        #endregion
    }
}