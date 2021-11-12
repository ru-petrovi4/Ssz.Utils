using System.Windows;

namespace Ssz.Utils.Wpf.WpfMessageBox
{
    public static class WpfMessageBox
    {
        //
        // Summary:
        //     Displays a message box that has a message and that returns a result.
        //
        // Parameters:
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.

        #region public functions

        public static WpfMessageBoxResult Show(string messageBoxText)
        {
            return ShowCore(null, messageBoxText);
        }

        //
        // Summary:
        //     Displays a message box that has a message and title bar caption; and that
        //     returns a result.
        //
        // Parameters:
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(string messageBoxText, string caption)
        {
            return ShowCore(null, messageBoxText, caption);
        }

        //
        // Summary:
        //     Displays a message box in front of the specified window. The message box
        //     displays a message and returns a result.
        //
        // Parameters:
        //   owner:
        //     A System.Windows.Window that represents the owner window of the message box.
        //
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(Window? owner, string messageBoxText)
        {
            return ShowCore(owner, messageBoxText);
        }

        //
        // Summary:
        //     Displays a message box that has a message, title bar caption, and button;
        //     and that returns a result.
        //
        // Parameters:
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        //   button:
        //     A System.Windows.WPFMessageBoxButton value that specifies which button or buttons
        //     to display.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(string messageBoxText, string caption, WpfMessageBoxButton button)
        {
            return ShowCore(null, messageBoxText, caption, button);
        }

        //
        // Summary:
        //     Displays a message box in front of the specified window. The message box
        //     displays a message and title bar caption; and it returns a result.
        //
        // Parameters:
        //   owner:
        //     A System.Windows.Window that represents the owner window of the message box.
        //
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(Window? owner, string messageBoxText, string caption)
        {
            return ShowCore(owner, messageBoxText, caption);
        }

        //
        // Summary:
        //     Displays a message box that has a message, title bar caption, button, and
        //     icon; and that returns a result.
        //
        // Parameters:
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        //   button:
        //     A System.Windows.WPFMessageBoxButton value that specifies which button or buttons
        //     to display.
        //
        //   icon:
        //     A System.Windows.MessageBoxImage value that specifies the icon to display.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(string messageBoxText, string caption, WpfMessageBoxButton button,
            MessageBoxImage icon)
        {
            return ShowCore(null, messageBoxText, caption, button, icon);
        }

        //
        // Summary:
        //     Displays a message box in front of the specified window. The message box
        //     displays a message, title bar caption, and button; and it also returns a
        //     result.
        //
        // Parameters:
        //   owner:
        //     A System.Windows.Window that represents the owner window of the message box.
        //
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        //   button:
        //     A System.Windows.WPFMessageBoxButton value that specifies which button or buttons
        //     to display.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            WpfMessageBoxButton button)
        {
            return ShowCore(owner, messageBoxText, caption, button);
        }

