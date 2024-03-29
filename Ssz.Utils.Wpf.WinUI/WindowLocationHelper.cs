﻿//using Microsoft.Win32;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.UI.Xaml;

//namespace Ssz.Utils.Wpf
//{
//    public static class WindowLocationHelper
//    {
//        #region public functions

//        public static void InitializeWindow(Window window, string category, bool rememberSize, double initialWidth = Double.NaN,
//            double initialHeight = Double.NaN)
//        {
//            List<WindowSlot>? windowSlots;
//            if (!WindowSlotsDictionary.TryGetValue(category, out windowSlots))
//            {
//                windowSlots = new List<WindowSlot>();
//                WindowSlotsDictionary[category] = windowSlots;
//            }

//            WindowSlot? freeWindowSlot = windowSlots.FirstOrDefault(slot => slot.Window is null);
//            if (freeWindowSlot is null)
//            {
//                var rect = new Rect(Double.NaN, Double.NaN, Double.NaN, Double.NaN);
//                if (category != "")
//                {
//                    RegistryKey? registryKey = GetOrCreateSszRegistryKey();
//                    if (registryKey is not null)
//                    {
//                        string? rectString = registryKey.GetValue(category) as string;
//                        if (rectString is not null)
//                        {
//                            var registryRect = (RegistryRect)NameValueCollectionValueSerializer<RegistryRect>.Instance.ConvertFromString(rectString);
//                            rect = new Rect(registryRect.X, registryRect.Y, registryRect.Width, registryRect.Height);
//                        }
//                    }
//                }

//                freeWindowSlot = new WindowSlot
//                {
//                    Num = windowSlots.Count,
//                    Location = rect
//                };
//                windowSlots.Add(freeWindowSlot);
//            }
//            freeWindowSlot.Window = window;
//            var windowInfo = new WindowInfo(category, freeWindowSlot.Num);
//            WindowInfosDictionary[window] = windowInfo;

//            Rect slotLocation = freeWindowSlot.Location;
//            if (Double.IsNaN(slotLocation.Width) && !Double.IsNaN(initialWidth))
//            {
//                slotLocation.Width = initialWidth;
//            }
//            if (Double.IsNaN(slotLocation.Height) && !Double.IsNaN(initialHeight))
//            {
//                slotLocation.Height = initialHeight;
//            }

//            if (Double.IsNaN(window.Left) && !Double.IsNaN(slotLocation.X))
//            {
//                window.WindowStartupLocation = WindowStartupLocation.Manual;
//                window.Left = slotLocation.X;
//            }
//            if (Double.IsNaN(window.Top) && !Double.IsNaN(slotLocation.Y))
//            {
//                window.WindowStartupLocation = WindowStartupLocation.Manual;
//                window.Top = slotLocation.Y;
//            }

//            if (rememberSize)
//            {
//                if (Double.IsNaN(window.Width) && !Double.IsNaN(slotLocation.Width))
//                {
//                    window.WindowStartupLocation = WindowStartupLocation.Manual;
//                    window.Width = slotLocation.Width;
//                }
//                if (Double.IsNaN(window.Height) && !Double.IsNaN(slotLocation.Height))
//                {
//                    window.WindowStartupLocation = WindowStartupLocation.Manual;
//                    window.Height = slotLocation.Height;
//                }
//            }            

//            window.Loaded += (sender, args) => WindowOnLoaded(window);
//            window.Closed += (sender, args) => WindowOnClosed(window);
//        }

//        /// <summary>
//        ///     Returns existing window or null.
//        /// </summary>
//        /// <param name="category"></param>
//        /// <returns></returns>
//        public static Window? TryActivateExistingWindow(string category)
//        {
//            List<WindowSlot>? windowSlots;
//            if (!WindowSlotsDictionary.TryGetValue(category, out windowSlots))
//            {
//                return null;
//            }

//            WindowSlot? occupiedWindowSlot = windowSlots.FirstOrDefault(slot => slot.Window is not null);
//            if (occupiedWindowSlot is null)
//            {
//                return null;
//            }

