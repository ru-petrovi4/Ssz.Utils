using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Markup;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{    
    public class CloseAllFaceplatesDsCommandOptions : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region public functions
        
        [DsDisplayName(ResourceStrings.CloseAllFaceplatesDsCommandOptions_PlayWindowClassInfo)]
        [LocalizedDescription(ResourceStrings.CloseAllFaceplatesDsCommandOptions_PlayWindowClassInfo_Description)]
        [ExpandableObject]
        [Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        [PropertyOrder(1)]
        public PlayWindowClassInfo PlayWindowClassInfo { get; set; } = new();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            //RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(PlayWindowClassInfo.WindowCategory,
                constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            PlayWindowClassInfo.WindowCategory = ConstantsHelper.ComputeValue(container, PlayWindowClassInfo.WindowCategory)!;
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.Write(PlayWindowClassInfo, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        using (Block block2 = reader.EnterBlock())
                        {
                            PlayWindowClassInfo.WindowCategory = reader.ReadString();
                        }
                        break;
                    case 2:
                        reader.ReadOwnedData(PlayWindowClassInfo, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}