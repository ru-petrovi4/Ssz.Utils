using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class RubberbandAdorner : Adorner
    {
        #region private fields

        private readonly Pen _rubberbandPen;

        #endregion

        #region construction and destruction

        public RubberbandAdorner(DesignDrawingBorder designerDrawingBorder, Point startPoint)
            : base(designerDrawingBorder)
        {
            StartPoint = startPoint;
            _rubberbandPen = new Pen(Brushes.LightSlateGray, 1);
            _rubberbandPen.DashStyle = new DashStyle(new double[] {2}, 1);

            IsHitTestVisible = false;
        }

        #endregion

        #region protected functions

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(null, _rubberbandPen, new Rect(StartPoint, EndPoint));
        }

        #endregion

        #region public fields

        public readonly Point StartPoint;

        public Point EndPoint;

        #endregion
    }
}