using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Research.DynamicDataDisplay.DataSources;
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
    public class Trend : FrameworkElement, IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     If playWindow is null, tyhe visual design mode.
        /// </summary>
        /// <param name="dsTrendItem"></param>
        /// <param name="showAlarmLevels"></param>
        /// <param name="autoUpdateObservableDataSource"></param>
        /// <param name="playWindow"></param>
        public Trend(DsTrendItem dsTrendItem, bool showAlarmLevels, bool autoUpdateObservableDataSource,
            IPlayWindow? playWindow)
        {
            DsTrendItem = dsTrendItem;

            _autoUpdateObservableDataSource = autoUpdateObservableDataSource;

            PlayWindow = playWindow;

            var container = dsTrendItem.ParentItem.Find<IDsContainer>();

            Visible = true;
            TagName = ConstantsHelper.ComputeValue(container, dsTrendItem.TagName)!;
            TagType = ConstantsHelper.ComputeValue(container, dsTrendItem.TagType)!;
            PropertyPath = ConstantsHelper.ComputeValue(container, dsTrendItem.PropertyPath)!;
            HdaId = TagName + PropertyPath;

            if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.WindowsPlayMode ||
                DsProject.Instance.Mode == DsProject.DsProjectModeEnum.WebPlayMode)
            {
                var constAny = ElementIdsMap.TryGetConstValue(HdaId);
                if (constAny.HasValue)
                {
                    Visibility = Visibility.Collapsed;
                }
                else if (_autoUpdateObservableDataSource)
                {
                    _firstTimestampUtc = DateTime.FromFileTimeUtc(0);

                    ObservableDataSource = new ObservableDataSource<KeyValuePair<DateTime, double>>();

                    DsProject.Instance.GlobalUITimerEvent += OnGlobalUITimerEvent;
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
                ((DataValueViewModel)DataContext).Dispose();

                if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.WindowsPlayMode ||
                    DsProject.Instance.Mode == DsProject.DsProjectModeEnum.WebPlayMode)
                {
                    var constAny = ElementIdsMap.TryGetConstValue(HdaId);
                    if (!constAny.HasValue && _autoUpdateObservableDataSource)
                    {
                        DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;
                    }
                }
            }
        }

        ~Trend()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register("Visible", typeof(bool),
            typeof(
                Trend),
            new FrameworkPropertyMetadata());

        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register("Brush", typeof(Brush),
            typeof(Trend),
            new FrameworkPropertyMetadata());

        public static readonly DependencyProperty TagNameToDisplayProperty = DependencyProperty.Register("TagNameToDisplay",
            typeof(string),
            typeof(
                Trend), new FrameworkPropertyMetadata
                (OnTagNameToDisplayPropertyChanged));

        public static readonly DependencyProperty HdaIdToDisplayProperty = DependencyProperty.Register(
            "HdaIdToDisplay", typeof(string),
            typeof(Trend));

        public static readonly DependencyProperty YMinProperty = DependencyProperty.Register("YMin", typeof(double),
            typeof(Trend), new FrameworkPropertyMetadata
                (double.NaN));

        public static readonly DependencyProperty YMaxProperty = DependencyProperty.Register("YMax", typeof(double),
            typeof(Trend), new FrameworkPropertyMetadata
                (double.NaN));

        public static readonly DependencyProperty EUProperty = DependencyProperty.Register("EU", typeof(string),
            typeof(Trend), new FrameworkPropertyMetadata
                (@"%"));

        public static readonly DependencyProperty HiHiAlarmLimitProperty = DependencyProperty.Register(
            "HiHiAlarmLimit", typeof(double),
            typeof(Trend), new FrameworkPropertyMetadata
                (double.NaN));

        public static readonly DependencyProperty HiAlarmLimitProperty = DependencyProperty.Register("HiAlarmLimit",
            typeof(double),
            typeof(Trend), new FrameworkPropertyMetadata
                (double.NaN));

        public static readonly DependencyProperty LoAlarmLimitProperty = DependencyProperty.Register("LoAlarmLimit",
            typeof(double),
            typeof(Trend), new FrameworkPropertyMetadata
                (double.NaN));

        public static readonly DependencyProperty LoLoAlarmLimitProperty = DependencyProperty.Register(
            "LoLoAlarmLimit", typeof(double),
            typeof(Trend), new FrameworkPropertyMetadata
                (double.NaN));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(Trend));

        public static readonly DependencyProperty ObservableDataSourceProperty =
            DependencyProperty.Register("ObservableDataSource",
                typeof(
                    ObservableDataSource
                    <KeyValuePair<DateTime, double>>),
                typeof(Trend));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(Trend), new PropertyMetadata(0.0));

        public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
            "ValueFormat", typeof(string), typeof(Trend), new PropertyMetadata(""));

        public DsTrendItem DsTrendItem { get; }

        public IPlayWindow? PlayWindow { get; }

        public bool VisualDesignMode => PlayWindow is null;

        public bool Visible
        {
            get => (bool) GetValue(VisibleProperty);
            set => SetValue(VisibleProperty, value);
        }

        [Browsable(false)]
        public Brush Brush
        {
            get => (Brush) GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public string TagName { get; }

        public string TagType { get; }

        public string PropertyPath { get; }

        public string HdaId { get; }

        public string TagNameToDisplay
        {
            get => (string) GetValue(TagNameToDisplayProperty);
            set => SetValue(TagNameToDisplayProperty, value);
        }

        public string PropertyPathToDisplay { get; }

        public string HdaIdToDisplay
        {
            get => (string) GetValue(HdaIdToDisplayProperty);
            set => SetValue(HdaIdToDisplayProperty, value);
        }

        public double YMin
        {
            get => (double) GetValue(YMinProperty);
            set => SetValue(YMinProperty, value);
        }

        public double YMax
        {
            get => (double) GetValue(YMaxProperty);
            set => SetValue(YMaxProperty, value);
        }

        public string EU
        {
            get => (string) GetValue(EUProperty);
            set => SetValue(EUProperty, value);
        }

        public double HiHiAlarmLimit
        {
            get => (double) GetValue(HiHiAlarmLimitProperty);
            set => SetValue(HiHiAlarmLimitProperty, value);
        }

        public double HiAlarmLimit
        {
            get => (double) GetValue(HiAlarmLimitProperty);
            set => SetValue(HiAlarmLimitProperty, value);
        }

        public double LoAlarmLimit
        {
            get => (double) GetValue(LoAlarmLimitProperty);
            set => SetValue(LoAlarmLimitProperty, value);
        }

        public double LoLoAlarmLimit
        {
            get => (double) GetValue(LoLoAlarmLimitProperty);
            set => SetValue(LoLoAlarmLimitProperty, value);
        }

        public string Description
        {
            get => (string) GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public ObservableDataSource<KeyValuePair<DateTime, double>> ObservableDataSource
        {
            get => (ObservableDataSource<KeyValuePair<DateTime, double>>) GetValue(ObservableDataSourceProperty);
            set => SetValue(ObservableDataSourceProperty, value);
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
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string ValueFormat
        {
            get { return (string)GetValue(ValueFormatProperty); }
            set { SetValue(ValueFormatProperty, value); }
        }

        public IDsContainer ParentContainer => throw new NotImplementedException();

        public ObservableCollection<DsConstant> DsConstantsCollection => throw new NotImplementedException();

        public DsShapeBase[] DsShapes
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public event Action<DependencyPropertyChangedEventArgs> PropertyChanged = delegate { };

        /*
        public string ResolveConstant(string constant)
        {
            if (
                StringHelper.CompareIgnoreCase(constant, DataEngineBase.ConstantConst))
            {
                return _dsTrendItem.Tag;
            }
            return null;
        }

        public string GetConstantType(string constant)
        {
            if (
                StringHelper.CompareIgnoreCase(constant, DataEngineBase.ConstantConst))
            {
                return _dsTrendItem.TagType;
            }
            return null;
        }*/

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

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            PropertyChanged(e);
        }

        #endregion        

        #region private functions

        private static void OnTagNameToDisplayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var trendItemView = d as Trend;
            if (trendItemView is null) return;

            trendItemView.HdaIdToDisplay = e.NewValue + trendItemView.PropertyPathToDisplay;            
        }        

        private async void OnGlobalUITimerEvent(int phase)
        {
            if (phase == 0)
            {
                var result = await DsDataAccessProvider.Instance.ReadElementValuesJournal(HdaId, _firstTimestampUtc,
                    DateTime.UtcNow);
                ReadJournalDataForTimeIntervalCallback(result);
            }
        }

        private void ReadJournalDataForTimeIntervalCallback(IEnumerable<ValueStatusTimestamp> xiValueStatusTimestamps)
        {
            if (ObservableDataSource is null) return;

            var maxTimestampUtc = DateTime.MinValue;
            var toAdd = new List<KeyValuePair<DateTime, double>>();
            foreach (var vst in xiValueStatusTimestamps)
            {
                if (vst.TimestampUtc <= _firstTimestampUtc)
                    continue;
                if (vst.TimestampUtc > maxTimestampUtc)
                    maxTimestampUtc = vst.TimestampUtc;
                var kvp = new KeyValuePair<DateTime, double>(vst.TimestampUtc.ToLocalTime(),
                    vst.Value.ValueAsDouble(false));
                toAdd.Add(kvp);
            }

            if (toAdd.Count == 0) return;
            _firstTimestampUtc = maxTimestampUtc;
            ObservableDataSource.SuspendUpdate();
            ObservableDataSource.AppendMany(toAdd);
            ObservableDataSource.ResumeUpdate();
        }

        #endregion

        #region private fields

        private readonly bool _autoUpdateObservableDataSource;

        private DateTime _firstTimestampUtc;

        #endregion
    }
}