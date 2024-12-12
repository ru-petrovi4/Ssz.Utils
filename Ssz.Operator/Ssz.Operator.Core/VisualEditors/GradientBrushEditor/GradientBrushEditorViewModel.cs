using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors.GradientBrushEditor
{
    internal class GradientBrushEditorViewModel : ViewModelBase
    {
        #region construction and destruction

        public GradientBrushEditorViewModel()
        {
            _selectedColor = Colors.Black;

            GradientStops = new ObservableCollection<GradientStopViewModel>();
            GradientStops.CollectionChanged += GradientStopsCollectionChanged;

            AvailableBrushTypes = GradientBrushType.Linear | GradientBrushType.Radial;
            Brush = new LinearGradientBrush(Colors.Red, Colors.Black, 45);
        }

        #endregion

        #region public functions

        public ObservableCollection<GradientStopViewModel> GradientStops { get; }

        public string GradientOriginString
        {
            get => _gradientOriginString;
            set
            {
                if (SetValue(ref _gradientOriginString, value)) OnPropertyChanged(() => Brush);
            }
        }

        public string CenterString
        {
            get => _centerString;
            set
            {
                if (SetValue(ref _centerString, value)) OnPropertyChanged(() => Brush);
            }
        }

        public string RadiusXString
        {
            get => _radiusXString;
            set
            {
                if (SetValue(ref _radiusXString, value)) OnPropertyChanged(() => Brush);
            }
        }

        public string RadiusYString
        {
            get => _radiusYString;
            set
            {
                if (SetValue(ref _radiusYString, value)) OnPropertyChanged(() => Brush);
            }
        }

        public string StartPointString
        {
            get => _startPointString;
            set
            {
                if (SetValue(ref _startPointString, value)) OnPropertyChanged(() => Brush);
            }
        }

        public string EndPointString
        {
            get => _endPointString;
            set
            {
                if (SetValue(ref _endPointString, value)) OnPropertyChanged(() => Brush);
            }
        }

        public GradientBrushType AvailableBrushTypes
        {
            get => _availableBrushTypes;
            set
            {
                if (SetValue(ref _availableBrushTypes, value)) OnPropertyChanged(() => AvailableBrushTypeValues);
            }
        }

        public IEnumerable<Enum> AvailableBrushTypeValues => GetFlags(AvailableBrushTypes);

        public GradientBrushType BrushType
        {
            get => _brushType;
            set
            {
                if (SetValue(ref _brushType, value)) OnPropertyChanged(() => Brush);
            }
        }

        public GradientStopViewModel? SelectedGradientStop
        {
            get => _selectedGradientStop;
            set
            {
                if (SetValue(ref _selectedGradientStop, value))
                    if (_selectedGradientStop is not null)
                        SelectedColor = _selectedGradientStop.Color;
            }
        }

        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (SetValue(ref _selectedColor, value))
                    if (_selectedGradientStop is not null)
                        _selectedGradientStop.Color = value;
            }
        }

        public RelayCommand AddCommand => new(AddGradientStop);

        public RelayCommand RemoveCommand
        {
            get { return new(RemoveGradientStop, parameter => SelectedGradientStop is not null, true); }
        }

        public Brush? Brush
        {
            get
            {
                if (BrushType == GradientBrushType.Linear)
                {
                    var brush = new LinearGradientBrush();
                    brush.StartPoint = ObsoleteAnyHelper.ConvertTo<Point>(StartPointString, false);
                    brush.EndPoint = ObsoleteAnyHelper.ConvertTo<Point>(EndPointString, false);

                    foreach (GradientStopViewModel g in GradientStops)
                        brush.GradientStops.Add(new GradientStop(g.Color, g.Offset));
                    return brush;
                }

                if (BrushType == GradientBrushType.Radial)
                {
                    var brush = new RadialGradientBrush();
                    brush.GradientOrigin = ObsoleteAnyHelper.ConvertTo<Point>(GradientOriginString, false);
                    brush.Center = ObsoleteAnyHelper.ConvertTo<Point>(CenterString, false);
                    brush.RadiusX = ObsoleteAnyHelper.ConvertTo<double>(RadiusXString, false);
                    brush.RadiusY = ObsoleteAnyHelper.ConvertTo<double>(RadiusYString, false);
                    foreach (GradientStopViewModel g in GradientStops)
                        brush.GradientStops.Add(new GradientStop(g.Color, g.Offset));
                    return brush;
                }

                return null;
            }
            set
            {
                GradientStops.Clear();

                var gradientBrush = value as LinearGradientBrush;
                if (gradientBrush is not null)
                {
                    BrushType = GradientBrushType.Linear;
                    StartPointString = ObsoleteAnyHelper.ConvertTo<string>(gradientBrush.StartPoint, false);
                    EndPointString = ObsoleteAnyHelper.ConvertTo<string>(gradientBrush.EndPoint, false);

                    for (var n = 0; n < gradientBrush.GradientStops.Count; n += 1)
                        GradientStops.Add(new GradientStopViewModel(gradientBrush.GradientStops[n]));
                }

                var radialGradientBrush = value as RadialGradientBrush;
                if (radialGradientBrush is not null)
                {
                    BrushType = GradientBrushType.Radial;
                    GradientOriginString = ObsoleteAnyHelper.ConvertTo<string>(radialGradientBrush.GradientOrigin, false);
                    CenterString = ObsoleteAnyHelper.ConvertTo<string>(radialGradientBrush.Center, false);
                    RadiusXString = ObsoleteAnyHelper.ConvertTo<string>(radialGradientBrush.RadiusX, false);
                    RadiusYString = ObsoleteAnyHelper.ConvertTo<string>(radialGradientBrush.RadiusY, false);

                    for (var n = 0; n < radialGradientBrush.GradientStops.Count; n += 1)
                        GradientStops.Add(new GradientStopViewModel(radialGradientBrush.GradientStops[n]));
                }
            }
        }

        #endregion

        #region private functions

        private static IEnumerable<Enum> GetFlags(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }


        private void GradientStopsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
                foreach (GradientStopViewModel viewModel in e.NewItems)
                    viewModel.PropertyChanged += ViewModelPropertyChanged;

            if (e.OldItems is not null)
                foreach (GradientStopViewModel viewModel in e.OldItems)
                    viewModel.PropertyChanged -= ViewModelPropertyChanged;

            OnPropertyChanged(() => Brush);
        }

        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(() => Brush);
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
        private string _gradientOriginString = "0.5,0.5";
        private string _centerString = "0.5,0.5";
        private string _radiusXString = "0.5";
        private string _radiusYString = "0.5";

        private GradientStopViewModel? _selectedGradientStop;
        private string _startPointString = "1, 0";
        private string _endPointString = "1, 1";

        #endregion
    }
}