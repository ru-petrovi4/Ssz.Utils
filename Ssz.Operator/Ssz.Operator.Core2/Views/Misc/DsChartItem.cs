using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class DsChartItem : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region construction and destruction

        public DsChartItem() // For XAML serialization
            : this(true)
        {
        }

        public DsChartItem(bool visualDesignMode)
        {
            HeaderInfo = new TextDataBinding(visualDesignMode, true);
            ValueInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 0.0};
            DsBrush = new BrushDataBinding(visualDesignMode, true)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.Blue
                }
            };
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.DsChartItemHeaderInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [DefaultValue(typeof(TextDataBinding), @"")] // For XAML serialization
        public TextDataBinding HeaderInfo { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.DsChartItemValueInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public DoubleDataBinding ValueInfo { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.DsChartItemDsBrush)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        public BrushDataBinding DsBrush { get; set; }

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
                writer.Write(HeaderInfo, context);
                writer.Write(ValueInfo, context);
                writer.Write(DsBrush, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            reader.ReadOwnedData(HeaderInfo, context);
                            reader.ReadOwnedData(ValueInfo, context);
                            reader.ReadOwnedData(DsBrush, context);
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
            HeaderInfo.FindConstants(constants);
            ValueInfo.FindConstants(constants);
            DsBrush.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ItemHelper.ReplaceConstants(HeaderInfo, container);
            ItemHelper.ReplaceConstants(ValueInfo, container);
            ItemHelper.ReplaceConstants(DsBrush, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.RefreshForPropertyGrid(HeaderInfo, container);
            ItemHelper.RefreshForPropertyGrid(ValueInfo, container);
            ItemHelper.RefreshForPropertyGrid(DsBrush, container);
        }

        public override string ToString()
        {
            return HeaderInfo.ToString();
        }

        #endregion
    }
}