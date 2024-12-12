using System.Windows.Media;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors.GradientBrushEditor
{
    internal class GradientStopViewModel : ViewModelBase
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

        public GradientStopViewModel(GradientStop gradientStop)
        {
            _color = gradientStop.Color;
            _offset = gradientStop.Offset;
        }

        #endregion

        #region public functions

        public double Offset
        {
            get => _offset;
            set
            {
                _offset = value;

                OnPropertyChangedAuto();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;

                OnPropertyChangedAuto();
                OnPropertyChanged(() => Brush);
            }
        }

        public SolidColorBrush Brush => new(Color);

        #endregion

        #region private fields

        private Color _color;
        private double _offset;

        #endregion
    }
}