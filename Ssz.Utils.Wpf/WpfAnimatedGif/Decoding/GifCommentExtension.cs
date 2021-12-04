using System.IO;
using System.Text;

namespace Ssz.Utils.Wpf.WpfAnimatedGif.Decoding
{
    internal class GifCommentExtension : GifExtension
    {
        #region construction and destruction

        private GifCommentExtension()
        {
        }

        #endregion

        #region public functions

        public string Text { get; private set; } = @"";

        #endregion

        #region internal functions

        internal static GifCommentExtension ReadComment(Stream stream)
        {
            var comment = new GifCommentExtension();
            comment.Read(stream);
            return comment;
        }

        internal const int ExtensionLabel = 0xFE;

        internal override GifBlockKind Kind
        {
            get { return GifBlockKind.SpecialPurpose; }
        }

        #endregion

        #region private functions

        private void Read(Stream stream)
        {
            // Note: at this point, the label (0xFE) has already been read

            byte[]? bytes = GifHelpers.ReadDataBlocks(stream, false);
            if (bytes != null)
                Text = Encoding.ASCII.GetString(bytes);
        }

        #endregion
    }
}