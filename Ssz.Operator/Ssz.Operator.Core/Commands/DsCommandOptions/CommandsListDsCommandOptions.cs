using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Utils;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [ContentProperty(@"DsCommandItemsArray")]
    // For XAML serialization. Content property must be of type object or string.
    public class CommandsListDsCommandOptions : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region private fields

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.CommandsListDsCommandOptionsDsCommandsList)]
        [Editor(typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        [NewItemTypes(typeof(DsCommand))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        // For XAML serialization of collections
        public List<DsCommand> DsCommandsList { get; private set; } = new();

        [Browsable(false)]
        public object DsCommandItemsArray
        {
            get { return new ArrayList(DsCommandsList.Select(ci => new DsCommandItem {DsCommand = ci}).ToArray()); }
            set
            {
                DsCommandsList = ((ArrayList) value).OfType<DsCommandItem>().Select(i => i.DsCommand)
                    .OfType<DsCommand>().ToList();
            }
        }

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
                writer.Write(DsCommandsList);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    DsCommandsList = reader.ReadList<DsCommand>();
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            foreach (DsCommand dsCommand in DsCommandsList) 
                dsCommand.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            foreach (DsCommand dsCommand in DsCommandsList) 
                dsCommand.ReplaceConstants(container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public override string ToString()
        {
            return @"Commands List...";
        }

        #endregion
    }


    public class DsCommandItem
    {
        public DsCommand? DsCommand { get; set; }
    }
}