        //
        // Summary:
        //     Displays a message box that has a message, title bar caption, button, and
        //     icon; and that accepts a default message box result and returns a result.
        //
        // Parameters:
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        //   button:
        //     A System.Windows.WPFMessageBoxButton value that specifies which button or buttons
        //     to display.
        //
        //   icon:
        //     A System.Windows.MessageBoxImage value that specifies the icon to display.
        //
        //   defaultResult:
        //     A System.Windows.WPFMessageBoxResult value that specifies the default result
        //     of the message box.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(string messageBoxText, string caption, WpfMessageBoxButton button,
            MessageBoxImage icon, WpfMessageBoxResult defaultResult)
        {
            return ShowCore(null, messageBoxText, caption, button, icon, defaultResult);
        }

        //
        // Summary:
        //     Displays a message box in front of the specified window. The message box
        //     displays a message, title bar caption, button, and icon; and it also returns
        //     a result.
        //
        // Parameters:
        //   owner:
        //     A System.Windows.Window that represents the owner window of the message box.
        //
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        //   button:
        //     A System.Windows.WPFMessageBoxButton value that specifies which button or buttons
        //     to display.
        //
        //   icon:
        //     A System.Windows.MessageBoxImage value that specifies the icon to display.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            WpfMessageBoxButton button, MessageBoxImage icon)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon);
        }

        //
        // Summary:
        //     Displays a message box that has a message, title bar caption, button, and
        //     icon; and that accepts a default message box result, complies with the specified
        //     options, and returns a result.
        //
        // Parameters:
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        //   button:
        //     A System.Windows.WPFMessageBoxButton value that specifies which button or buttons
        //     to display.
        //
        //   icon:
        //     A System.Windows.MessageBoxImage value that specifies the icon to display.
        //
        //   defaultResult:
        //     A System.Windows.WPFMessageBoxResult value that specifies the default result
        //     of the message box.
        //
        //   options:
        //     A System.Windows.MessageBoxOptions value object that specifies the options.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(string messageBoxText, string caption, WpfMessageBoxButton button,
            MessageBoxImage icon, WpfMessageBoxResult defaultResult, MessageBoxOptions options)
        {
            return ShowCore(null, messageBoxText, caption, button, icon, defaultResult, options);
        }

        //
        // Summary:
        //     Displays a message box in front of the specified window. The message box
        //     displays a message, title bar caption, button, and icon; and accepts a default
        //     message box result and returns a result.
        //
        // Parameters:
        //   owner:
        //     A System.Windows.Window that represents the owner window of the message box.
        //
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        //   button:
        //     A System.Windows.WPFMessageBoxButton value that specifies which button or buttons
        //     to display.
        //
        //   icon:
        //     A System.Windows.MessageBoxImage value that specifies the icon to display.
        //
        //   defaultResult:
        //     A System.Windows.WPFMessageBoxResult value that specifies the default result
        //     of the message box.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            WpfMessageBoxButton button, MessageBoxImage icon, WpfMessageBoxResult defaultResult)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, defaultResult);
        }

        //
        // Summary:
        //     Displays a message box in front of the specified window. The message box
        //     displays a message, title bar caption, button, and icon; and accepts a default
        //     message box result, complies with the specified options, and returns a result.
        //
        // Parameters:
        //   owner:
        //     A System.Windows.Window that represents the owner window of the message box.
        //
        //   messageBoxText:
        //     A System.String that specifies the text to display.
        //
        //   caption:
        //     A System.String that specifies the title bar caption to display.
        //
        //   button:
        //     A System.Windows.WPFMessageBoxButton value that specifies which button or buttons
        //     to display.
        //
        //   icon:
        //     A System.Windows.MessageBoxImage value that specifies the icon to display.
        //
        //   defaultResult:
        //     A System.Windows.WPFMessageBoxResult value that specifies the default result
        //     of the message box.
        //
        //   options:
        //     A System.Windows.MessageBoxOptions value object that specifies the options.
        //
        // Returns:
        //     A System.Windows.WPFMessageBoxResult value that specifies which message box
        //     button is clicked by the user.
        public static WpfMessageBoxResult Show(Window? owner, string messageBoxText, string caption,
            WpfMessageBoxButton button, MessageBoxImage icon, WpfMessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, defaultResult, options);
        }

        #endregion

        #region private functions

        private static WpfMessageBoxResult ShowCore(
            Window? owner,
            string messageBoxText,
            string caption = "",
            WpfMessageBoxButton button = WpfMessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            WpfMessageBoxResult defaultResult = WpfMessageBoxResult.None,
            MessageBoxOptions options = MessageBoxOptions.None)
        {
            return WpfMessageBoxWindow.Show(
                delegate(Window messageBoxWindow) {
                    try
                    {
                        if (owner is not null)
                            messageBoxWindow.Owner = owner;
                    }
                    catch { }
                },
                messageBoxText, caption, button, icon, defaultResult, options);
        }

        #endregion
    }
}