/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Standard;

namespace Microsoft.Windows.Shell
{
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class SystemParameters2 : INotifyPropertyChanged
    {
        [ThreadStatic] private static SystemParameters2 _threadLocalSingleton;

        private readonly Dictionary<WM, List<_SystemMetricUpdate>> _UpdateTable;
        private Rect _captionButtonLocation;
        private double _captionHeight;
        private Color _glassColor;
        private SolidColorBrush _glassColorBrush;

        private bool _isGlassEnabled;
        private bool _isHighContrast;

        private MessageWindow _messageHwnd;
        private Size _smallIconSize;
        private string _uxThemeColor;
        private string _uxThemeName;
        private CornerRadius _windowCornerRadius;
        private Thickness _windowNonClientFrameThickness;
        private Thickness _windowResizeBorderThickness;

        /// <summary>
        ///     Private constructor.  The public way to access this class is through the static Current property.
        /// </summary>
        private SystemParameters2()
        {
            // This window gets used for calculations about standard caption button locations
            // so it has WS_OVERLAPPEDWINDOW as a style to give it normal caption buttons.
            // This window may be shown during calculations of caption bar information, so create it at a location that's likely offscreen.
            _messageHwnd = new MessageWindow(0, WS.OVERLAPPEDWINDOW | WS.DISABLED, 0,
                new Rect(-16000, -16000, 100, 100), "", _WndProc);
            _messageHwnd.Dispatcher.ShutdownStarted += (sender, e) => Utility.SafeDispose(ref _messageHwnd);

            // Fixup the default values of the DPs.
            _InitializeIsGlassEnabled();
            _InitializeGlassColor();
            _InitializeCaptionHeight();
            _InitializeWindowNonClientFrameThickness();
            _InitializeWindowResizeBorderThickness();
            _InitializeCaptionButtonLocation();
            _InitializeSmallIconSize();
            _InitializeHighContrast();
            _InitializeThemeInfo();
            // WindowCornerRadius isn't exposed by true system parameters, so it requires the theme to be initialized first.
            _InitializeWindowCornerRadius();

            _UpdateTable = new Dictionary<WM, List<_SystemMetricUpdate>>
            {
                {
                    WM.THEMECHANGED,
                    new List<_SystemMetricUpdate>
                    {
                        _UpdateThemeInfo,
                        _UpdateHighContrast,
                        _UpdateWindowCornerRadius,
                        _UpdateCaptionButtonLocation
                    }
                },
                {
                    WM.SETTINGCHANGE,
                    new List<_SystemMetricUpdate>
                    {
                        _UpdateCaptionHeight,
                        _UpdateWindowResizeBorderThickness,
                        _UpdateSmallIconSize,
                        _UpdateHighContrast,
                        _UpdateWindowNonClientFrameThickness,
                        _UpdateCaptionButtonLocation
                    }
                },
                {WM.DWMNCRENDERINGCHANGED, new List<_SystemMetricUpdate> {_UpdateIsGlassEnabled}},
                {WM.DWMCOMPOSITIONCHANGED, new List<_SystemMetricUpdate> {_UpdateIsGlassEnabled}},
                {WM.DWMCOLORIZATIONCOLORCHANGED, new List<_SystemMetricUpdate> {_UpdateGlassColor}}
            };
        }

        public static SystemParameters2 Current
        {
            get
            {
                if (_threadLocalSingleton is null) _threadLocalSingleton = new SystemParameters2();
                return _threadLocalSingleton;
            }
        }

        public bool IsGlassEnabled
        {
            get =>
                // return _isGlassEnabled;
                // It turns out there may be some lag between someone asking this
                // and the window getting updated.  It's not too expensive, just always do the check.
                NativeMethods.DwmIsCompositionEnabled();
            private set
            {
                if (value != _isGlassEnabled)
                {
                    _isGlassEnabled = value;
                    _NotifyPropertyChanged("IsGlassEnabled");
                }
            }
        }

        public Color WindowGlassColor
        {
            get => _glassColor;
            private set
            {
                if (value != _glassColor)
                {
                    _glassColor = value;
                    _NotifyPropertyChanged("WindowGlassColor");
                }
            }
        }

        public SolidColorBrush WindowGlassBrush
        {
            get => _glassColorBrush;
            private set
            {
                Assert.IsNotNull(value);
                Assert.IsTrue(value.IsFrozen);
                if (_glassColorBrush is null || value.Color != _glassColorBrush.Color)
                {
                    _glassColorBrush = value;
                    _NotifyPropertyChanged("WindowGlassBrush");
                }
            }
        }

        public Thickness WindowResizeBorderThickness
        {
            get => _windowResizeBorderThickness;
            private set
            {
                if (value != _windowResizeBorderThickness)
                {
                    _windowResizeBorderThickness = value;
                    _NotifyPropertyChanged("WindowResizeBorderThickness");
                }
            }
        }

        public Thickness WindowNonClientFrameThickness
        {
            get => _windowNonClientFrameThickness;
            private set
            {
                if (value != _windowNonClientFrameThickness)
                {
                    _windowNonClientFrameThickness = value;
                    _NotifyPropertyChanged("WindowNonClientFrameThickness");
                }
            }
        }

        public double WindowCaptionHeight
        {
            get => _captionHeight;
            private set
            {
                if (value != _captionHeight)
                {
                    _captionHeight = value;
                    _NotifyPropertyChanged("WindowCaptionHeight");
                }
            }
        }

        public Size SmallIconSize
        {
            get => new(_smallIconSize.Width, _smallIconSize.Height);
            private set
            {
                if (value != _smallIconSize)
                {
                    _smallIconSize = value;
                    _NotifyPropertyChanged("SmallIconSize");
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ux")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ux")]
        public string UxThemeName
        {
            get => _uxThemeName;
            private set
            {
                if (value != _uxThemeName)
                {
                    _uxThemeName = value;
                    _NotifyPropertyChanged("UxThemeName");
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ux")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ux")]
        public string UxThemeColor
        {
            get => _uxThemeColor;
            private set
            {
                if (value != _uxThemeColor)
                {
                    _uxThemeColor = value;
                    _NotifyPropertyChanged("UxThemeColor");
                }
            }
        }

        public bool HighContrast
        {
            get => _isHighContrast;
            private set
            {
                if (value != _isHighContrast)
                {
                    _isHighContrast = value;
                    _NotifyPropertyChanged("HighContrast");
                }
            }
        }

        public CornerRadius WindowCornerRadius
        {
            get => _windowCornerRadius;
            private set
            {
                if (value != _windowCornerRadius)
                {
                    _windowCornerRadius = value;
                    _NotifyPropertyChanged("WindowCornerRadius");
                }
            }
        }

        public Rect WindowCaptionButtonsLocation
        {
            get => _captionButtonLocation;
            private set
            {
                if (value != _captionButtonLocation)
                {
                    _captionButtonLocation = value;
                    _NotifyPropertyChanged("WindowCaptionButtonsLocation");
                }
            }
        }

        private IntPtr _WndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            // Don't do this if called within the SystemParameters2 constructor
            if (_UpdateTable is not null)
            {
                List<_SystemMetricUpdate> handlers;
                if (_UpdateTable.TryGetValue(msg, out handlers))
                {
                    Assert.IsNotNull(handlers);
                    foreach (var handler in handlers) handler(wParam, lParam);
                }
            }

            return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        private delegate void _SystemMetricUpdate(IntPtr wParam, IntPtr lParam);

        #region Initialization and Update Methods

        // Most properties exposed here have a way of being queried directly
        // and a way of being notified of updates via a window message.
        // This region is a grouping of both, for each of the exposed properties.

        private void _InitializeIsGlassEnabled()
        {
            IsGlassEnabled = NativeMethods.DwmIsCompositionEnabled();
        }

        private void _UpdateIsGlassEnabled(IntPtr wParam, IntPtr lParam)
        {
            // Neither the wParam or lParam are used in this case.
            _InitializeIsGlassEnabled();
        }

        private void _InitializeGlassColor()
        {
            bool isOpaque;
            uint color;
            NativeMethods.DwmGetColorizationColor(out color, out isOpaque);
            color |= isOpaque ? 0xFF000000 : 0;

            WindowGlassColor = Utility.ColorFromArgbDword(color);

            var glassBrush = new SolidColorBrush(WindowGlassColor);
            glassBrush.Freeze();

            WindowGlassBrush = glassBrush;
        }

        private void _UpdateGlassColor(IntPtr wParam, IntPtr lParam)
        {
            var isOpaque = lParam != IntPtr.Zero;
            var color = unchecked((uint) (int) wParam.ToInt64());
            color |= isOpaque ? 0xFF000000 : 0;
            WindowGlassColor = Utility.ColorFromArgbDword(color);
            var glassBrush = new SolidColorBrush(WindowGlassColor);
            glassBrush.Freeze();
            WindowGlassBrush = glassBrush;
        }

        private void _InitializeCaptionHeight()
        {
            var ptCaption = new Point(0, NativeMethods.GetSystemMetrics(SM.CYCAPTION));
            WindowCaptionHeight = DpiHelper.DevicePixelsToLogical(ptCaption).Y;
        }

        private void _UpdateCaptionHeight(IntPtr wParam, IntPtr lParam)
        {
            _InitializeCaptionHeight();
        }

        private void _InitializeWindowResizeBorderThickness()
        {
            var frameSize = new Size(
                NativeMethods.GetSystemMetrics(SM.CXSIZEFRAME),
                NativeMethods.GetSystemMetrics(SM.CYSIZEFRAME));
            var frameSizeInDips = DpiHelper.DeviceSizeToLogical(frameSize);
            WindowResizeBorderThickness = new Thickness(frameSizeInDips.Width, frameSizeInDips.Height,
                frameSizeInDips.Width, frameSizeInDips.Height);
        }

        private void _UpdateWindowResizeBorderThickness(IntPtr wParam, IntPtr lParam)
        {
            _InitializeWindowResizeBorderThickness();
        }

        private void _InitializeWindowNonClientFrameThickness()
        {
            var frameSize = new Size(
                NativeMethods.GetSystemMetrics(SM.CXSIZEFRAME),
                NativeMethods.GetSystemMetrics(SM.CYSIZEFRAME));
            var frameSizeInDips = DpiHelper.DeviceSizeToLogical(frameSize);
            var captionHeight = NativeMethods.GetSystemMetrics(SM.CYCAPTION);
            var captionHeightInDips = DpiHelper.DevicePixelsToLogical(new Point(0, captionHeight)).Y;
            WindowNonClientFrameThickness = new Thickness(frameSizeInDips.Width,
                frameSizeInDips.Height + captionHeightInDips, frameSizeInDips.Width, frameSizeInDips.Height);
        }

        private void _UpdateWindowNonClientFrameThickness(IntPtr wParam, IntPtr lParam)
        {
            _InitializeWindowNonClientFrameThickness();
        }

        private void _InitializeSmallIconSize()
        {
            SmallIconSize = new Size(
                NativeMethods.GetSystemMetrics(SM.CXSMICON),
                NativeMethods.GetSystemMetrics(SM.CYSMICON));
        }

        private void _UpdateSmallIconSize(IntPtr wParam, IntPtr lParam)
        {
            _InitializeSmallIconSize();
        }

        private void _LegacyInitializeCaptionButtonLocation()
        {
            // This calculation isn't quite right, but it's pretty close.
            // I expect this is good enough for the scenarios where this is expected to be used.
            var captionX = NativeMethods.GetSystemMetrics(SM.CXSIZE);
            var captionY = NativeMethods.GetSystemMetrics(SM.CYSIZE);

            var frameX = NativeMethods.GetSystemMetrics(SM.CXSIZEFRAME) + NativeMethods.GetSystemMetrics(SM.CXEDGE);
            var frameY = NativeMethods.GetSystemMetrics(SM.CYSIZEFRAME) + NativeMethods.GetSystemMetrics(SM.CYEDGE);

            var captionRect = new Rect(0, 0, captionX * 3, captionY);
            captionRect.Offset(-frameX - captionRect.Width, frameY);

            WindowCaptionButtonsLocation = captionRect;
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private void _InitializeCaptionButtonLocation()
        {
            // There is a completely different way to do this on XP.
            if (!Utility.IsOSVistaOrNewer || !NativeMethods.IsThemeActive())
            {
                _LegacyInitializeCaptionButtonLocation();
                return;
            }

            var tbix = new TITLEBARINFOEX {cbSize = Marshal.SizeOf(typeof(TITLEBARINFOEX))};
            var lParam = Marshal.AllocHGlobal(tbix.cbSize);
            try
            {
                Marshal.StructureToPtr(tbix, lParam, false);
                // This might flash a window in the taskbar while being calculated.
                // WM_GETTITLEBARINFOEX doesn't work correctly unless the window is visible while processing.
                NativeMethods.ShowWindow(_messageHwnd.Handle, SW.SHOW);
                NativeMethods.SendMessage(_messageHwnd.Handle, WM.GETTITLEBARINFOEX, IntPtr.Zero, lParam);
                tbix = (TITLEBARINFOEX) Marshal.PtrToStructure(lParam, typeof(TITLEBARINFOEX));
            }
            finally
            {
                NativeMethods.ShowWindow(_messageHwnd.Handle, SW.HIDE);
                Utility.SafeFreeHGlobal(ref lParam);
            }

            // TITLEBARINFOEX has information relative to the screen.  We need to convert the containing rect
            // to instead be relative to the top-right corner of the window.
            var rcAllCaptionButtons = RECT.Union(tbix.rgrect_CloseButton, tbix.rgrect_MinimizeButton);
            // For all known themes, the RECT for the maximize box shouldn't add anything to the union of the minimize and close boxes.
            Assert.AreEqual(rcAllCaptionButtons, RECT.Union(rcAllCaptionButtons, tbix.rgrect_MaximizeButton));

            var rcWindow = NativeMethods.GetWindowRect(_messageHwnd.Handle);

            // Reorient the Top/Right to be relative to the top right edge of the Window.
            var deviceCaptionLocation = new Rect(
                rcAllCaptionButtons.Left - rcWindow.Width - rcWindow.Left,
                rcAllCaptionButtons.Top - rcWindow.Top,
                rcAllCaptionButtons.Width,
                rcAllCaptionButtons.Height);

            var logicalCaptionLocation = DpiHelper.DeviceRectToLogical(deviceCaptionLocation);

            WindowCaptionButtonsLocation = logicalCaptionLocation;
        }

        private void _UpdateCaptionButtonLocation(IntPtr wParam, IntPtr lParam)
        {
            _InitializeCaptionButtonLocation();
        }

        private void _InitializeHighContrast()
        {
            var hc = NativeMethods.SystemParameterInfo_GetHIGHCONTRAST();
            HighContrast = (hc.dwFlags & HCF.HIGHCONTRASTON) != 0;
        }

        private void _UpdateHighContrast(IntPtr wParam, IntPtr lParam)
        {
            _InitializeHighContrast();
        }

        private void _InitializeThemeInfo()
        {
            if (!NativeMethods.IsThemeActive())
            {
                UxThemeName = "Classic";
                UxThemeColor = "";
                return;
            }

            string name;
            string color;
            string size;
            NativeMethods.GetCurrentThemeName(out name, out color, out size);

            // Consider whether this is the most useful way to expose this...
            UxThemeName = Path.GetFileNameWithoutExtension(name);
            UxThemeColor = color;
        }

        private void _UpdateThemeInfo(IntPtr wParam, IntPtr lParam)
        {
            _InitializeThemeInfo();
        }

        private void _InitializeWindowCornerRadius()
        {
            // The radius of window corners isn't exposed as a true system parameter.
            // It instead is a logical size that we're approximating based on the current theme.
            // There aren't any known variations based on theme color.
            Assert.IsNeitherNullNorEmpty(UxThemeName);

            // These radii are approximate.  The way WPF does rounding is different than how
            //     rounded-rectangle HRGNs are created, which is also different than the actual
            //     round corners on themed Windows.  For now we're not exposing anything to
            //     mitigate the differences.
            var cornerRadius = default(CornerRadius);

            // This list is known to be incomplete and very much not future-proof.
            // On XP there are at least a couple of shipped themes that this won't catch,
            // "Zune" and "Royale", but WPF doesn't know about these either.
            // If a new theme was to replace Aero, then this will fall back on "classic" behaviors.
            // This isn't ideal, but it's not the end of the world.  WPF will generally have problems anyways.
            switch (UxThemeName.ToUpperInvariant())
            {
                case "LUNA":
                    cornerRadius = new CornerRadius(6, 6, 0, 0);
                    break;
                case "AERO":
                    // Aero has two cases.  One with glass and one without...
                    if (NativeMethods.DwmIsCompositionEnabled())
                        cornerRadius = new CornerRadius(8);
                    else
                        cornerRadius = new CornerRadius(6, 6, 0, 0);
                    break;
                case "CLASSIC":
                case "ZUNE":
                case "ROYALE":
                default:
                    cornerRadius = new CornerRadius(0);
                    break;
            }

            WindowCornerRadius = cornerRadius;
        }

        private void _UpdateWindowCornerRadius(IntPtr wParam, IntPtr lParam)
        {
            // Neither the wParam or lParam are used in this case.
            _InitializeWindowCornerRadius();
        }

        #endregion

        #region INotifyPropertyChanged Members

        private void _NotifyPropertyChanged(string propertyName)
        {
            Assert.IsNeitherNullNorEmpty(propertyName);
            var handler = PropertyChanged;
            if (handler is not null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}