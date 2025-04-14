using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Markup;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<GenericDsCommandOptions>))]
    //[ValueSerializer(typeof(NameValueCollectionValueSerializer<GenericDsCommandOptions>))]
    public class GenericDsCommandOptions : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region public functions

        [DsDisplayName(ResourceStrings.GenericDsCommandOptionsParamsString)]
        public virtual string ParamsString { get; set; } = "";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(ParamsString);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        ParamsString = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override string ToString()
        {
            return ParamsString ?? @"";
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(ParamsString,
                constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            ParamsString = ConstantsHelper.ComputeValue(container, ParamsString)!;
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        #endregion
    }
}