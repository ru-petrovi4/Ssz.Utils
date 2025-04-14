using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapes.Trends;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public class Trend : StyledElement, IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     If playWindow is null, tyhe visual design mode.
        /// </summary>
        /// <param name="dsTrendItem"></param>
        /// <param name="showAlarmLevels"></param>
        /// <param name="autoUpdateObservableDataSource"></param>
        /// <param name="playWindow"></param>
        public Trend(
            DsTrendItem dsTrendItem, 
            bool showAlarmLevels,
            IPlayWindow? playWindow)
        {
            DsTrendItem = dsTrendItem;

            PlayWindow = playWindow;

            PropertyChanged += OnPropertyChanged;

            var container = dsTrendItem.ParentItem.Find<IDsContainer>();

            Visible = true;
            TagName = ConstantsHelper.ComputeValue(container, dsTrendItem.TagName)!;
            TagType = ConstantsHelper.ComputeValue(container, dsTrendItem.TagType)!;
            PropertyPath = ConstantsHelper.ComputeValue(container, dsTrendItem.PropertyPath)!;
            HdaId = TagName + PropertyPath;

            if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.DesktopPlayMode ||
                DsProject.Instance.Mode == DsProject.DsProjectModeEnum.BrowserPlayMode)
            {
                var constAny = ElementIdsMap.TryGetConstValue(HdaId);
                if (constAny.HasValue)
                {
                    Visible = false;
                }                
            }

            ParentGenericContainer = new GenericContainer();
            ParentGenericContainer.ParentItem = DsProject.Instance;
            ParentGenericContainer.DsConstantsCollection.Add(new DsConstant
            {
                Name = DataEngineBase.TagConstant,
                Value = TagName,
                Type = TagType
            });

            var genericDataEngine = DsProject.Instance.DataEngine;
            if (!VisualDesignMode)
                this.SetBindingOrConst(ParentGenericContainer, TagNameToDisplayProperty,
                    genericDataEngine.TagNameToDisplayInfo,
                    BindingMode.OneWay, UpdateSourceTrigger.Default);
            else
                TagNameToDisplay = TagName;

            if (dsTrendItem.DescriptionInfo.IsConst && String.IsNullOrEmpty(dsTrendItem.DescriptionInfo.ConstValue))
                this.SetBindingOrConst(ParentGenericContainer, DescriptionProperty, genericDataEngine.TagDescInfo,
                    BindingMode.OneWay, UpdateSourceTrigger.Default, VisualDesignMode);
            else
                this.SetBindingOrConst(container, DescriptionProperty, dsTrendItem.DescriptionInfo,
                    BindingMode.OneWay, UpdateSourceTrigger.Default, VisualDesignMode);

            this.SetBindingOrConst(
                container,
                BrushProperty,
                dsTrendItem.DsBrush,
                BindingMode.OneWay,
                UpdateSourceTrigger.Default);                        

            this.SetBindingOrConst(
                container,
                ValueProperty,
                new DoubleDataBinding
                {
                    DataBindingItemsCollection =
                    {
                        new DataBindingItem(HdaId, DataSourceType.OpcVariable)
                    }
                },
                BindingMode.OneWay,
                UpdateSourceTrigger.Default);

            ModelTagPropertyInfo = genericDataEngine.FindPropertyInfo(TagType,
                PropertyPath);
            if (ModelTagPropertyInfo is not null)
            {
                PropertyPathToDisplay = ModelTagPropertyInfo.PropertyPathToDisplay != ""
                    ? ModelTagPropertyInfo.PropertyPathToDisplay
                    : PropertyPath;

                this.SetBindingOrConst(ParentGenericContainer, YMinProperty, ModelTagPropertyInfo.MinScaleInfo,
                    BindingMode.OneWay, UpdateSourceTrigger.Default, VisualDesignMode);
                this.SetBindingOrConst(ParentGenericContainer, YMaxProperty, ModelTagPropertyInfo.MaxScaleInfo,
                    BindingMode.OneWay, UpdateSourceTrigger.Default, VisualDesignMode);
                this.SetBindingOrConst(ParentGenericContainer, EUProperty, ModelTagPropertyInfo.EUInfo,
                    BindingMode.OneWay, UpdateSourceTrigger.Default, VisualDesignMode);
                if (showAlarmLevels)
                {
                    this.SetBindingOrConst(ParentGenericContainer, HiHiAlarmLimitProperty,
                        ModelTagPropertyInfo.HiHiAlarmInfo,
                        BindingMode.OneWay, UpdateSourceTrigger.Default, VisualDesignMode);
                    this.SetBindingOrConst(ParentGenericContainer, HiAlarmLimitProperty,
                        ModelTagPropertyInfo.HiAlarmInfo,
                        BindingMode.OneWay,
                        UpdateSourceTrigger.Default, VisualDesignMode);
                    this.SetBindingOrConst(ParentGenericContainer, LoAlarmLimitProperty,
                        ModelTagPropertyInfo.LoAlarmInfo,
                        BindingMode.OneWay,
                        UpdateSourceTrigger.Default, VisualDesignMode);
                    this.SetBindingOrConst(ParentGenericContainer, LoLoAlarmLimitProperty,
                        ModelTagPropertyInfo.LoLoAlarmInfo,
                        BindingMode.OneWay, UpdateSourceTrigger.Default, VisualDesignMode);
                }
            }
            else
            {
                YMin = 0.0;
                YMax = 100.0;
                EU = @"%";
                PropertyPathToDisplay = PropertyPath;
            }

            if (dsTrendItem.ValueFormatInfo.IsConst && String.IsNullOrEmpty(dsTrendItem.ValueFormatInfo.ConstValue) &&
                    ModelTagPropertyInfo is not null)
                this.SetBindingOrConst(
                    ParentGenericContainer,
                    ValueFormatProperty,
                    ModelTagPropertyInfo.FormatInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default);
            else
                this.SetBindingOrConst(
                    container,
                    ValueFormatProperty,
                    dsTrendItem.ValueFormatInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default);

            DataContext = new DataValueViewModel(playWindow, false);
        }        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((DataValueViewModel)DataContext!).Dispose();
            }
        }

        ~Trend()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly AvaloniaProperty VisibleProperty = AvaloniaProperty.Register<Trend, bool>("Visible");

        public static readonly AvaloniaProperty BrushProperty = AvaloniaProperty.Register<Trend, Brush>("Brush");

        public static readonly AvaloniaProperty TagNameToDisplayProperty = AvaloniaProperty.Register<Trend, string>("TagNameToDisplay");

        public static readonly AvaloniaProperty HdaIdToDisplayProperty = AvaloniaProperty.Register<Trend, string>("HdaIdToDisplay");

        public static readonly AvaloniaProperty YMinProperty = AvaloniaProperty.Register<Trend, double>("YMin", double.NaN);

        public static readonly AvaloniaProperty YMaxProperty = AvaloniaProperty.Register<Trend, double>("YMax", double.NaN);

        public static readonly AvaloniaProperty EUProperty = AvaloniaProperty.Register<Trend, string>("EU", @"%");

        public static readonly AvaloniaProperty HiHiAlarmLimitProperty = AvaloniaProperty.Register<Trend, double>("HiHiAlarmLimit", double.NaN);

        public static readonly AvaloniaProperty HiAlarmLimitProperty = AvaloniaProperty.Register<Trend, double>("HiAlarmLimit", double.NaN);

        public static readonly AvaloniaProperty LoAlarmLimitProperty = AvaloniaProperty.Register<Trend, double>("LoAlarmLimit", double.NaN);

        public static readonly AvaloniaProperty LoLoAlarmLimitProperty = AvaloniaProperty.Register<Trend, double>("LoLoAlarmLimit", double.NaN);

        public static readonly AvaloniaProperty DescriptionProperty = AvaloniaProperty.Register<Trend, string>("Description");

        public static readonly AvaloniaProperty ValueProperty = AvaloniaProperty.Register<Trend, double>("Value", 0.0);

        public static readonly AvaloniaProperty ValueFormatProperty = AvaloniaProperty.Register<Trend, string>("ValueFormat", "");

        public DsTrendItem DsTrendItem { get; }

        public IPlayWindow? PlayWindow { get; }

        public bool VisualDesignMode => PlayWindow is null;

        public bool Visible
        {
            get => (bool) GetValue(VisibleProperty)!;
            set => SetValue(VisibleProperty, value);
        }

        [Browsable(false)]
        public Brush Brush
        {
            get => (Brush) GetValue(BrushProperty)!;
            set => SetValue(BrushProperty, value);
        }

        public string TagName { get; }

        public string TagType { get; }

        public string PropertyPath { get; }

        public string HdaId { get; }

        public string TagNameToDisplay
        {
            get => (string) GetValue(TagNameToDisplayProperty)!;
            set => SetValue(TagNameToDisplayProperty, value);
        }

        public string PropertyPathToDisplay { get; }

        public string HdaIdToDisplay
        {
            get => (string) GetValue(HdaIdToDisplayProperty)!;
            set => SetValue(HdaIdToDisplayProperty, value);
        }

        public double YMin
        {
            get => (double) GetValue(YMinProperty)!;
            set => SetValue(YMinProperty, value);
        }

        public double YMax
        {
            get => (double) GetValue(YMaxProperty)!;
            set => SetValue(YMaxProperty, value);
        }

        public string EU
        {
            get => (string) GetValue(EUProperty)!;
            set => SetValue(EUProperty, value);
        }

        public double HiHiAlarmLimit
        {
            get => (double) GetValue(HiHiAlarmLimitProperty)!;
            set => SetValue(HiHiAlarmLimitProperty, value);
        }

        public double HiAlarmLimit
        {
            get => (double) GetValue(HiAlarmLimitProperty)!;
            set => SetValue(HiAlarmLimitProperty, value);
        }

        public double LoAlarmLimit
        {
            get => (double) GetValue(LoAlarmLimitProperty)!;
            set => SetValue(LoAlarmLimitProperty, value);
        }

        public double LoLoAlarmLimit
        {
            get => (double) GetValue(LoLoAlarmLimitProperty)!;
            set => SetValue(LoLoAlarmLimitProperty, value);
        }

        public string Description
        {
            get => (string) GetValue(DescriptionProperty)!;
            set => SetValue(DescriptionProperty, value);
        }        

        public double VisibleYMax { get; set; }

        public double VisibleYMin { get; set; }

        public double Thicknes { get; set; } = 3;

        public Func<KeyValuePair<DateTime, double>, double> YMapping
        {
            get
            {
                return point =>
                {
                    var y = point.Value;
                    return (y - VisibleYMin) / (VisibleYMax - VisibleYMin);
                };
            }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty)!; }
            set { SetValue(ValueProperty, value); }
        }

        public string ValueFormat
        {
            get { return (string)GetValue(ValueFormatProperty)!; }
            set { SetValue(ValueFormatProperty, value); }
        }

        public IDsContainer ParentContainer => throw new NotImplementedException();

        public ObservableCollection<DsConstant> DsConstantsCollection => throw new NotImplementedException();

        public DsShapeBase[] DsShapes
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }        

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(HdaId))
                return HdaId;
            return "Trend Seria";
        }

        public string ResolveConstant(string constant)
        {
            throw new NotImplementedException();
        }

        public string GetConstantType(string constant)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region protected functions

        protected GenericContainer ParentGenericContainer { get; }

        protected ProcessModelPropertyInfo? ModelTagPropertyInfo { get; }

        #endregion

        #region private functions

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TagNameToDisplayProperty)
            {
                var trendItemView = sender as Trend;
                if (trendItemView is null) return;

                trendItemView.HdaIdToDisplay = e.NewValue + trendItemView.PropertyPathToDisplay;
            }
        }

        #endregion
    }
}