//            occupiedWindowSlot.Window?.Activate();

//            return occupiedWindowSlot.Window;
//        }   

//        #endregion

//        #region private functions

//        private static RegistryKey? GetOrCreateSszRegistryKey()
//        {
//            try
//            {
//                return Registry.CurrentUser.CreateSubKey(SszSubKeyString);
//            }
//            catch (Exception)
//            {
//            }
//            return null;
//        }

//        private static void WindowOnLoaded(Window window)
//        {
//            Point pointInPixels = window.PointToScreen(new Point(window.Left + window.ActualWidth / 2, window.Top + window.ActualHeight / 2));
//            Rect screenInPixels = ScreenHelper.GetNearestSystemScreenWorkingAreaInPixels(pointInPixels);
//            if (screenInPixels == Rect.Empty)
//                return;
//            var t = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
//            var p1 = t.Transform(screenInPixels.TopLeft);            
//            var p2 = t.Transform(screenInPixels.BottomRight);            
//            var screen = new Rect(p1, p2);

//            if (window.Width > screen.Width)
//            {
//                window.Width = screen.Width;
//            }
//            if (window.Height > screen.Height)
//            {
//                window.Height = screen.Height;
//            }

//            if ((int)window.Left < (int)screen.X)
//            {
//                window.Left = screen.X;
//            }
//            if ((int)window.Top < (int)screen.Y)
//            {
//                window.Top = screen.Y;
//            }
//            if ((int)(window.Left + window.Width) > (int)(screen.X + screen.Width))
//            {
//                window.Left = screen.X + screen.Width - window.Width;
//            }
//            if ((int)(window.Top + window.Height) > (int)(screen.Y + screen.Height))
//            {
//                window.Top = screen.Y + screen.Height - window.Height;
//            }
//        }

//        private static void WindowOnClosed(Window window)
//        {
//            WindowInfosDictionary.TryGetValue(window, out WindowInfo? windowInfo);
//            if (windowInfo is null) return;
//            WindowInfosDictionary.Remove(window);

//            List<WindowSlot> windowSlots = WindowSlotsDictionary[windowInfo.Category];

//            var rect = new Rect(window.Left, window.Top, window.Width, window.Height);
//            var windowSlot = windowSlots[windowInfo.SlotNum];
//            windowSlot.Window = null;
//            windowSlot.Location = rect;

//            if (windowInfo.Category != "")
//            {
//                RegistryKey? registryKey = GetOrCreateSszRegistryKey();
//                if (registryKey is not null)
//                {
//                    string rectString = NameValueCollectionValueSerializer<RegistryRect>.Instance.ConvertToString(
//                        new RegistryRect { X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height }
//                        );
//                    registryKey.SetValue(windowInfo.Category, rectString);
//                }
//            }
//        }

//        #endregion

//        #region private fields

//        private const string SszSubKeyString = @"SOFTWARE\Ssz\LocationMindfulWindows";

//        private static readonly Dictionary<Window, WindowInfo> WindowInfosDictionary =
//            new Dictionary<Window, WindowInfo>(ReferenceEqualityComparer<Window>.Default);

//        private static readonly CaseInsensitiveDictionary<List<WindowSlot>> WindowSlotsDictionary =
//            new CaseInsensitiveDictionary<List<WindowSlot>>();

//        #endregion

//        private class RegistryRect
//        {
//            public double Width { get; set; }

//            public double Height { get; set; }

//            public double X { get; set; }

//            public double Y { get; set; }
//        }

//        private class WindowSlot
//        {
//            #region public functions

//            public int Num { get; set; }

//            public Window? Window { get; set; }

//            public Rect Location { get; set; }

//            #endregion 
//        }

//        private class WindowInfo
//        {
//            #region construction and destruction

//            public WindowInfo(string category, int slotNum)
//            {
//                Category = category;
//                SlotNum = slotNum;
//            }

//            #endregion            

//            #region public functions

//            public string Category { get; }

//            public int SlotNum { get; }

//            #endregion            
//        }
//    }
//}