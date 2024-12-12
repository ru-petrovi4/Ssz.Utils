using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Research.DynamicDataDisplay;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.WinApi;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay
{
    public class TouchScreenWindowBase : Window
    {
        #region construction and destruction

        public TouchScreenWindowBase()
        {
            Stylus.SetIsPressAndHoldEnabled(this, false);
            Stylus.SetIsTapFeedbackEnabled(this, false);
            Stylus.SetIsTouchFeedbackEnabled(this, false);
            Stylus.SetIsFlicksEnabled(this, false);

            if (PlayDsProjectView.TouchScreenMode == TouchScreenMode.MultiTouch &&
                _deltaSimOperatorEnhanceTouchscreenProcess is null)
            {
                var fi = new FileInfo(Process.GetCurrentProcess().MainModule?.FileName ?? "");
                var startInfo = new ProcessStartInfo(fi.FullName, "-e 1");
                try
                {
                    _deltaSimOperatorEnhanceTouchscreenProcess = Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"Failed to start Enhance Touchscreen Process");
                }
            }

            _touchScreenWindowsCount += 1;

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        #endregion

        #region internal functions

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            //Set the window style to noactivate.
            WindowInteropHelper helper = new(this);
            SetWindowLong(helper.Handle, GWL_EXSTYLE,
                GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (PlayDsProjectView.TouchScreenMode == TouchScreenMode.SingleTouch)
            {
                var w = PlayDsProjectView.LastActiveRootPlayWindow as Window;
                if (w is not null)
                {
                    var c = PointToScreen(new Rect(w.Left, w.Top, w.ActualWidth, w.ActualHeight).GetCenter());
                    SetCursorPos((int)c.X, (int)c.Y);
                }
            }
        }

        #endregion

        #region internal functions

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private void OnLoaded(object? sender, RoutedEventArgs routedEventArgs)
        {
            var thisWindowHandle = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(thisWindowHandle);
            if (_hwndSource is not null) _hwndSource.AddHook(WndProc);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_hwndSource is not null) _hwndSource.RemoveHook(WndProc);

            _touchScreenWindowsCount--;

            if (_touchScreenWindowsCount == 0 && _deltaSimOperatorEnhanceTouchscreenProcess is not null &&
                !_deltaSimOperatorEnhanceTouchscreenProcess.HasExited)
            {
                try
                {
                    ProcessHelper.CloseAllWindows(_deltaSimOperatorEnhanceTouchscreenProcess);
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"Failed to stop Enhance Touchscreen Process");
                }

                _deltaSimOperatorEnhanceTouchscreenProcess = null;
            }
        }


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_MOUSEACTIVATE:
                    handled = true;
                    return new IntPtr(MA_NOACTIVATE);
            }

            return IntPtr.Zero;
        }

        #endregion

        #region private fields

        private static uint _touchScreenWindowsCount;
        private static Process? _deltaSimOperatorEnhanceTouchscreenProcess;

        private HwndSource? _hwndSource;

        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        #endregion
    }
}