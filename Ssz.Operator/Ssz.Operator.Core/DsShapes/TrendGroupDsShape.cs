using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.DsShapes.Trends;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.MonitoredUndo;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class TrendGroupDsShape : DsShapeBase
    {
        #region construction and destruction

        public TrendGroupDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public TrendGroupDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 600;
            HeightInitial = 500;

            Background = new SolidDsBrush(Colors.DarkGray);
            ChartBackground = new SolidDsBrush(Colors.Black);
            ChartGridBrush = new SolidDsBrush(Colors.DimGray);
            ChartAxisBrush = new SolidDsBrush(Colors.White);
            TrendsInfoTableVisibility = true;
            TrendsTuningVisibility = true;
            TrendsAxisXVisibility = true;
            TrendsAxisYVisibility = true;
            TrendsScrollbarsVisibility = true;

            if (visualDesignMode) 
                DsTrendItemsCollection.CollectionChanged += DsTrendItemsCollectionOnChanged;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Trend Group";
        public static readonly Guid DsShapeTypeGuid = new(@"7B206461-896D-4950-ABE2-672CA9ACCFE2");

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeDsTrendItemsCollection)]
        [Editor(typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        [NewItemTypes(typeof(DsTrendItem))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // For XAML serialization of collections
        public ObservableCollection<DsTrendItem> DsTrendItemsCollection { get; } = new();

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeBackground)]
        [PropertyOrder(1)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public DsBrushBase Background
        {
            get => _background;
            set => SetValue(ref _background, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeChartBackground)]
        [PropertyOrder(2)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public DsBrushBase ChartBackground
        {
            get => _chartBackground;
            set => SetValue(ref _chartBackground, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeChartGridBrush)]
        [PropertyOrder(3)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public DsBrushBase ChartGridBrush
        {
            get => _chartGridBrush;
            set => SetValue(ref _chartGridBrush, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeChartAxisBrush)]
        [PropertyOrder(4)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public DsBrushBase ChartAxisBrush
        {
            get => _chartAxisBrush;
            set => SetValue(ref _chartAxisBrush, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeTrendsInfoTableVisibility)]
        [PropertyOrder(5)]
        public bool TrendsInfoTableVisibility
        {
            get => _trendsInfoTableVisibility;
            set => SetValue(ref _trendsInfoTableVisibility, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeTrendsTuningVisibility)]
        [PropertyOrder(6)]
        public bool TrendsTuningVisibility
        {
            get => _trendsTuningVisibility;
            set => SetValue(ref _trendsTuningVisibility, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeTrendsAxisXVisibility)]
        [PropertyOrder(7)]
        public bool TrendsAxisXVisibility
        {
            get => _trendsAxisXVisibility;
            set => SetValue(ref _trendsAxisXVisibility, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeTrendsAxisYVisibility)]
        [PropertyOrder(8)]
        public bool TrendsAxisYVisibility
        {
            get => _trendsAxisYVisibility;
            set => SetValue(ref _trendsAxisYVisibility, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TrendGroupDsShapeTrendsScrollbarsVisibility)]
        [PropertyOrder(9)]
        public bool TrendsScrollbarsVisibility
        {
            get => _trendsScrollbarsVisibility;
            set => SetValue(ref _trendsScrollbarsVisibility, value);
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

            using (writer.EnterBlock(3))
            {
                writer.Write(DsTrendItemsCollection.ToList());

                writer.WriteObject(Background);
                writer.WriteObject(ChartBackground);
                writer.WriteObject(ChartGridBrush);
                writer.WriteObject(ChartAxisBrush);
                writer.Write(TrendsInfoTableVisibility);
                writer.Write(TrendsTuningVisibility);
                writer.Write(TrendsAxisXVisibility);
                writer.Write(TrendsAxisYVisibility);
                writer.Write(TrendsScrollbarsVisibility);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 3:
                        try
                        {
                            List<DsTrendItem> dsTrendItemsCollection = reader.ReadList<DsTrendItem>();
                            DsTrendItemsCollection.Clear();
                            foreach (DsTrendItem dsTrendItem in dsTrendItemsCollection)
                                DsTrendItemsCollection.Add(dsTrendItem);

                            Background = (reader.ReadObject() as DsBrushBase)!;
                            ChartBackground = (reader.ReadObject() as DsBrushBase)!;                                              
                            ChartGridBrush = (reader.ReadObject() as DsBrushBase)!;
                            ChartAxisBrush = (reader.ReadObject() as DsBrushBase)!;
                            TrendsInfoTableVisibility = reader.ReadBoolean();
                            TrendsTuningVisibility = reader.ReadBoolean();
                            TrendsAxisXVisibility = reader.ReadBoolean();
                            TrendsAxisYVisibility = reader.ReadBoolean();
                            TrendsScrollbarsVisibility = reader.ReadBoolean();
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

            foreach (DsTrendItem dsTrendItem in DsTrendItemsCollection)
                ItemHelper.RefreshForPropertyGrid(dsTrendItem, container);

            OnPropertyChanged(nameof(DsTrendItemsCollection));
        }

        #endregion        

        #region private functions

        private void DsTrendItemsCollectionOnChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, nameof(DsTrendItemsCollection),
                DsTrendItemsCollection, e);

            OnPropertyChanged(nameof(DsTrendItemsCollection));
        }

        #endregion

        #region private fields

        private DsBrushBase _chartBackground = null!;
        private DsBrushBase _chartGridBrush = null!;
        private DsBrushBase _chartAxisBrush = null!;
        private DsBrushBase _background = null!;
        private bool _trendsInfoTableVisibility;
        private bool _trendsTuningVisibility;
        private bool _trendsAxisXVisibility;
        private bool _trendsAxisYVisibility;
        private bool _trendsScrollbarsVisibility;

        #endregion
    }
}