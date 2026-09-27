using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.ViewModels;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors.GradientBrushEditor
{
    /// <summary>
    ///     The gradient being built: which kind it is, where it runs from and to, and the stops along
    ///     it. Ported from the WPF editor unchanged in what it does.
    /// </summary>
    public class GradientBrushEditorViewModel : ViewModels.ViewModelBase
    {
        #region construction and destruction

        public GradientBrushEditorViewModel()
        {
            _selectedColor = Colors.Black;

            GradientStops = new ObservableCollection<GradientStopViewModel>();
            GradientStops.CollectionChanged += GradientStopsCollectionChanged;

            AvailableBrushTypes = GradientBrushType.Linear | GradientBrushType.Radial;
            Brush = new LinearGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Colors.Red, 0.0),
                    new GradientStop(Colors.Black, 1.0)
                }
            };
        }

        #endregion

        #region public functions

        public ObservableCollection<GradientStopViewModel> GradientStops { get; }

        public string GradientOriginString
        {
            get => _gradientOriginString;
            set
            {
                if (SetProperty(ref _gradientOriginString, value)) OnPropertyChanged(nameof(Brush));
            }
        }

        public string CenterString
        {
            get => _centerString;
            set
            {
                if (SetProperty(ref _centerString, value)) OnPropertyChanged(nameof(Brush));
            }
        }

        public string RadiusXString
        {
            get => _radiusXString;
            set
            {
                if (SetProperty(ref _radiusXString, value)) OnPropertyChanged(nameof(Brush));
            }
        }

        public string RadiusYString
        {
            get => _radiusYString;
            set
            {
                if (SetProperty(ref _radiusYString, value)) OnPropertyChanged(nameof(Brush));
            }
        }

        public string StartPointString
        {
            get => _startPointString;
            set
            {
                if (SetProperty(ref _startPointString, value)) OnPropertyChanged(nameof(Brush));
            }
        }

        public string EndPointString
        {
            get => _endPointString;
            set
            {
                if (SetProperty(ref _endPointString, value)) OnPropertyChanged(nameof(Brush));
            }
        }

        public GradientBrushType AvailableBrushTypes
        {
            get => _availableBrushTypes;
            set
            {
                if (SetProperty(ref _availableBrushTypes, value))
                    OnPropertyChanged(nameof(AvailableBrushTypeValues));
            }
        }

        public IEnumerable<Enum> AvailableBrushTypeValues => GetFlags(AvailableBrushTypes);

        public GradientBrushType BrushType
        {
            get => _brushType;
            set
            {
                if (SetProperty(ref _brushType, value))
                {
                    OnPropertyChanged(nameof(Brush));
                    OnPropertyChanged(nameof(IsLinear));
                    OnPropertyChanged(nameof(IsRadial));
                }
            }
        }

        public bool IsLinear => BrushType == GradientBrushType.Linear;

        public bool IsRadial => BrushType == GradientBrushType.Radial;

        public GradientStopViewModel? SelectedGradientStop
        {
            get => _selectedGradientStop;
            set
            {
                if (SetProperty(ref _selectedGradientStop, value) && _selectedGradientStop is not null)
                    SelectedColor = _selectedGradientStop.Color;
            }
        }

        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (SetProperty(ref _selectedColor, value) && _selectedGradientStop is not null)
                    _selectedGradientStop.Color = value;
            }
        }

        public ICommand AddCommand => new RelayCommand(AddGradientStop);

        public ICommand RemoveCommand =>
            new RelayCommand(RemoveGradientStop, parameter => SelectedGradientStop is not null, true);

        public IBrush? Brush
        {
            get
            {
                if (BrushType == GradientBrushType.Linear)
                {
                    var brush = new LinearGradientBrush
                    {
                        StartPoint = ToRelativePoint(StartPointString),
                        EndPoint = ToRelativePoint(EndPointString)
                    };
                    foreach (GradientStopViewModel g in GradientStops)
                        brush.GradientStops.Add(new GradientStop(g.Color, g.Offset));
                    return brush;
                }

                if (BrushType == GradientBrushType.Radial)
                {
                    var brush = new RadialGradientBrush
                    {
                        GradientOrigin = ToRelativePoint(GradientOriginString),
                        Center = ToRelativePoint(CenterString),
                        RadiusX = new RelativeScalar(
                            ObsoleteAnyHelper.ConvertTo<double>(RadiusXString, false), RelativeUnit.Relative),
                        RadiusY = new RelativeScalar(
                            ObsoleteAnyHelper.ConvertTo<double>(RadiusYString, false), RelativeUnit.Relative)
                    };
                    foreach (GradientStopViewModel g in GradientStops)
                        brush.GradientStops.Add(new GradientStop(g.Color, g.Offset));
                    return brush;
                }

                return null;
            }
            set
            {
                GradientStops.Clear();

                var linearGradientBrush = value as ILinearGradientBrush;
                if (linearGradientBrush is not null)
                {
                    BrushType = GradientBrushType.Linear;
                    StartPointString = FromRelativePoint(linearGradientBrush.StartPoint);
                    EndPointString = FromRelativePoint(linearGradientBrush.EndPoint);

                    foreach (IGradientStop gradientStop in linearGradientBrush.GradientStops)
                        GradientStops.Add(new GradientStopViewModel(gradientStop));
                }

                var radialGradientBrush = value as IRadialGradientBrush;
                if (radialGradientBrush is not null)
                {
                    BrushType = GradientBrushType.Radial;
                    GradientOriginString = FromRelativePoint(radialGradientBrush.GradientOrigin);
                    CenterString = FromRelativePoint(radialGradientBrush.Center);
                    RadiusXString = ObsoleteAnyHelper.ConvertTo<string>(radialGradientBrush.RadiusX.Scalar, false)
                                    ?? @"0.5";
                    RadiusYString = ObsoleteAnyHelper.ConvertTo<string>(radialGradientBrush.RadiusY.Scalar, false)
                                    ?? @"0.5";

                    foreach (IGradientStop gradientStop in radialGradientBrush.GradientStops)
                        GradientStops.Add(new GradientStopViewModel(gradientStop));
                }
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     The points of a gradient are written as they were in WPF, as a pair of numbers between
        ///     zero and one; Avalonia carries them as relative points.
        /// </summary>
        private static RelativePoint ToRelativePoint(string text)
        {
            Point point = ObsoleteAnyHelper.ConvertTo<Point>(text, false);
            return new RelativePoint(point, RelativeUnit.Relative);
        }

        private static string FromRelativePoint(RelativePoint relativePoint)
        {
            return ObsoleteAnyHelper.ConvertTo<string>(relativePoint.Point, false) ?? @"0,0";
        }

        private static IEnumerable<Enum> GetFlags(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        private void GradientStopsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
                foreach (GradientStopViewModel viewModel in e.NewItems.OfType<GradientStopViewModel>())
                    viewModel.PropertyChanged += ViewModelPropertyChanged;

            if (e.OldItems is not null)
                foreach (GradientStopViewModel viewModel in e.OldItems.OfType<GradientStopViewModel>())
                    viewModel.PropertyChanged -= ViewModelPropertyChanged;

            OnPropertyChanged(nameof(Brush));
        }

        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Brush));
        }

        private void AddGradientStop(object? parameter)
        {
            GradientStops.Add(new GradientStopViewModel());
            SelectedGradientStop = GradientStops.Last();
        }

        private void RemoveGradientStop(object? parameter)
        {
            if (SelectedGradientStop is not null) GradientStops.Remove(SelectedGradientStop);
        }

        #endregion

        #region private fields

        private Color _selectedColor;

        private GradientBrushType _availableBrushTypes;
        private GradientBrushType _brushType = GradientBrushType.Linear;
        private string _gradientOriginString = @"0.5,0.5";
        private string _centerString = @"0.5,0.5";
        private string _radiusXString = @"0.5";
        private string _radiusYString = @"0.5";

        private GradientStopViewModel? _selectedGradientStop;
        private string _startPointString = @"1, 0";
        private string _endPointString = @"1, 1";

        #endregion
    }
}
