using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Utils;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    public class SetValueDsCommandOptions : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region private fields

        private TextDataBinding _valueInfo = null!;

        #endregion

        #region construction and destruction

        public SetValueDsCommandOptions()
        {
            _valueInfo = new TextDataBinding(false, true);
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.ValueDataBindingBaseDataBindingItemsCollection)]
        //[Editor(typeof(DataBindingItemsCollectionTypeEditor), typeof(DataBindingItemsCollectionTypeEditor))]
        //[PropertyOrder(1)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public ObservableCollection<DataBindingItem> DataBindingItemsCollection => ValueInfo.DataBindingItemsCollection;

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.ValueDataBindingBaseConverter)]
        //[Editor(typeof(OneWayToSourceStructConverterTypeEditor), typeof(OneWayToSourceStructConverterTypeEditor))]
        //[PropertyOrder(2)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public ValueConverterBase? Converter
        {
            get => ValueInfo.Converter;
            set => ValueInfo.Converter = value;
        }

        [Browsable(false)]
        public TextDataBinding ValueInfo
        {
            get => _valueInfo;
            set
            {
                if (Equals(value, _valueInfo)) return;
                _valueInfo = value;
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
            using (writer.EnterBlock(3))
            {
                writer.Write(ValueInfo, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 3:
                        try
                        {
                            reader.ReadOwnedData(ValueInfo, context);
                        }
                        catch (BlockEndingException)
                        {
                        }

                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            ValueInfo.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ItemHelper.ReplaceConstants(ValueInfo, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public override string ToString()
        {
            if (ValueInfo.DataBindingItemsCollection.Count == 0) return Resources.NotDefined;
            var localizedConverter = ValueInfo.Converter as LocalizedConverter;
            if (localizedConverter is null || localizedConverter.UiToDataSourceStatements.Count == 0)
                return Resources.NotDefined;

            var dataSourceAndValueList = new List<string>(ValueInfo.DataBindingItemsCollection.Count);
            foreach (TextStatement statement in localizedConverter.UiToDataSourceStatements)
                if (statement.ParamNum >= 0 && statement.ParamNum < ValueInfo.DataBindingItemsCollection.Count &&
                    statement.Condition.ExpressionString.ToLowerInvariant() == @"true")
                    dataSourceAndValueList.Add(ValueInfo.DataBindingItemsCollection[statement.ParamNum] + @" = " +
                                               statement.Value.ExpressionString);
                else
                    return string.Join("; ", ValueInfo.DataBindingItemsCollection);
            return string.Join("; ", dataSourceAndValueList);
        }

        #endregion
    }
}