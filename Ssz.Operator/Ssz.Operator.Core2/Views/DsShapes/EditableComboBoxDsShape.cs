using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    public class EditableComboBoxDsShape : ControlDsShape
    {
        #region private functions

        private void MenuItemInfosCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, nameof(MenuItemInfosCollection),
                MenuItemInfosCollection, e);

            OnPropertyChanged(nameof(MenuItemInfosCollection));
        }

        #endregion

        #region construction and destruction

        public EditableComboBoxDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public EditableComboBoxDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 90;
            HeightInitial = 30;

            TextInfo = new TextDataBinding(visualDesignMode, loadXamlContent);
            IsEditableInfo = new BooleanDataBinding(visualDesignMode, loadXamlContent) {ConstValue = false};
            if (visualDesignMode) MenuItemInfosCollection.CollectionChanged += MenuItemInfosCollectionChanged;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "ComboBox Editable";
        public static readonly Guid DsShapeTypeGuid = new(@"4FFEAC5A-B9B0-44AF-AE4B-926F6BCEF102");

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
        [DsDisplayName(ResourceStrings.ComboBoxDsShapeTextInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public TextDataBinding TextInfo
        {
            get => _textInfo;
            set => SetValue(ref _textInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ComboBoxDsShapeIsEditableInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        public BooleanDataBinding IsEditableInfo
        {
            get => _isEditableInfo;
            set => SetValue(ref _isEditableInfo, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ComboBoxDsShapeMenuItemInfosCollection)]
        //[Editor(//typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            //typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        //[NewItemTypes(typeof(SimpleMenuItemInfo), typeof(SeparatorMenuItemInfo))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public ObservableCollection<ICloneable> MenuItemInfosCollection { get; } = new();

        [Browsable(false)]
        public object MenuItemInfosArray
        {
            get => new ArrayList(MenuItemInfosCollection);
            set
            {
                MenuItemInfosCollection.Clear();
                foreach (MenuItemInfo menuItemInfo in ((ArrayList) value).OfType<MenuItemInfo>())
                    MenuItemInfosCollection.Add(menuItemInfo);
            }
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

                writer.Write(TextInfo, context);
                writer.Write(IsEditableInfo, context);
                writer.Write(MenuItemInfosCollection.ToList());
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

                        reader.ReadOwnedData(TextInfo, context);
                        reader.ReadOwnedData(IsEditableInfo, context);
                        List<MenuItemInfo> menuItemInfosCollection = reader.ReadList<MenuItemInfo>();
                        MenuItemInfosCollection.Clear();
                        foreach (MenuItemInfo menuItemInfo in menuItemInfosCollection)
                            MenuItemInfosCollection.Add(menuItemInfo);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        #region private fields

        private TextDataBinding _textInfo = null!;
        private BooleanDataBinding _isEditableInfo = null!;

        #endregion
    }
}