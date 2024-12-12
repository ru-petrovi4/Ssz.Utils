using System.Windows.Controls;

using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class BrushEditorControl : UserControl
    {
        #region construction and destruction

        public BrushEditorControl()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public object? DsBrush
        {
            get
            {
                switch (MainTabControl.SelectedIndex)
                {
                    case 0:
                        return new SolidDsBrush {ColorString = SolidColorPicker.SelectedColorString ?? ""};
                    case 1:
                        return new XamlDsBrush {Brush = GradientBrushEditor.Brush};
                    case 2:
                        return new BlinkingDsBrush
                        {
                            FirstColorString = FirstColorPicker.SelectedColorString ?? "",
                            SecondColorString = SecondColorPicker.SelectedColorString ?? ""
                        };
                    case 3:
                        return ObsoleteAnyHelper.ConvertTo<int>(ParamNumBrushTextBox.Text, false);
                    default:
                        return null;
                }
            }
            set
            {
                if (value is null)
                {
                    SolidBrushTabItem.IsSelected = true;
                    SolidColorPicker.SelectedColorString = null;
                    return;
                }

                var solidDsBrush = value as SolidDsBrush;
                if (solidDsBrush is not null)
                {
                    SolidBrushTabItem.IsSelected = true;
                    SolidColorPicker.SelectedColorString = solidDsBrush.ColorString;
                    return;
                }

                var gradientDsBrush = value as XamlDsBrush;
                if (gradientDsBrush is not null)
                {
                    GradientBrushTabItem.IsSelected = true;
                    GradientBrushEditor.Brush = gradientDsBrush.Brush;
                    return;
                }

                var blinkingDsBrush = value as BlinkingDsBrush;
                if (blinkingDsBrush is not null)
                {
                    BlinkingBrushTabItem.IsSelected = true;
                    FirstColorPicker.SelectedColorString = blinkingDsBrush.FirstColorString;
                    SecondColorPicker.SelectedColorString = blinkingDsBrush.SecondColorString;
                    return;
                }

                if (value is int)
                {
                    ParamNumBrushTabItem.IsSelected = true;
                    ParamNumBrushTextBox.Text = ObsoleteAnyHelper.ConvertTo<string>((int) value, false);
                    return;
                }

                DefaultBrushTabItem.IsSelected = true;
            }
        }

        public void HideConstants()
        {
            SolidColorPicker.HideConstants();
            FirstColorPicker.HideConstants();
            SecondColorPicker.HideConstants();
        }

        #endregion
    }
}