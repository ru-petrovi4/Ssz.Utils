using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.MonitoredUndo;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    [ContentProperty(@"MenuItemInfosArray")]
    // For XAML serialization. Content property must be of type object or string.
    public class ContextMenuDsShape : DsShapeBase
    {
        #region private functions

        private void MenuItemInfosCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, nameof(MenuItemInfosCollection),
                MenuItemInfosCollection, e);

            OnPropertyChanged(nameof(MenuItemInfosCollection));
        }

        #endregion

        #region private fields

        #endregion

        #region construction and destruction

        public ContextMenuDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ContextMenuDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 30;
            HeightInitial = 30;

            if (visualDesignMode) MenuItemInfosCollection.CollectionChanged += MenuItemInfosCollectionChanged;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Context Menu";

        public static readonly Guid DsShapeTypeGuid = new(@"0735FB52-94C1-43FB-AEE7-9FF45DFFC08E");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override DoubleDataBinding OpacityInfo
        {
            get => base.OpacityInfo;
            set => base.OpacityInfo = value;
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ContextMenuDsShapeMenuItemInfosCollection)]
        [Editor(typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        [NewItemTypes(typeof(MenuItemInfo), typeof(SeparatorMenuItemInfo))]
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
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(1))
            {
                writer.Write(MenuItemInfosCollection.ToList());
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            List<MenuItemInfo> menuItemInfosCollection = reader.ReadList<MenuItemInfo>();
                            MenuItemInfosCollection.Clear();
                            foreach (MenuItemInfo menuItemInfo in menuItemInfosCollection)
                                MenuItemInfosCollection.Add(menuItemInfo);
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

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            foreach (MenuItemInfo menuItemInfo in MenuItemInfosCollection)
                ItemHelper.RefreshForPropertyGrid(menuItemInfo, container);

            OnPropertyChanged(nameof(MenuItemInfosCollection));
        }

        #endregion
    }
}