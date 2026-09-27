using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     The grid drawn over the drawing while the discrete mode is on, so the author sees what the
    ///     shapes snap to.
    ///     <para>
    ///         The WPF version tiled a VisualBrush holding a single line and then adjusted its viewport
    ///         to the step. Here the lines are drawn straight into the drawing context, which is both
    ///         simpler and exact at any step.
    ///     </para>
    /// </summary>
    public class LinesGrid : Control
    {
        #region public functions

        public static readonly StyledProperty<int> StepProperty =
            AvaloniaProperty.Register<LinesGrid, int>(nameof(Step), 1);

        public static readonly StyledProperty<double> LineThicknessProperty =
            AvaloniaProperty.Register<LinesGrid, double>(nameof(LineThickness), 1d);

        public static readonly StyledProperty<IBrush?> LineBrushProperty =
            AvaloniaProperty.Register<LinesGrid, IBrush?>(nameof(LineBrush), Brushes.Black);

        public int Step
        {
            get => GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        public double LineThickness
        {
            get => GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        public IBrush? LineBrush
        {
            get => GetValue(LineBrushProperty);
            set => SetValue(LineBrushProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var step = Step;
            IBrush? lineBrush = LineBrush;
            if (step < 1 || lineBrush is null) return;

            Rect bounds = Bounds;
            if (bounds.Width < 1d || bounds.Height < 1d) return;

            // A step of one pixel would mean a line per pixel: a solid fill that says nothing and costs
            // a lot to draw.
            if (bounds.Width / step > MaxLines || bounds.Height / step > MaxLines) return;

            var pen = new Pen(lineBrush, LineThickness);

            for (double x = step; x < bounds.Width; x += step)
                context.DrawLine(pen, new Point(x, 0), new Point(x, bounds.Height));

            for (double y = step; y < bounds.Height; y += step)
                context.DrawLine(pen, new Point(0, y), new Point(bounds.Width, y));
        }

        #endregion

        #region protected functions

        static LinesGrid()
        {
            AffectsRender<LinesGrid>(StepProperty, LineThicknessProperty, LineBrushProperty);
        }

        #endregion

        #region private fields

        private const int MaxLines = 2000;

        #endregion
    }
}
