using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     The dashed rectangle the author drags over the drawing to pick several shapes at once.
    ///     <para>
    ///         In WPF this was an Adorner; Avalonia has an adorner layer that takes any control, so this
    ///         is a control that draws the rectangle and lets every click through.
    ///     </para>
    /// </summary>
    public class RubberbandAdorner : Control
    {
        #region construction and destruction

        public RubberbandAdorner(DesignDrawingBorder designerDrawingBorder, Point startPoint)
        {
            DesignDrawingBorder = designerDrawingBorder;
            StartPoint = startPoint;
            EndPoint = startPoint;

            _rubberbandPen = new Pen(Brushes.LightSlateGray, 1)
            {
                DashStyle = new DashStyle(new double[] { 2 }, 1)
            };

            IsHitTestVisible = false;
        }

        #endregion

        #region public functions

        public DesignDrawingBorder DesignDrawingBorder { get; }

        public readonly Point StartPoint;

        public Point EndPoint
        {
            get => _endPoint;
            set
            {
                _endPoint = value;
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.DrawRectangle(null, _rubberbandPen, new Rect(StartPoint, EndPoint));
        }

        #endregion

        #region private fields

        private readonly Pen _rubberbandPen;
        private Point _endPoint;

        #endregion
    }
}
