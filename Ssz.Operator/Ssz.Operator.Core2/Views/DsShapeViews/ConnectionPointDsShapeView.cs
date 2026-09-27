using Avalonia.Media;
using Avalonia.Controls.Shapes;
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

        /// <summary>
        ///     What the drawing surface knows about this connection point: which connectors start and
        ///     end at it. The surface fills it in and clears it again.
        /// </summary>
        public ControlsDesign.ConnectionPointInfo? ConnectionPointInfo { get; set; }


        //public ConnectionPointInfo? ConnectionPointInfo { get; set; }

        #endregion
    }
}