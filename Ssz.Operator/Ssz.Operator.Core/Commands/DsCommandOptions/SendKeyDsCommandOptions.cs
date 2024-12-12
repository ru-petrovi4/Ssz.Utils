using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    public class SendKeyDsCommandOptions : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region construction and destruction

        public SendKeyDsCommandOptions()
        {
            Key = "";
        }

        #endregion

        public class KeysItemsSource : IItemsSource
        {
            #region public functions

            public ItemCollection GetValues()
            {
                var keys = new ItemCollection();
                foreach (var key in Enum.GetValues(typeof(Keys)).Cast<Keys>()
                    .OrderBy(k => (int) k)
                    .Where(k => (int) k > 0)
                    .Select(k => k.ToString())
                    .Distinct())
                    keys.Add(key);
                return keys;
            }

            #endregion
        }

        #region public functions

        [DsDisplayName(ResourceStrings.SendKeyDsCommandOptionsKey)]
        [LocalizedDescription(ResourceStrings.SendKeyDsCommandOptionsKeyDescription)]
        [ItemsSource(typeof(KeysItemsSource), true)]
        [PropertyOrder(1)]
        public string Key { get; set; }

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
                writer.Write(Key);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    try
                    {
                        Key = reader.ReadString();
                    }
                    catch (BlockEndingException)
                    {
                    }
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(Key,
                constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            Key = ConstantsHelper.ComputeValue(container,
                Key)!;
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public override string ToString()
        {
            return Key;
        }

        #endregion
    }
}