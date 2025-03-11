using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Utils;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    public class StartProcessDsCommandOptions : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region construction and destruction

        public StartProcessDsCommandOptions()
        {
            Command = "";
            Arguments = "";
        }

        #endregion

        #region public functions

        [DsDisplayName(ResourceStrings.StartProcessDsCommandOptionsCommand)]
        [LocalizedDescription(ResourceStrings.StartProcessDsCommandOptionsCommandDescription)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(1)]
        public string Command { get; set; }

        [DsDisplayName(ResourceStrings.StartProcessDsCommandOptionsArguments)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(2)]
        public string Arguments { get; set; }

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
                writer.Write(Command);
                writer.Write(Arguments);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    try
                    {
                        Command = reader.ReadString();
                        Arguments = reader.ReadString();
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
            return @"start " + Command + @" " + Arguments;
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(Command,
                constants);
            ConstantsHelper.FindConstants(Arguments,
                constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            Command = ConstantsHelper.ComputeValue(container,
                Command)!;
            Arguments = ConstantsHelper.ComputeValue(container,
                Arguments)!;
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        #endregion
    }
}