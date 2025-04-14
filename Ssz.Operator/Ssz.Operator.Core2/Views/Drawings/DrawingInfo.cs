using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;

namespace Ssz.Operator.Core.Drawings
{
    public abstract class DrawingInfo : EntityInfo, IOwnedDataSerializable, IDsContainer
    {
        #region construction and destruction

        public DrawingInfo(string fileFullName)
            : base(Path.GetFileNameWithoutExtension(fileFullName))
        {
            FileFullName = fileFullName;
            DsConstantsCollection = new ObservableCollection<DsConstant>();
            ActuallyUsedAddonsInfo = new List<GuidAndName>();
        }

        public DrawingInfo(string fileFullName, Guid drawingGuid, string desc,
            string group, byte[]? previewImageBytes, DateTime serializationVersionDateTime,
            DsConstant[] dsConstantsCollection,
            int mark,
            List<GuidAndName> actuallyUsedAddonsInfo)
            : base(Path.GetFileNameWithoutExtension(fileFullName), drawingGuid, desc, group, previewImageBytes)
        {
            FileFullName = fileFullName;
            SerializationVersionDateTime = serializationVersionDateTime;
            DsConstantsCollection = new ObservableCollection<DsConstant>(dsConstantsCollection);
            Mark = mark;
            ActuallyUsedAddonsInfo = actuallyUsedAddonsInfo;
        }

        #endregion

        #region public functions

        public string FileFullName { get; }

        public string FileName => Path.GetFileName(FileFullName);

        public DateTime SerializationVersionDateTime { get; protected set; }

        public int Mark { get; set; }

        public ObservableCollection<DsConstant> DsConstantsCollection { get; protected set; }        

        public virtual DsConstant[]? HiddenDsConstantsCollection => null;

        public DsShapeBase[] DsShapes
        {
            get => new DsShapeBase[0];
            set
            {
            }
        }

        public IPlayWindowBase? PlayWindow => null;

        public IDsItem? ParentItem { get; set; }        

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public List<GuidAndName> ActuallyUsedAddonsInfo { get; protected set; }

        public abstract void SerializeOwnedData(SerializationWriter writer, object? context);

        public abstract void DeserializeOwnedData(SerializationReader reader, object? context);

        public abstract void DeserializeGuidOnly(SerializationReader reader);

        public void FindConstants(HashSet<string> constants)
        {
            throw new NotImplementedException();
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            throw new NotImplementedException();
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {            
        }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(this);
        }

        public void EndEditInPropertyGrid()
        {
        }

        #endregion
    }

    public static class DrawingInfoExtensions
    {
        #region public functions

        public static string GetFileRelativePath(this DrawingInfo drawingInfo)
        {
            return DsProject.Instance.GetFileRelativePath(drawingInfo.FileFullName);
        }

        #endregion
    }
}


//public virtual void DeserializeGuidOnly(SerializationReader reader,
//            out DateTime serializationVersionDateTime,
//            out List<GuidAndName> actuallyUsedAddonsInfo,
//            out Guid guid)
//{
//    using (Block block = reader.EnterBlock())
//    {
//        switch (block.Version)
//        {
//            case 1:
//                serializationVersionDateTime = reader.ReadDateTime();
//                actuallyUsedAddonsInfo = reader.ReadList<GuidAndName>();
//                guid = reader.ReadGuid();

//                break;
//            default:
//                throw new BlockUnsupportedVersionException();
//        }
//    }
//}