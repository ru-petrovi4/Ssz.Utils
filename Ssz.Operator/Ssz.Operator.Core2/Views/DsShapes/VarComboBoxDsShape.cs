using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Markup;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.MonitoredUndo;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    //[ContentProperty(@"MenuItemInfosArray")]
    // For XAML serialization. Content property must be of type object or string.
    public class VarComboBoxDsShape : ControlDsShape
    {
        #region construction and destruction

        public VarComboBoxDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public VarComboBoxDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 90;
            HeightInitial = 30;

            SelectedIndexInfo = new Int32DataBinding(visualDesignMode, loadXamlContent);
            MenuItemsInfo = new TextDataBinding(visualDesignMode, loadXamlContent) { ConstValue = "item1,item2" };
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "VarComboBox";
        public static readonly Guid DsShapeTypeGuid = new(@"E28BE173-AD08-45E4-BCDC-DC726FE70565");

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeStyleInfo)]
        //[Editor(////typeof(DsUIElementPropertyTypeEditor<ComboBoxStyleInfoSupplier>),
            ////typeof(DsUIElementPropertyTypeEditor<ComboBoxStyleInfoSupplier>))]
        // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ComboBoxDsShapeSelectedIndexInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public Int32DataBinding SelectedIndexInfo
        {
            get => _selectedIndexInfo;
            set => SetValue(ref _selectedIndexInfo, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.VarComboBoxDsShape_MenuItemsInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public TextDataBinding MenuItemsInfo
        {
            get => _menuItemsInfo;
            set => SetValue(ref _menuItemsInfo, value);
        }

        public override string? GetStyleXamlString(IDsContainer? container)
        {
            return new ComboBoxStyleInfoSupplier().GetPropertyXamlString(base.StyleInfo, container);
        }

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write(SelectedIndexInfo, context);
                writer.Write(MenuItemsInfo, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        base.DeserializeOwnedDataAsync(reader, context);

                        reader.ReadOwnedData(SelectedIndexInfo, context);
                        reader.ReadOwnedData(MenuItemsInfo, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        #region private fields

        private Int32DataBinding _selectedIndexInfo = null!;
        private TextDataBinding _menuItemsInfo = null!;

        #endregion
    }
}