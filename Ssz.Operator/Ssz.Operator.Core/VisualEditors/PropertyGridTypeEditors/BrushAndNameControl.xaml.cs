using System.Windows;
using System.Windows.Controls;
using Ssz.Operator.Core.Constants;

using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class BrushAndNameControl : UserControl
    {
        #region construction and destruction

        public BrushAndNameControl(DsBrushBase dsBrush)
        {
            InitializeComponent();

            var solidDsBrush = dsBrush as SolidDsBrush;
            if (solidDsBrush is not null &&
                ConstantsHelper.ContainsQuery(solidDsBrush.ColorString))
            {
                Border.Visibility = Visibility.Collapsed;
                TextBlock.Text = solidDsBrush.ColorString;
                return;
            }

            var blinkingDsBrush = dsBrush as BlinkingDsBrush;
            if (blinkingDsBrush is not null &&
                (ConstantsHelper.ContainsQuery(blinkingDsBrush.FirstColorString) ||
                 ConstantsHelper.ContainsQuery(blinkingDsBrush.SecondColorString)))
            {
                Border.Visibility = Visibility.Collapsed;
                TextBlock.Text = blinkingDsBrush.FirstColorString + @";" + blinkingDsBrush.SecondColorString;
                return;
            }

            if (solidDsBrush is not null) TextBlock.Text = ObsoleteAnyHelper.ConvertTo<string>(solidDsBrush.Color, false);

            Rectangle.Fill = dsBrush.GetBrush(null);
        }

        #endregion
    }
}