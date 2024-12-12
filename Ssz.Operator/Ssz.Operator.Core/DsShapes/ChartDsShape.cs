using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.MonitoredUndo;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class ChartDsShape : ControlDsShape
    {
        public enum AppearanceType
        {
            StackedChart = 0,
            PieChart = 1,
            RingChart = 2
        }

        #region private fields

        private AppearanceType _type;

        #endregion

        #region private functions

        private void OnDsChartItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, nameof(DsChartItemsCollection),
                DsChartItemsCollection, e);

            OnPropertyChanged(nameof(DsChartItemsCollection));
        }

        #endregion

        #region construction and destruction

        public ChartDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ChartDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 120;
            HeightInitial = 30;

            if (visualDesignMode) DsChartItemsCollection.CollectionChanged += OnDsChartItemsCollectionChanged;
            Type = AppearanceType.StackedChart;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Chart";

        public static readonly Guid DsShapeTypeGuid = new(@"AEA50CE8-DD33-423B-86EC-13E9DF4229DF");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override BrushDataBinding ForegroundInfo
        {
            get => base.ForegroundInfo;
            set => base.ForegroundInfo = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override HorizontalAlignment HorizontalContentAlignment
        {
            get => base.HorizontalContentAlignment;
            set => base.HorizontalContentAlignment = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override VerticalAlignment VerticalContentAlignment
        {
            get => base.VerticalContentAlignment;
            set => base.VerticalContentAlignment = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ChartDsShapeDsChartItemsCollection)]
        [Editor(typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        [NewItemTypes(typeof(DsChartItem))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // For XAML serialization of collections
        public ObservableCollection<DsChartItem> DsChartItemsCollection { get; } = new();

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ChartDsShapeAppearanceType)]
        public AppearanceType Type
        {
            get => _type;
            set => SetValue(ref _type, value);
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
                writer.Write(DsChartItemsCollection.ToList());
                writer.Write((int) Type);
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
                            List<DsChartItem> dsChartItemsCollection = reader.ReadList<DsChartItem>();
                            DsChartItemsCollection.Clear();
                            foreach (DsChartItem dsChartItem in dsChartItemsCollection)
                                DsChartItemsCollection.Add(dsChartItem);
                            Type = (AppearanceType) reader.ReadInt32();
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

            foreach (DsChartItem dsChartItem in DsChartItemsCollection)
                ItemHelper.RefreshForPropertyGrid(dsChartItem, container);

            OnPropertyChanged(nameof(DsChartItemsCollection));
        }

        #endregion
    }
}