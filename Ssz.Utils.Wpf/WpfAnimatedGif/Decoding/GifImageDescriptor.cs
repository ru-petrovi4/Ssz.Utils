using System;
using System.IO;

namespace Ssz.Utils.Wpf.WpfAnimatedGif.Decoding
{
    internal class GifImageDescriptor
    {
        #region construction and destruction

        private GifImageDescriptor()
        {
        }

        #endregion

        #region public functions

        public int Left { get; private set; }
        public int Top { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool HasLocalColorTable { get; private set; }
        public bool Interlace { get; private set; }
        public bool IsLocalColorTableSorted { get; private set; }
        public int LocalColorTableSize { get; private set; }

        #endregion

        #region internal functions

        internal static GifImageDescriptor ReadImageDescriptor(Stream stream)
        {
            var descriptor = new GifImageDescriptor();
            descriptor.Read(stream);
            return descriptor;
        }

        #endregion

        #region private functions

        private void Read(Stream stream)
        {
            var bytes = new byte[9];
            stream.ReadAll(bytes, 0, bytes.Length);
            Left = BitConverter.ToUInt16(bytes, 0);
            Top = BitConverter.ToUInt16(bytes, 2);
            Width = BitConverter.ToUInt16(bytes, 4);
            Height = BitConverter.ToUInt16(bytes, 6);
            byte packedFields = bytes[8];
            HasLocalColorTable = (packedFields & 0x80) != 0;
            Interlace = (packedFields & 0x40) != 0;
            IsLocalColorTableSorted = (packedFields & 0x20) != 0;
            LocalColorTableSize = 1 << ((packedFields & 0x07) + 1);
        }

        #endregion
    }
}