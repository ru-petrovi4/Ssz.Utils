using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Ssz.Utils.Wpf.WpfMessageBox
{
    [TemplatePart(Name = "PART_YesButton", Type = typeof (Button))]
    [TemplatePart(Name = "PART_NoButton", Type = typeof (Button))]
    [TemplatePart(Name = "PART_YesForAllButton", Type = typeof (Button))]
    [TemplatePart(Name = "PART_NoForAllButton", Type = typeof (Button))]
    [TemplatePart(Name = "PART_OkButton", Type = typeof (Button))]
    [TemplatePart(Name = "PART_CancelButton", Type = typeof (Button))]
    public class WpfMessageBoxControl : Control
    {
        #region construction and destruction

        static WpfMessageBoxControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (WpfMessageBoxControl),
                new FrameworkPropertyMetadata(typeof (WpfMessageBoxControl)));
        }

        #endregion

        #region protected functions

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WpfMessageBoxControlAutomationPeer(this);
        }

        #endregion
    }
}