using System.Windows.Controls;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class DesignControlsInfo
    {
        #region construction and destruction

        public DesignControlsInfo(DesignDrawingCanvas designDrawingCanvas)
        {
            DesignDrawingCanvas = designDrawingCanvas;
        }

        #endregion

        #region public functions

        public DesignDrawingCanvas DesignDrawingCanvas { get; }

        public ScrollViewer? ScrollViewer { get; set; }

        #endregion
    }
}