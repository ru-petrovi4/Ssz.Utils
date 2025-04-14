using Ssz.Operator.Core;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsShapes;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Properties;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Markup;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.MonitoredUndo;

namespace Ssz.Operator.Core.DsShapes
{
    //[ContentProperty(nameof(TabItemInfosArray))]
    public class TabDsShape : ControlDsShape
    {
        #region construction and destruction

        public TabDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public TabDsShape(bool visualDesignerMode, bool loadXamlContent)
            : base(visualDesignerMode, loadXamlContent)
        {
            WidthInitial = 100;
            HeightInitial = 100;

            if (visualDesignerMode) _tabItemInfosCollection.CollectionChanged += TabItemInfosCollectionChanged;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Tab Control";
        public static readonly Guid DsShapeTypeGuid = new Guid(@"B26FB25F-CB2E-408A-88E3-1DAED9A6E7B6");

        [DsCategory(ResourceStrings.AppearanceCategory),
         DsDisplayName(ResourceStrings.ControlDsShapeStyleInfo)]
        //[Editor(//typeof(DsUIElementPropertyTypeEditor<TabStyleInfoSupplier>),
            //typeof(DsUIElementPropertyTypeEditor<TabStyleInfoSupplier>))]
        // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get { return base.StyleInfo; }
            set { base.StyleInfo = value; }
        }

        [DsCategory(ResourceStrings.MainCategory),
         DsDisplayName(ResourceStrings.TabDsShape_TabItemInfosCollection)]
        //[Editor(//typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            //typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        //[NewItemTypes(typeof(TabItemInfo))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public ObservableCollection<ICloneable> TabItemInfosCollection
        {
            get { return _tabItemInfosCollection; }
        }

        [Browsable(false)]
        public object TabItemInfosArray
        {
            get { return new ArrayList(_tabItemInfosCollection); }
            set
            {
                _tabItemInfosCollection.Clear();
                foreach (TabItemInfo tabItemInfo in ((ArrayList)value).OfType<TabItemInfo>())
                {
                    _tabItemInfosCollection.Add(tabItemInfo);
                }
            }
        }

        public override string? GetStyleXamlString(IDsContainer? container)
        {
            return (new TabStyleInfoSupplier()).GetPropertyXamlString(base.StyleInfo, container);
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

                writer.Write(TabItemInfosCollection.ToList());
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            base.DeserializeOwnedData(reader, context);
                            
                            List<TabItemInfo> tabItemInfosCollection = reader.ReadList<TabItemInfo>();
                            TabItemInfosCollection.Clear();
                            foreach (TabItemInfo tabItemInfo in tabItemInfosCollection)
                            {
                                TabItemInfosCollection.Add(tabItemInfo);
                            }
                            break;
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

        #endregion

        #region private functions

        private void TabItemInfosCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, nameof(TabItemInfosCollection),
                    _tabItemInfosCollection, e);

            OnPropertyChanged(nameof(TabItemInfosCollection));
        }

        #endregion

        #region private fields

        private readonly ObservableCollection<ICloneable> _tabItemInfosCollection =
            new ObservableCollection<ICloneable>();

        #endregion
    }
}
