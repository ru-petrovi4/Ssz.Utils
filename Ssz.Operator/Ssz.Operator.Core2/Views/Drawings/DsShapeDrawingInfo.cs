using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Drawings
{
    public class DsShapeDrawingInfo : DrawingInfo
    {
        #region construction and destruction

        public DsShapeDrawingInfo(string fileFullName) :
            base(fileFullName)
        {
            ShortBytesSHA512Hash = new byte[32];
        }

        public DsShapeDrawingInfo(string fileFullName, Guid drawingGuid, string desc,
            string group, byte[]? previewImageBytes, DateTime deserializedVersionDateTime,
            DsConstant[] dsConstantsCollection,
            int mark,
            List<GuidAndName> actuallyUsedAddonsInfo, byte[] shortBytesSHA512Hash, int shortBytesLength)
            : base(fileFullName, drawingGuid, desc,
                group, previewImageBytes, deserializedVersionDateTime,
                dsConstantsCollection,
                mark,
                actuallyUsedAddonsInfo)
        {
            if (shortBytesSHA512Hash.Length != 64) throw new ArgumentException(@"shortBytesSHA512Hash.Length != 64");

            ShortBytesSHA512Hash = shortBytesSHA512Hash;
            ShortBytesLength = shortBytesLength;
        }

        #endregion

        #region public functions

        public byte[] ShortBytesSHA512Hash { get; private set; }

        public int ShortBytesLength { get; private set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(3))
            {
                writer.Write(SerializationVersionDateTime);
                writer.Write(Guid);

                writer.Write(ActuallyUsedAddonsInfo);

                writer.Write(Desc);
                writer.Write(Group);
                writer.WriteArray(DsConstantsCollection.ToArray());

                writer.WriteArray(PreviewImageBytes);
                writer.Write(Mark);

                writer.Write(ShortBytesSHA512Hash);
                writer.Write(ShortBytesLength);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 3:
                        SerializationVersionDateTime = reader.ReadDateTime();
                        Guid = reader.ReadGuid();

                        ActuallyUsedAddonsInfo = reader.ReadList<GuidAndName>();

                        Desc = reader.ReadString();
                        Group = reader.ReadString();
                        DsConstantsCollection = new ObservableCollection<DsConstant>(reader.ReadArray<DsConstant>());

                        PreviewImageBytes = reader.ReadArray<byte>();
                        Mark = reader.ReadInt32();

                        ShortBytesSHA512Hash = reader.ReadBytes(32);
                        ShortBytesLength = reader.ReadInt32();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override void DeserializeGuidOnly(SerializationReader reader)
        {
            reader.EnterBlock();

            SerializationVersionDateTime = reader.ReadDateTime();
            Guid = reader.ReadGuid();
        }

        #endregion
    }
}