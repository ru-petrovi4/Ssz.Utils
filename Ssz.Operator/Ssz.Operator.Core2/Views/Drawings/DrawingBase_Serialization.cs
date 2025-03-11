using System;
using System.IO;
using System.Text;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Drawings
{
    public abstract partial class DrawingBase
    {
        #region public functions

        public static readonly DateTime CurrentSerializationVersionDateTime = new(2021, 07, 07, 00, 00, 00);

        /// <summary>
        ///     Returns -1 if XML stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static int GetStreamInfo(Stream stream)
        {
            try
            {
                var buffer = new byte[16];
                int readedCount = stream.Read(buffer, 0, 16);
                string str = Encoding.Unicode.GetString(buffer, 0, readedCount);
                if (str.Contains("<?xml")) return -1;
                str = Encoding.UTF8.GetString(buffer, 0, 16);
                if (str.Contains("<?xml")) return -1;
                return BitConverter.ToInt32(buffer, 0);
            }
            finally
            {
                stream.Position = 0;
            }
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(Width);
                writer.Write(Height);
                writer.Write(Settings, context);
                writer.WriteDsShapes(DsShapes, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        Width = reader.ReadDouble();
                        Height = reader.ReadDouble();
                        reader.ReadOwnedData(Settings, context);
                        DsShapes = reader.ReadDsShapes(context, VisualDesignMode, LoadXamlContent);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}