using Avalonia.Controls;
using Avalonia.Media;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     Choosing what a shape is painted with: one colour, a gradient, two colours it blinks
    ///     between, a number of the parameter that carries the colour at run time, or nothing at all.
    ///     <para>
    ///         Ported from the WPF editor, tab for tab; which tab is open is what decides the kind of
    ///         brush the dialog answers with.
    ///     </para>
    /// </summary>
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
                        return new SolidDsBrush { ColorString = SolidColorPicker.SelectedColorString ?? @"" };
                    case 1:
                        return new XamlDsBrush { Brush = GradientBrushEditor.Brush as Brush };
                    case 2:
                        return new BlinkingDsBrush
                        {
                            FirstColorString = FirstColorPicker.SelectedColorString ?? @"",
                            SecondColorString = SecondColorPicker.SelectedColorString ?? @""
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
                    MainTabControl.SelectedItem = SolidBrushTabItem;
                    SolidColorPicker.SelectedColorString = null;
                    return;
                }

                var solidDsBrush = value as SolidDsBrush;
                if (solidDsBrush is not null)
                {
                    MainTabControl.SelectedItem = SolidBrushTabItem;
                    SolidColorPicker.SelectedColorString = solidDsBrush.ColorString;
                    return;
                }

                var gradientDsBrush = value as XamlDsBrush;
                if (gradientDsBrush is not null)
                {
                    MainTabControl.SelectedItem = GradientBrushTabItem;
                    GradientBrushEditor.Brush = gradientDsBrush.Brush;
                    return;
                }

                var blinkingDsBrush = value as BlinkingDsBrush;
                if (blinkingDsBrush is not null)
                {
                    MainTabControl.SelectedItem = BlinkingBrushTabItem;
                    FirstColorPicker.SelectedColorString = blinkingDsBrush.FirstColorString;
                    SecondColorPicker.SelectedColorString = blinkingDsBrush.SecondColorString;
                    return;
                }

                if (value is int paramNum)
                {
                    MainTabControl.SelectedItem = ParamNumBrushTabItem;
                    ParamNumBrushTextBox.Text = ObsoleteAnyHelper.ConvertTo<string>(paramNum, false);
                    return;
                }

                MainTabControl.SelectedItem = DefaultBrushTabItem;
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
