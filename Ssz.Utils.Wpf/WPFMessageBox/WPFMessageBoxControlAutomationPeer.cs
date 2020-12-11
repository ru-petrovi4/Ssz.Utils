using System.Windows.Automation.Peers;

namespace Ssz.Utils.Wpf.WpfMessageBox
{
    internal class WpfMessageBoxControlAutomationPeer : UIElementAutomationPeer
    {
        #region construction and destruction

        public WpfMessageBoxControlAutomationPeer(WpfMessageBoxControl owner) : base(owner)
        {
        }

        #endregion

        #region protected functions

        protected override string GetClassNameCore()
        {
            return "WPFMessageBoxControl";
        }

        #endregion
    }
}