using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;

namespace Ssz.Operator.Core.Drawings
{
    public class DsPageDrawingInfo : DrawingInfo
    {
        #region construction and destruction

        public DsPageDrawingInfo(string fileFullName) :
            base(fileFullName)
        {
            DsPageTypeInfo = new GuidAndName();
        }

        public DsPageDrawingInfo(string fileFullName, Guid drawingGuid, string desc,
            string group, byte[]? previewImageBytes, DateTime deserializedVersionDateTime,
            DsConstant[] dsConstantsCollection,
            int mark,
            List<GuidAndName> actuallyUsedAddonsInfo,
            bool excludeFromTagSearch,
            GuidAndName dsPageTypeInfo, DsPageTypeBase? dsPageTypeObject)
            : base(fileFullName, drawingGuid, desc,
                group, previewImageBytes, deserializedVersionDateTime,
                dsConstantsCollection,
                mark,
                actuallyUsedAddonsInfo)
        {
            ExcludeFromTagSearch = excludeFromTagSearch;
            DsPageTypeInfo = dsPageTypeInfo;
            DsPageTypeObject = dsPageTypeObject;
        }

        #endregion

        #region public functions

        public bool ExcludeFromTagSearch { get; private set; }

        public GuidAndName DsPageTypeInfo { get; }

        public DsPageTypeBase? DsPageTypeObject { get; private set; }

        public bool IsFaceplate
        {
            get
            {
                if (DsPageTypeObject is null) return false;
                return DsPageTypeObject.IsFaceplate;
            }
        }

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

                writer.Write(ExcludeFromTagSearch);
                writer.Write(DsPageTypeInfo, context);
                writer.WriteNullableOwnedData(DsPageTypeObject, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 2:
                        SerializationVersionDateTime = reader.ReadDateTime();
                        Guid = reader.ReadGuid();

                        ActuallyUsedAddonsInfo = reader.ReadList<GuidAndName>();

                        Desc = reader.ReadString();
                        Group = reader.ReadString();
                        DsConstantsCollection = new ObservableCollection<DsConstant>(reader.ReadArray<DsConstant>());

                        PreviewImageBytes = reader.ReadArray<byte>();
                        Mark = reader.ReadInt32();

                        reader.ReadOwnedData(DsPageTypeInfo, context);
                        DsPageTypeObject = AddonsManager.NewDsPageTypeObject(DsPageTypeInfo.Guid);
                        reader.ReadNullableOwnedData(DsPageTypeObject, context);
                        break;
                    case 3:
                        SerializationVersionDateTime = reader.ReadDateTime();
                        Guid = reader.ReadGuid();

                        ActuallyUsedAddonsInfo = reader.ReadList<GuidAndName>();

                        Desc = reader.ReadString();
                        Group = reader.ReadString();
                        DsConstantsCollection = new ObservableCollection<DsConstant>(reader.ReadArray<DsConstant>());

                        PreviewImageBytes = reader.ReadArray<byte>();
                        Mark = reader.ReadInt32();

                        ExcludeFromTagSearch = reader.ReadBoolean();
                        reader.ReadOwnedData(DsPageTypeInfo, context);
                        DsPageTypeObject = AddonsManager.NewDsPageTypeObject(DsPageTypeInfo.Guid);
                        reader.ReadNullableOwnedData(DsPageTypeObject, context);
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