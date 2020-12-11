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

        #region public functions

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            YesButton = base.GetTemplateChild("PART_YesButton") as Button;
            NoButton = base.GetTemplateChild("PART_NoButton") as Button;
            YesForAllButton = base.GetTemplateChild("PART_YesForAllButton") as Button;
            NoForAllButton = base.GetTemplateChild("PART_NoForAllButton") as Button;
            OkButton = base.GetTemplateChild("PART_OkButton") as Button;
            CancelButton = base.GetTemplateChild("PART_CancelButton") as Button;
        }

        #endregion

        #region internal functions

        internal Button? YesButton { get; set; }
        internal Button? NoButton { get; set; }
        internal Button? YesForAllButton { get; set; }
        internal Button? NoForAllButton { get; set; }
        internal Button? OkButton { get; set; }
        internal Button? CancelButton { get; set; }

        #endregion

        #region protected functions

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WpfMessageBoxControlAutomationPeer(this);
        }

        #endregion
    }
}