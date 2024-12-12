using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    public partial class LinesGrid : UserControl
    {
        #region construction and destruction

        public LinesGrid()
        {
            InitializeComponent();
            DataContext = this;
            SizeChanged += OnSizeChanged;
        }

        #endregion

        #region public functions

        public static void StepOnChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is null) return;
            ((LinesGrid) sender).RecreateLines();
        }


        public static readonly DependencyProperty StepProperty = DependencyProperty.Register("Step", typeof(int),
            typeof(LinesGrid), new PropertyMetadata(1, StepOnChanged));


        public static readonly DependencyProperty LineThicknessProperty = DependencyProperty.Register("LineThickness",
            typeof(double), typeof(LinesGrid), new PropertyMetadata(1d));

        public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register("LineBrush",
            typeof(Brush), typeof(LinesGrid), new PropertyMetadata(Brushes.Black));


        public int Step
        {
            get => (int) GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }


        public double LineThickness
        {
            get => (double) GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        public Brush LineBrush
        {
            get => (Brush) GetValue(LineBrushProperty);
            set => SetValue(LineBrushProperty, value);
        }

        #endregion

        #region private functions

        private void OnSizeChanged(object? sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            RecreateLines();
        }

        private void RecreateLines()
        {
            if (double.IsNaN(ActualWidth) || double.IsNaN(ActualHeight) || ActualWidth < 1d ||
                ActualHeight < 1d) return;

            double cellWidth = Step;
            VerticalLinesBrush.Viewbox = new Rect(0, 0, cellWidth, 1);

            double cellHeight = Step;
            HorizontalLinesBrush.Viewbox = new Rect(0, 0, 1, cellHeight);

            var columns = ActualWidth / cellWidth;
            var rows = ActualHeight / cellHeight;

            var qv = (1d - LineThickness / ActualWidth) / columns;
            VerticalLinesBrush.Viewport = new Rect(0, 0, qv, 1);

            var qh = (1d - LineThickness / ActualHeight) / rows;
            HorizontalLinesBrush.Viewport = new Rect(0, 0, 1, qh);
        }

        #endregion
    }
}