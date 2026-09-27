using Avalonia.Media;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.VisualEditors.GradientBrushEditor
{
    /// <summary>
    ///     One stop of a gradient: a colour and where along the gradient it sits.
    /// </summary>
    public class GradientStopViewModel : ViewModelBase
    {
        #region construction and destruction

        public GradientStopViewModel()
        {
            _color = Colors.Black;
        }

        public GradientStopViewModel(Color color, double offset)
        {
            _color = color;
            _offset = offset;
        }

        public GradientStopViewModel(IGradientStop gradientStop)
        {
            _color = gradientStop.Color;
            _offset = gradientStop.Offset;
        }

        #endregion

        #region public functions

        public double Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (SetProperty(ref _color, value)) OnPropertyChanged(nameof(Brush));
            }
        }

        public IBrush Brush => new SolidColorBrush(Color);

        #endregion

        #region private fields

        private Color _color;
        private double _offset;

        #endregion
    }
}
