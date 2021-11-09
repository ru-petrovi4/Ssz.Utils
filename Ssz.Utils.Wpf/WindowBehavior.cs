using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Ssz.Utils.Wpf
{
    public class WindowBehavior
    {
        #region construction and destruction

        static WindowBehavior()
        {
            IsHiddenCloseButtonProperty = IsHiddenCloseButtonKey.DependencyProperty;
        }

        #endregion

        #region public functions

        [AttachedPropertyBrowsableForType(typeof (Window))]
        public static bool GetHideCloseButton(Window obj)
        {
            return (bool) obj.GetValue(HideCloseButtonProperty);
        }

        [AttachedPropertyBrowsableForType(typeof (Window))]
        public static void SetHideCloseButton(Window obj, bool value)
        {
            if (obj is null) return;
            obj.SetValue(HideCloseButtonProperty, value);
        }

        [AttachedPropertyBrowsableForType(typeof (Window))]
        public static bool GetIsHiddenCloseButton(Window obj)
        {
            return (bool) obj.GetValue(IsHiddenCloseButtonProperty);
        }

        public static readonly DependencyProperty HideCloseButtonProperty =
            DependencyProperty.RegisterAttached(
                "HideCloseButton",
                typeof (bool),
                typeof (WindowBehavior),
                new FrameworkPropertyMetadata(false, HideCloseButtonChangedCallback));

        public static readonly DependencyProperty IsHiddenCloseButtonProperty;

        #endregion

        #region private functions

        private static void HideCloseButtonChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window is null) return;

            var hideCloseButton = (bool) e.NewValue;
            if (hideCloseButton && !GetIsHiddenCloseButton(window))
            {
                if (!window.IsLoaded)
                {
                    window.Loaded += HideWhenLoadedDelegate;
                }
                else
                {
                    HideCloseButton(window);
                }
                SetIsHiddenCloseButton(window, true);
            }
            else if (!hideCloseButton && GetIsHiddenCloseButton(window))
            {
                if (!window.IsLoaded)
                {
                    window.Loaded -= ShowWhenLoadedDelegate;
                }
                else
                {
                    ShowCloseButton(window);
                }
                SetIsHiddenCloseButton(window, false);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private static void HideCloseButton(Window w)
        {
            IntPtr hwnd = new WindowInteropHelper(w).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private static void ShowCloseButton(Window w)
        {
            IntPtr hwnd = new WindowInteropHelper(w).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) | WS_SYSMENU);
        }

        private static void SetIsHiddenCloseButton(Window obj, bool value)
        {
            obj.SetValue(IsHiddenCloseButtonKey, value);
        }

        #endregion

        #region private fields

        private static readonly RoutedEventHandler HideWhenLoadedDelegate = (sender, args) =>
                                                                            {
                                                                                if (sender is Window == false) return;
                                                                                var w = (Window) sender;
                                                                                HideCloseButton(w);
                                                                                w.Loaded -= HideWhenLoadedDelegate;
                                                                            };

        private static readonly RoutedEventHandler ShowWhenLoadedDelegate = (sender, args) =>
                                                                            {
                                                                                if (sender is Window == false) return;
                                                                                var w = (Window) sender;
                                                                                HideCloseButton(w);
                                                                                w.Loaded -= ShowWhenLoadedDelegate;
                                                                            };

        private static readonly DependencyPropertyKey IsHiddenCloseButtonKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsHiddenCloseButton",
                typeof (bool),
                typeof (WindowBehavior),
                new FrameworkPropertyMetadata(false));

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        #endregion
    }
}