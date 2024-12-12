using System.Windows.Media;
using System.Windows.Shapes;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ConnectionPointDsShapeView : DsShapeViewBase
    {
        #region construction and destruction

        public ConnectionPointDsShapeView(ConnectionPointDsShape dsShape, Frame? frame)
            : base(dsShape, frame)
        {
            IsHitTestVisible = false;

            Content = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Red
            };
        }

        #endregion

        #region public functions

        public ConnectionPointInfo? ConnectionPointInfo { get; set; }

        #endregion
    }
}