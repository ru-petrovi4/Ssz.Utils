using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Markup;
using Ssz.Operator.Core.Constants;


using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using OwnedDataSerializableAndCloneable = Ssz.Operator.Core.Utils.OwnedDataSerializableAndCloneable;

namespace Ssz.Operator.Core
{
    [TypeConverter(typeof(DsUIElementPropertyTypeConverter))]
    [ValueSerializer(typeof(DsUIElementPropertyValueSerializer))]
    public class DsUIElementProperty : OwnedDataSerializableAndCloneable, IConstantsHolder
    {
        #region construction and destruction

        public DsUIElementProperty() :
            this(true, true) // For XAML serialization
        {
        }

        public DsUIElementProperty(bool visualDesignMode, bool loadXamlContent)
        {
            VisualDesignMode = visualDesignMode;
            LoadXamlContent = loadXamlContent;

            TypeString = DsUIElementPropertySupplier.DefaultTypeString;
        }

        #endregion

        #region public functions

        public string TypeString { get; set; }

        [Searchable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string CustomXamlString { get; set; } = "";

        [Searchable(false)]
        public DsXaml? CustomXaml
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CustomXamlString)) return null;
                return new DsXaml {Xaml = CustomXamlString};
            }
            set
            {
                if (LoadXamlContent)
                {
                    if (value is null)
                        CustomXamlString = @"";
                    else
                        CustomXamlString = value.Xaml;
                }
            }
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(TypeString);
                writer.Write(CustomXamlString);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    try
                    {
                        TypeString = reader.ReadString();
                        if (LoadXamlContent) CustomXamlString = reader.ReadString();
                        else reader.SkipString();
                    }
                    catch (BlockEndingException)
                    {
                    }
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        public override string ToString()
        {
            return TypeString;
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(CustomXamlString, constants);
        }

        #endregion

        #region protected functions

        protected bool LoadXamlContent { get; }

        protected bool VisualDesignMode { get; }

        #endregion
    }
}