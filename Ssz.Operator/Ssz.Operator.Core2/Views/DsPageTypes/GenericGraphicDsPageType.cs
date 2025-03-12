using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsPageTypes
{
    public class GenericGraphicDsPageType : DsPageTypeBase
    {
        #region construction and destruction

        public GenericGraphicDsPageType()
        {
            FramesDsPageFileRelativePath = @"";
            FrameInitializationInfosCollection = new List<FrameInitializationInfo>();
        }

        #endregion

        #region public functions

        public static readonly Guid TypeGuid = new(@"E2A64214-61E1-4E01-A2AE-D77868B40625");

        public override Guid Guid => TypeGuid;

        public override string Name => @"Generic DsPage";

        public override string Desc => Resources.GenericGraphicDsPageType_Desc;

        public override bool IsFaceplate => false;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.GenericGraphicDsPageType_FramesDsPageFileRelativePath)]
        [LocalizedDescription(ResourceStrings.GenericGraphicDsPageType_FramesDsPageFileRelativePath_Description)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(1)]
        public string FramesDsPageFileRelativePath { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.GenericGraphicDsPageType_FrameInitializationInfosCollection)]
        //[Editor(//typeof(SameTypeCloneableObjectsListTypeEditor), //typeof(SameTypeCloneableObjectsListTypeEditor))]
        //[PropertyOrder(2)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<FrameInitializationInfo> FrameInitializationInfosCollection { get; set; }

        [Browsable(false)]
        public object? FrameInitializationInfosArray
        {
            get => new ArrayList(FrameInitializationInfosCollection);
            set
            {
                if (value is null) FrameInitializationInfosCollection = new List<FrameInitializationInfo>();
                else FrameInitializationInfosCollection = ((ArrayList)value).OfType<FrameInitializationInfo>().ToList();
            }
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(FramesDsPageFileRelativePath);
                writer.Write(FrameInitializationInfosCollection);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    try
                    {
                        FramesDsPageFileRelativePath = reader.ReadString();
                        FrameInitializationInfosCollection = reader.ReadList<FrameInitializationInfo>();
                    }
                    catch (BlockEndingException)
                    {
                    }
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        #endregion
    }    
}