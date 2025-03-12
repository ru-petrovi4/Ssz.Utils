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
    public class ComboBoxDsShape : ControlDsShape
    {
        #region private fields

        private Int32DataBinding _selectedIndexInfo = null!;

        #endregion

        #region private functions

        private void MenuItemInfosCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, nameof(MenuItemInfosCollection),
                MenuItemInfosCollection, e);

            OnPropertyChanged(nameof(MenuItemInfosCollection));
        }

        #endregion

        #region construction and destruction

        public ComboBoxDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ComboBoxDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 90;
            HeightInitial = 30;

            SelectedIndexInfo = new Int32DataBinding(visualDesignMode, loadXamlContent);
            if (visualDesignMode) MenuItemInfosCollection.CollectionChanged += MenuItemInfosCollectionChanged;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "ComboBox";
        public static readonly Guid DsShapeTypeGuid = new(@"D3E3958B-F3DE-4257-BF38-41B084A1E6A4");

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

                writer.Write(SelectedIndexInfo, context);
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

                        reader.ReadOwnedData(SelectedIndexInfo, context);
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
    }
}