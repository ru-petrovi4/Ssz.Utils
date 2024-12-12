using System.Windows;
using Ssz.Operator.Core.Properties;
using Ssz.Utils.Wpf.WpfMessageBox;

namespace Ssz.Operator.Core
{
    public static class MessageBoxHelper
    {
        #region public functions

        public static Window? GetRootWindow()
        {
            if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.WindowsPlayMode)
                return PlayDsProjectView.LastActiveRootPlayWindow as Window;
            return Application.Current.MainWindow;
        }

        public static void ShowInfo(string messageBoxText)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var rootWindow = GetRootWindow();
                if (rootWindow is not null)
                    WpfMessageBox.Show(rootWindow, messageBoxText, Resources.InfoMessageBoxCaption,
                        WpfMessageBoxButton.OK,
                        MessageBoxImage.Information, WpfMessageBoxResult.OK);
                else
                    WpfMessageBox.Show(messageBoxText, Resources.InfoMessageBoxCaption,
                        WpfMessageBoxButton.OK,
                        MessageBoxImage.Information, WpfMessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            });
        }

        public static void ShowWarning(string messageBoxText)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var rootWindow = GetRootWindow();
                if (rootWindow is not null)
                    WpfMessageBox.Show(rootWindow, messageBoxText, Resources.WarningMessageBoxCaption,
                        WpfMessageBoxButton.OK,
                        MessageBoxImage.Warning, WpfMessageBoxResult.OK);
                else
                    WpfMessageBox.Show(messageBoxText, Resources.WarningMessageBoxCaption,
                        WpfMessageBoxButton.OK,
                        MessageBoxImage.Warning, WpfMessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            });
        }

        public static void ShowError(string messageBoxText)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var rootWindow = GetRootWindow();
                if (rootWindow is not null)
                    WpfMessageBox.Show(rootWindow, messageBoxText, Resources.ErrorMessageBoxCaption,
                        WpfMessageBoxButton.OK,
                        MessageBoxImage.Error, WpfMessageBoxResult.OK);
                else
                    WpfMessageBox.Show(messageBoxText, Resources.ErrorMessageBoxCaption,
                        WpfMessageBoxButton.OK,
                        MessageBoxImage.Error, WpfMessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            });
        }

        #endregion
    }
}