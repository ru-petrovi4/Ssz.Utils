using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class DsShapeViewModel : DataValueViewModel, ISelectable
    {
        #region protected functions

        protected override void OnGlobalUITimerEvent(int phase)
        {           
            if (IsDisposed || !_updatingIsEnabled) return;

            OnPropertyChanged(Binding.IndexerName);
        }

        #endregion

        #region construction and destruction

        public DsShapeViewModel(DsShapeBase dsShape, IPlayWindowBase? playWindow)
            : base(playWindow, playWindow is null)
        {
            DsShape = dsShape;

            ResizeDecoratorIsVisible = true;

            DsShape.PropertyChanged += DsShapeOnPropertyChanged;
        }


        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                DsShapeChanged = delegate { };

                // DsShape clears all events subscriptions after disposing.
                DsShape = ConstEmptyDsShape; // To avoid exceptions after disposing.

                if (_playDrawingViewModel is not null)
                {
                    _playDrawingViewModel.PropertyChanged -= PlayDrawingViewModelOnPropertyChanged;
                    _playDrawingViewModel = null;
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public const int DesignDsShapeViewsStartZIndex = 0x10000;

        public string Header => DsShape.GetDsShapeNameToDisplayAndType();

        public double CenterInitialPositionX => DsShape.CenterInitialPositionNotRounded.X;

        public double CenterDeltaPositionX => DsShape.CenterDeltaPositionXInfo.ConstValue;

        public double CenterInitialPositionY => DsShape.CenterInitialPositionNotRounded.Y;

        public double CenterDeltaPositionY => DsShape.CenterDeltaPositionYInfo.ConstValue;

        public Point CenterRelativePosition => DsShape.CenterRelativePosition;

        public double LeftNotTransformed
        {
            get => DsShape.LeftNotTransformed;
            set => DsShape.LeftNotTransformed = value;
        }

        public double TopNotTransformed
        {
            get => DsShape.TopNotTransformed;
            set => DsShape.TopNotTransformed = value;
        }

        public double WidthInitial
        {
            get => DsShape.WidthInitialNotRounded;
            set => DsShape.WidthInitial = value;
        }

        public double WidthDelta => DsShape.WidthDeltaInfo.ConstValue;

        public double WidthFinal => DsShape.WidthFinalNotRounded;

        public double HeightInitial
        {
            get => DsShape.HeightInitialNotRounded;
            set => DsShape.HeightInitial = value;
        }

        public double HeightDelta => DsShape.HeightDeltaInfo.ConstValue;

        public double HeightFinal => DsShape.HeightFinalNotRounded;

        public double AngleInitial
        {
            get => DsShape.AngleInitialNotRounded;
            set => DsShape.AngleInitial = value;
        }

        public double AngleDelta => DsShape.AngleDeltaInfo.ConstValue;

        public double AngleFinal => DsShape.AngleFinalNotRounded;

        public bool IsFlipped
        {
            get => DsShape.IsFlipped;
            set => DsShape.IsFlipped = value;
        }

        public double RotationX
        {
            get => DsShape.RotationXNotRounded;
            set => DsShape.RotationX = value;
        }

        public double RotationY
        {
            get => DsShape.RotationYNotRounded;
            set => DsShape.RotationY = value;
        }

        public double RotationZ
        {
            get => DsShape.RotationZNotRounded;
            set => DsShape.RotationZ = value;
        }

        public double FieldOfView
        {
            get => DsShape.FieldOfViewNotRounded;
            set => DsShape.FieldOfView = value;
        }

        public int ZIndex =>
            DsShape.Index >= 0 ? DsShape.Index + 1 : DesignDsShapeViewsStartZIndex + DsShape.Index;

        public int DesignZIndex => ZIndex + DesignDsShapeViewsStartZIndex;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetValue(ref _isSelected, value))
                {
                    if (value) SetGeometryEditingMode();
                    else GeometryEditingMode = false;
                }
            }
        }

        public bool IsFirstSelected
        {
            get => _isFirstSelected;
            set => SetValue(ref _isFirstSelected, value);
        }

        public bool GeometryEditingMode
        {
            get => _geometryEditingMode;
            set => SetValue(ref _geometryEditingMode, value);
        }

        public bool ResizeDecoratorIsVisible { get; set; }

        public double CenterFinalPositionX => DsShape.CenterFinalPositionNotRounded.X;

        public double CenterFinalPositionY => DsShape.CenterFinalPositionNotRounded.Y;

        public Visibility OpenDsShapeDrawingMenuItemVisibility
        {
            get
            {
                var complexDsShape = DsShape as ComplexDsShape;
                if (complexDsShape is not null) return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public string OpenDsShapeDrawingMenuItemHeader
        {
            get
            {
                var complexDsShape = DsShape as ComplexDsShape;
                if (complexDsShape is not null)
                    return (Resources.OpenDsShapeDrawingFromComplexDsShape + " " +
                            complexDsShape.DsShapeDrawingName).Replace("_", "__");
                return @"";
            }
        }

        public DsShapeBase DsShape { get; private set; }

        public event Action<string?> DsShapeChanged = delegate { };

        public override void Initialize(object? param_)
        {
            base.Initialize(param_);

            PlayDrawingViewModel? playDrawingViewModel = param_ as PlayDrawingViewModel;
            if (playDrawingViewModel is not null)
            {
                _playDrawingViewModel = playDrawingViewModel;
                _updatingIsEnabled = _playDrawingViewModel.DrawingUpdatingIsEnabled;
                _playDrawingViewModel.PropertyChanged += PlayDrawingViewModelOnPropertyChanged;
            }
        }

        public void SetGeometryEditingMode()
        {
            GeometryEditingMode =
                !(DsShape.WidthInitialNotRounded < 25.0 || DsShape.HeightInitialNotRounded < 25.0);
        }

        public Rect GetNotTransformedRect()
        {
            return DsShape.GetNotTransformedRect();
        }

        public Rect GetBoundingRect()
        {
            return DsShape.GetBoundingRect();
        }

        public void SetBoundingRect(Rect rect)
        {
            DsShape.SetBoundingRect(rect);
        }

        public void OnDsShapeChanged()
        {
            DsShapeChanged(null);
        }

        #endregion

        #region private functions

        private void DsShapeOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case @"CenterInitialPosition":
                    OnPropertyChanged(@"CenterInitialPositionX");
                    OnPropertyChanged(@"CenterInitialPositionY");
                    OnPropertyChanged(@"LeftNotTransformed");
                    OnPropertyChanged(@"TopNotTransformed");
                    break;
                case @"CenterDeltaPositionXInfo":
                    OnPropertyChanged(@"CenterDeltaPositionX");
                    break;
                case @"CenterDeltaPositionYInfo":
                    OnPropertyChanged(@"CenterDeltaPositionY");
                    break;
                case @"WidthInitial":
                    OnPropertyChanged(@"WidthInitial");
                    OnPropertyChanged(@"LeftNotTransformed");
                    break;
                case @"WidthDeltaInfo":
                    OnPropertyChanged(@"WidthDelta");
                    break;
                case @"HeightInitial":
                    OnPropertyChanged(@"HeightInitial");
                    OnPropertyChanged(@"TopNotTransformed");
                    break;
                case "HeightDeltaInfo":
                    OnPropertyChanged(@"HeightDelta");
                    break;
                case @"CenterRelativePosition":
                    OnPropertyChanged(@"CenterRelativePosition");
                    OnPropertyChanged(@"LeftNotTransformed");
                    OnPropertyChanged(@"TopNotTransformed");
                    break;
                case @"AngleInitial":
                    OnPropertyChanged(@"AngleInitial");
                    break;
                case @"AngleDeltaInfo":
                    OnPropertyChanged(@"AngleDelta");
                    break;
                case @"IsFlipped":
                    OnPropertyChanged(@"IsFlipped");
                    break;
                case @"RotationX":
                    OnPropertyChanged(@"RotationX");
                    break;
                case @"RotationY":
                    OnPropertyChanged(@"RotationY");
                    break;
                case @"RotationZ":
                    OnPropertyChanged(@"RotationZ");
                    break;
                case @"FieldOfView":
                    OnPropertyChanged(@"FieldOfView");
                    break;
                case @"Index":
                    OnPropertyChanged(@"ZIndex");
                    OnPropertyChanged(@"DesignZIndex");
                    break;
                case @"Name":
                case @"Desc":
                    OnPropertyChanged(@"Header");
                    break;
                default:
                    DsShapeChanged(args.PropertyName);
                    break;
            }
        }

        private void PlayDrawingViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_playDrawingViewModel is null) return;
            if (e.PropertyName == @"DrawingUpdatingIsEnabled" &&
                _updatingIsEnabled != _playDrawingViewModel.DrawingUpdatingIsEnabled)
            {
                _updatingIsEnabled = _playDrawingViewModel.DrawingUpdatingIsEnabled;

                if (_updatingIsEnabled) OnPropertyChanged(Binding.IndexerName);
            }
        }

        #endregion

        #region private fields

        private static readonly EmptyDsShape ConstEmptyDsShape = new();

        private bool _isSelected;
        private bool _isFirstSelected;
        private bool _geometryEditingMode;

        private PlayDrawingViewModel? _playDrawingViewModel;

        private bool _updatingIsEnabled = true;

        #endregion
    }
}