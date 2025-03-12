using System.Windows;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Play
{
    public partial class TouchScreenSetupWindow
    {
        #region construction and destruction

        public TouchScreenSetupWindow()
        {
            InitializeComponent();

            SetTouchScreenMode(PlayDsProjectView.TouchScreenMode);

            Closing += (sender, args) =>
            {
                PlayDsProjectView.TouchScreenMode = GetTouchScreenMode();
                PlayDsProjectView.SaveTouchScreenConfiguration();
            };
        }

        #endregion

        #region private functions

        private void Button1_OnClick(object sender, RoutedEventArgs e)
        {
            PlayDsProjectView.TouchScreenRect = ScreenHelper.GetSystemScreenWorkingArea(
                new Point(Left + ActualWidth / 2, Top + ActualHeight / 2));            
            
            Close();
        }

        private void Button2_OnClick(object sender, RoutedEventArgs e)
        {
            PlayDsProjectView.TouchScreenRect = new Rect(new Point(Left, Top), new Point(Left + ActualWidth, Top + ActualHeight));
            
            Close();
        }

        private void Button3_OnClick(object sender, RoutedEventArgs e)
        {
            PlayDsProjectView.TouchScreenRect = null;
            SetTouchScreenMode(TouchScreenMode.MouseClick);
            
            Close();
        }

        private TouchScreenMode GetTouchScreenMode()
        {
            if (TouchScreenModeMouseClickRadioButton.IsChecked == true) return TouchScreenMode.MouseClick;
            if (TouchScreenModeSingleTouchRadioButton.IsChecked == true) return TouchScreenMode.SingleTouch;
            if (TouchScreenModeMultiTouchRadioButton.IsChecked == true) return TouchScreenMode.MultiTouch;
            return TouchScreenMode.MouseClick;
        }

        private void SetTouchScreenMode(TouchScreenMode touchScreenMode)
        {
            switch (touchScreenMode)
            {
                case TouchScreenMode.MouseClick:
                    TouchScreenModeMouseClickRadioButton.IsChecked = true;
                    break;
                case TouchScreenMode.SingleTouch:
                    TouchScreenModeSingleTouchRadioButton.IsChecked = true;
                    break;
                case TouchScreenMode.MultiTouch:
                    TouchScreenModeMultiTouchRadioButton.IsChecked = true;
                    break;
                default:
                    TouchScreenModeMouseClickRadioButton.IsChecked = true;
                    break;
            }
        }

        #endregion
    }
}