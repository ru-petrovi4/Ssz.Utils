using System;
using System.Media;
using System.Windows;
using System.Windows.Input;

namespace Ssz.Utils.Wpf.WpfMessageBox
{
    /// <summary>
    ///     Interaction logic for WPFMessageBoxWindow.xaml
    /// </summary>
    public partial class WpfMessageBoxWindow : Window
    {
        #region construction and destruction

        public WpfMessageBoxWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public static WpfMessageBoxResult Show(
            Action<Window> setOwner,
            string messageBoxText,
            string caption,
            WpfMessageBoxButton button,
            MessageBoxImage icon,
            WpfMessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            if ((options & MessageBoxOptions.DefaultDesktopOnly) == MessageBoxOptions.DefaultDesktopOnly)
            {
                return ToWpfMessageBoxResult(MessageBox.Show(messageBoxText,
                    caption,
                    ToMessageBoxButton(button),
                    icon,
                    ToMessageBoxResult(defaultResult),
                    options));
            }

            if ((options & MessageBoxOptions.ServiceNotification) == MessageBoxOptions.ServiceNotification)
            {
                return ToWpfMessageBoxResult(MessageBox.Show(messageBoxText,
                    caption,
                    ToMessageBoxButton(button),
                    icon,
                    ToMessageBoxResult(defaultResult),
                    options));
            }

            _messageBoxWindow = new WpfMessageBoxWindow();

            setOwner(_messageBoxWindow);

            PlayMessageBeep(icon);

            _messageBoxWindow._viewModel = new MessageBoxViewModel(_messageBoxWindow, caption, messageBoxText, button,
                icon, defaultResult, options);
            _messageBoxWindow.DataContext = _messageBoxWindow._viewModel;
            _messageBoxWindow.ShowDialog();
            return _messageBoxWindow._viewModel.Result;
        }        

        #endregion

        #region protected functions

        protected override void OnSourceInitialized(EventArgs e)
        {
            // removes the application icon from the window top left corner
            // this is different than just hiding it
            WindowHelper.RemoveIcon(this);
            if (_viewModel is null) throw new InvalidOperationException();
            switch (_viewModel.Options)
            {
                case MessageBoxOptions.None:
                    break;

                case MessageBoxOptions.RightAlign:
                    WindowHelper.SetRightAligned(this);
                    break;

                case MessageBoxOptions.RtlReading:
                    break;

                case MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading:
                    break;
            }

            // disable close button if needed and remove resize menu items from the window system menu
            var systemMenuHelper = new SystemMenuHelper(this);

            if (_viewModel.ButtonOption == WpfMessageBoxButton.YesNo)
            {
                systemMenuHelper.DisableCloseButton = true;
            }

            systemMenuHelper.RemoveResizeMenu = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_viewModel is null) throw new InvalidOperationException();
            _viewModel.CloseCommand.Execute(null);
        }

        #endregion

        #region private functions

        private static MessageBoxButton ToMessageBoxButton(WpfMessageBoxButton button)
        {
            return (MessageBoxButton)(int)button;
        }

        private static MessageBoxResult ToMessageBoxResult(WpfMessageBoxResult result)
        {
            return (MessageBoxResult)(int)result;
        }

        private static WpfMessageBoxResult ToWpfMessageBoxResult(MessageBoxResult result)
        {
            return (WpfMessageBoxResult)(int)result;
        }

        private static void PlayMessageBeep(MessageBoxImage icon)
        {
            switch (icon)
            {
                    //case MessageBoxImage.Hand:
                    //case MessageBoxImage.Stop:
                case MessageBoxImage.Error:
                    SystemSounds.Hand.Play();
                    break;

                    //case MessageBoxImage.Exclamation:
                case MessageBoxImage.Warning:
                    SystemSounds.Exclamation.Play();
                    break;

                case MessageBoxImage.Question:
                    SystemSounds.Question.Play();
                    break;

                    //case MessageBoxImage.Asterisk:
                case MessageBoxImage.Information:
                    SystemSounds.Asterisk.Play();
                    break;

                default:
                    SystemSounds.Beep.Play();
                    break;
            }
        }

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_viewModel is null) throw new InvalidOperationException();
                _viewModel.EscapeCommand.Execute(null);
            }
        }

        #endregion

        #region private fields

        [ThreadStatic] private static WpfMessageBoxWindow? _messageBoxWindow;
        private MessageBoxViewModel? _viewModel;

        #endregion
    }
}