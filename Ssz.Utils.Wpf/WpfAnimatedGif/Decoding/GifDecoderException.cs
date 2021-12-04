using System;
using System.Runtime.Serialization;

namespace Ssz.Utils.Wpf.WpfAnimatedGif.Decoding
{
    [Serializable]
    internal class GifDecoderException : Exception
    {
        #region construction and destruction

        internal GifDecoderException()
        {
        }

        internal GifDecoderException(string message) : base(message)
        {
        }

        internal GifDecoderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected GifDecoderException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}