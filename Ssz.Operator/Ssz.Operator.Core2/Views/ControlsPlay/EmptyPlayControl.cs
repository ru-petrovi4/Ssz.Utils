using Ssz.Operator.Core.Drawings;

namespace Ssz.Operator.Core.ControlsPlay
{
    public class EmptyPlayControl : PlayControlBase
    {
        #region construction and destruction

        public EmptyPlayControl(IPlayWindow playWindow) :
            base(playWindow)
        {
        }

        #endregion

        #region public functions

        public override void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {
        }

        #endregion
    }
}