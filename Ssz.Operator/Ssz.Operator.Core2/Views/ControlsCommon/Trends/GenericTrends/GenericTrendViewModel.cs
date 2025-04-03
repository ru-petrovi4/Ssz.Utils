using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using Ssz.Operator.Core;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.DsShapes.Trends;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    public class GenericTrendViewModel : TrendViewModel
    {
        #region construction and destruction

        // Constructor for adding new row
        public GenericTrendViewModel() :
            this(CreateTrend())
        {
            _derivedAxisMinimum = Source.YMin;
            _derivedAxisMaximum = Source.YMax;
        }        

        public GenericTrendViewModel(Trend trend) :
            base(trend)
        {
            _derivedAxisMinimum = Source.YMin;
            _derivedAxisMaximum = Source.YMax;

            IsDisplayedOnPlot = true;

            trend.PropertyChanged += args =>
            {
                if (args.Property == Trend.HdaIdToDisplayProperty)
                {
                    OnPropertyChanged(nameof(Generic_TagToDisplay));
                    OnPropertyChanged(nameof(Generic_PropertyToDisplay));                   
                }
                if (args.Property == Trend.ValueFormatProperty)
                {
                    OnPropertyChanged(nameof(ValueString));
                    OnPropertyChanged(nameof(YMinString));
                    OnPropertyChanged(nameof(YMaxString));
                }
                else if (args.Property == Trend.ValueProperty)
                {
                    OnPropertyChanged(nameof(ValueString));
                }
                else if (args.Property == Trend.YMinProperty)
                {
                    OnPropertyChanged(nameof(YMinString));

                    OnPropertyChanged(() => YMinWithPadding);
                    OnPropertyChanged(() => YMaxWithPadding);

                    if (_overriddenAxisMinimum == null)
                    {
                        _derivedAxisMinimum = Source.YMin;
                        OnPropertyChanged(() => AxisMinimum);
                        OnPropertyChanged(() => AxisMinimumWithPadding);
                        OnPropertyChanged(() => AxisMaximumWithPadding);
                        OnPropertyChanged(() => MajorStep);
                        OnPropertyChanged(() => MinorStep);
                    }
                }
                else if (args.Property == Trend.YMaxProperty)
                {
                    OnPropertyChanged(nameof(YMaxString));

                    OnPropertyChanged(() => YMinWithPadding);
                    OnPropertyChanged(() => YMaxWithPadding);

                    if (_overriddenAxisMaximum == null)
                    {
                        _derivedAxisMaximum = Source.YMax;
                        OnPropertyChanged(() => AxisMaximum);
                        OnPropertyChanged(() => AxisMinimumWithPadding);
                        OnPropertyChanged(() => AxisMaximumWithPadding);
                        OnPropertyChanged(() => MajorStep);
                        OnPropertyChanged(() => MinorStep);
                    }
                }                
            };
        }

        #endregion

        #region public functions

        public static readonly Color[] DefaultColors =
        {            
            new Any("#FFFF00FE").ValueAs<Color>(false),
            new Any("#FF0200F9").ValueAs<Color>(false),
            new Any("#FF00FFFF").ValueAs<Color>(false),
            new Any("#FFFF8041").ValueAs<Color>(false),
            new Any("#FFFC0100").ValueAs<Color>(false),
            new Any("#FF00FF01").ValueAs<Color>(false),
            new Any("#FFFFFF00").ValueAs<Color>(false),
            new Any("#FFFF0000").ValueAs<Color>(false),
        };

        public double YMinWithPadding
        {
            get { return Source.YMin - (Source.YMax - Source.YMin) * 0.01; }
        }

        public double YMaxWithPadding
        {
            get { return Source.YMax + (Source.YMax - Source.YMin) * 0.01; }
        }

        public double AxisMinimum
        {
            get { return _overriddenAxisMinimum ?? _derivedAxisMinimum; }
            set
            {
                SetValue(ref _overriddenAxisMinimum, value);
                OnPropertyChanged(() => AxisMinimumWithPadding);
                OnPropertyChanged(() => AxisMaximumWithPadding);
                OnPropertyChanged(() => MajorStep);
                OnPropertyChanged(() => MinorStep);
            }
        }

        public double AxisMaximum
        {
            get { return _overriddenAxisMaximum ?? _derivedAxisMaximum; }
            set
            {
                SetValue(ref _overriddenAxisMaximum, value);
                OnPropertyChanged(() => AxisMinimumWithPadding);
                OnPropertyChanged(() => AxisMaximumWithPadding);
                OnPropertyChanged(() => MajorStep);
                OnPropertyChanged(() => MinorStep);
            }
        }

        public double AxisMinimumWithPadding
        {
            get { return AxisMinimum - (AxisMaximum - AxisMinimum) * 0.01; }
        }

        public double AxisMaximumWithPadding
        {
            get { return AxisMaximum + (AxisMaximum - AxisMinimum) * 0.01; }
        }

        public double MajorStep
        {
            get { return (AxisMaximum - AxisMinimum) / 10; }
        }

        public double MinorStep
        {
            get { return MajorStep / 5; }
        }

        public int Num
        {
            get { return _num; }
            set { SetValue(ref _num, value); }
        }

        public string HdaId => base.Source.HdaId;

        public string Generic_TagToDisplay
        {
            get
            {                
                return base.Source.TagName;
            }
            set
            {
                if (base.Source.DsTrendItem.TagName != value)
                {
                    base.Source.DsTrendItem.TagName = value;

                    var parentContainer = new GenericContainer();
                    parentContainer.ParentItem = DsProject.Instance;
                    parentContainer.DsConstantsCollection.Add(new DsConstant
                    {
                        Name = DataEngineBase.TagConstant,
                        Value = value
                    });                    
                    base.Source.DsTrendItem.ParentItem = parentContainer;

                    OnPropertyChanged(nameof(HdaId));
                }
            }
        }

        public string Generic_PropertyToDisplay
        {
            get
            {
                var p = base.Source.PropertyPathToDisplay;
                if (String.IsNullOrEmpty(p))
                    p = base.Source.PropertyPath;
                if (p.Length < 2)
                    return @"";
                return p.Substring(1);
            }
            set
            {
                var genericDataEngine = DsProject.Instance.GetDataEngine<GenericDataEngine>();

                string propertyPathToDisplay = "." + value;
                ProcessModelPropertyInfo? processModelPropertyInfo = null;
                if (genericDataEngine != null)
                {
                    processModelPropertyInfo = genericDataEngine.ModelTagPropertyInfosCollection.FirstOrDefault(
                        pi => StringHelper.CompareIgnoreCase(pi.PropertyPathToDisplay, propertyPathToDisplay));
                }
                if (processModelPropertyInfo != null)
                {
                    base.Source.DsTrendItem.PropertyPath = processModelPropertyInfo.PropertyPath;
                }

                OnPropertyChanged(nameof(HdaId));
            }
        }        

        public string[] Generic_PropertyToDisplayCollection
        {
            get
            {
                var genericDataEngine = DsProject.Instance.GetDataEngine<GenericDataEngine>();

                return genericDataEngine.ModelTagPropertyInfosCollection
                    .Where(pi => pi.PropertyPathToDisplay.Length > 1 && pi.PropertyPathToDisplay.StartsWith("."))
                    .Select(
                        pi => pi.PropertyPathToDisplay.Substring(1)).ToArray();
            }            
        }

        public string ValueString
        {
            get
            {
                return new Any(Source.Value).ValueAsString(true, Source.ValueFormat);                
            }            
        }

        public string YMinString
        {
            get
            {
                return new Any(Source.YMin).ValueAsString(true, Source.ValueFormat);                
            }
            set
            {
                if (Double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out double result))
                {
                    Source.YMin = result;
                }
            }
        }

        public string YMaxString
        {
            get
            {
                return new Any(Source.YMax).ValueAsString(true, Source.ValueFormat);                
            }
            set
            {
                if (Double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out double result))
                {
                    Source.YMax = result;
                }
            }
        }

        public DateTime RulerTime
        {
            get { return _rulerTime; }
            set { SetValue(ref _rulerTime, value); }
        }

        public string RulerValueString
        {
            get { return _rulerValueString; }
            set { SetValue(ref _rulerValueString, value); }
        }

        #endregion

        #region protected functions

        protected override void OnRawTrendPointsLoaded(TrendPoint[] points)
        {
            Points = points;
        }

        #endregion

        #region private functions

        private static Trend CreateTrend()
        {                      
            return new Trend(new DsTrendItem(), false, false, PlayDsProjectView.LastActiveRootPlayWindow);
        }

        #endregion

        private int _num;

        private DateTime _rulerTime;
        private string _rulerValueString = @"";

        private double _derivedAxisMinimum;
        private double _derivedAxisMaximum;
        private double? _overriddenAxisMinimum;
        private double? _overriddenAxisMaximum;
    }
}