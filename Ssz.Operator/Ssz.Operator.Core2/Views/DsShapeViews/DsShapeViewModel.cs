using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class DsShapeViewModel : DataValueViewModel, ISelectable
    {
        #region construction and destruction

        public DsShapeViewModel(DsShapeBase dsShape, IPlayWindowBase? playWindow)
            : base(playWindow, playWindow is null)
        {
            DsShape = dsShape;

            ResizeDecoratorIsVisible = true;

            if (VisualDesignMode) 
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

        public double DesignLeft
        {
            get
            {
                if (DsShape.GetParentComplexDsShape() is null || (DsShape.CenterDeltaPositionXInfo.IsConst && DsShape.WidthDeltaInfo.IsConst))
                    return DsShape.LeftNotTransformed;

                var initial = DsShape.CenterInitialPositionNotRounded.X;
                var delta = DsShape.CenterDeltaPositionXInfo.ConstValue;
                var final = DsShape.CenterFinalPositionNotRounded.X;
                var relative = DsShape.CenterRelativePosition.X;
                var lengthInitial = DsShape.WidthInitialNotRounded;
                var lengthDelta = DsShape.WidthDeltaInfo.ConstValue;
                var lengthFinal = DsShape.WidthFinalNotRounded;

                if (lengthDelta < 0) lengthDelta = 0;
                else if (lengthDelta > 1) lengthDelta = 1;

                var length = lengthInitial + (lengthFinal - lengthInitial) * lengthDelta;

                if (delta < 0) delta = 0;
                else if (delta > 1) delta = 1;

                var pos = initial + (final - initial) * delta;

                return pos - length * relative;
            }
        }

        public double TopNotTransformed
        {
            get => DsShape.TopNotTransformed;
            set => DsShape.TopNotTransformed = value;
        }

        public double DesignTop
        {
            get
            {
                if (DsShape.GetParentComplexDsShape() is null || (DsShape.CenterDeltaPositionYInfo.IsConst && DsShape.HeightDeltaInfo.IsConst))
                    return DsShape.TopNotTransformed;

                var initial = DsShape.CenterInitialPositionNotRounded.Y;
                var delta = DsShape.CenterDeltaPositionYInfo.ConstValue;
                var final = DsShape.CenterFinalPositionNotRounded.Y;
                var relative = DsShape.CenterRelativePosition.Y;
                var lengthInitial = DsShape.HeightInitialNotRounded;
                var lengthDelta = DsShape.HeightDeltaInfo.ConstValue;
                var lengthFinal = DsShape.HeightFinalNotRounded;

                if (lengthDelta < 0) lengthDelta = 0;
                else if (lengthDelta > 1) lengthDelta = 1;

                var length = lengthInitial + (lengthFinal - lengthInitial) * lengthDelta;

                if (delta < 0) delta = 0;
                else if (delta > 1) delta = 1;

                var pos = initial + (final - initial) * delta;

                return pos - length * relative;
            }
        }

        public double WidthInitial
        {
            get => DsShape.WidthInitialNotRounded;
            set => DsShape.WidthInitial = value;
        }        

        public double WidthFinal => DsShape.WidthFinalNotRounded;

        public double DesignWidth
        {
            get
            {
                if (DsShape.GetParentComplexDsShape() is null || DsShape.WidthDeltaInfo.IsConst)
                    return DsShape.WidthInitialNotRounded;

                var initial = DsShape.WidthInitialNotRounded;
                var delta = DsShape.WidthDeltaInfo.ConstValue;
                var final = DsShape.WidthFinalNotRounded;

                if (delta < 0)
                    delta = 0;
                else if (delta > 1)
                    delta = 1;

                return initial + (final - initial) * delta;
            }
        }

        public double HeightInitial
        {
            get => DsShape.HeightInitialNotRounded;
            set => DsShape.HeightInitial = value;
        }        

        public double HeightFinal => DsShape.HeightFinalNotRounded;

        public double DesignHeight
        {
            get
            {
                if (DsShape.GetParentComplexDsShape() is null || DsShape.HeightDeltaInfo.IsConst)
                    return DsShape.HeightInitialNotRounded;

                var initial = DsShape.HeightInitialNotRounded;
                var delta = DsShape.HeightDeltaInfo.ConstValue;
                var final = DsShape.HeightFinalNotRounded;

                if (delta < 0)
                    delta = 0;
                else if (delta > 1)
                    delta = 1;

                return initial + (final - initial) * delta;
            }
        }

        public double AngleInitial
        {
            get => DsShape.AngleInitialNotRounded;
            set => DsShape.AngleInitial = value;
        }        

        public double AngleFinal => DsShape.AngleFinalNotRounded;

        public double DesignAngle
        {
            get
            {
                if (DsShape.GetParentComplexDsShape() is null || DsShape.AngleDeltaInfo.IsConst)
                    return DsShape.AngleInitialNotRounded;

                var initial = DsShape.AngleInitialNotRounded;
                var delta = DsShape.AngleDeltaInfo.ConstValue;
                var final = DsShape.AngleFinalNotRounded;

                if (delta < 0)
                    delta = 0;
                else if (delta > 1)
                    delta = 1;

                return initial + (final - initial) * delta;
            }
        }

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
                if (SetProperty(ref _isSelected, value))
                {
                    if (value) SetGeometryEditingMode();
                    else GeometryEditingMode = false;
                }
            }
        }

        public bool IsFirstSelected
        {
            get => _isFirstSelected;
            set => SetProperty(ref _isFirstSelected, value);
        }

        public bool GeometryEditingMode
        {
            get => _geometryEditingMode;
            set => SetProperty(ref _geometryEditingMode, value);
        }

        public bool ResizeDecoratorIsVisible { get; set; }

        public double CenterFinalPositionX => DsShape.CenterFinalPositionNotRounded.X;

        public double CenterFinalPositionY => DsShape.CenterFinalPositionNotRounded.Y;

        public bool OpenDsShapeDrawingMenuItemVisibility
        {
            get
            {
                var complexDsShape = DsShape as ComplexDsShape;
                if (complexDsShape is not null) return true;
                return false;
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

        #region protected functions

        protected override void OnGlobalUITimerEvent(int phase)
        {
            if (IsDisposed || !_updatingIsEnabled)
                return;

            NotifyIndexerChanged();
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
                    OnPropertyChanged(nameof(DesignLeft));
                    OnPropertyChanged(nameof(DesignTop));
                    break;
                case @"CenterDeltaPositionXInfo":
                    OnPropertyChanged(@"CenterDeltaPositionX");
                    OnPropertyChanged(nameof(DesignLeft));
                    break;
                case @"CenterDeltaPositionYInfo":
                    OnPropertyChanged(@"CenterDeltaPositionY");
                    OnPropertyChanged(nameof(DesignTop));
                    break;
                case @"CenterFinalPosition":
                    OnPropertyChanged(nameof(DesignLeft));
                    OnPropertyChanged(nameof(DesignTop));
                    break;
                case @"WidthInitial":
                    OnPropertyChanged(@"WidthInitial");
                    OnPropertyChanged(@"LeftNotTransformed");
                    OnPropertyChanged(nameof(DesignWidth));
                    OnPropertyChanged(nameof(DesignLeft));
                    break;
                case @"WidthDeltaInfo":
                case @"WidthFinal":
                    OnPropertyChanged(nameof(DesignWidth));
                    OnPropertyChanged(nameof(DesignLeft));
                    break;
                case @"HeightInitial":
                    OnPropertyChanged(@"HeightInitial");
                    OnPropertyChanged(@"TopNotTransformed");
                    OnPropertyChanged(nameof(DesignHeight));
                    OnPropertyChanged(nameof(DesignTop));
                    break;
                case "HeightDeltaInfo":
                case "HeightFinal":
                    OnPropertyChanged(nameof(DesignHeight));
                    OnPropertyChanged(nameof(DesignTop));
                    break;
                case @"CenterRelativePosition":
                    OnPropertyChanged(@"CenterRelativePosition");
                    OnPropertyChanged(@"LeftNotTransformed");
                    OnPropertyChanged(@"TopNotTransformed");
                    OnPropertyChanged(nameof(DesignLeft));
                    OnPropertyChanged(nameof(DesignTop));
                    break;
                case @"AngleInitial":
                    OnPropertyChanged(@"AngleInitial");
                    OnPropertyChanged(nameof(DesignAngle));
                    break;
                case @"AngleDeltaInfo":
                case @"AngleFinal":
                    OnPropertyChanged(nameof(DesignAngle));
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

                if (_updatingIsEnabled)
                    NotifyIndexerChanged();
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