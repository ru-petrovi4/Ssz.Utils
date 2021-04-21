using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Ssz.Utils.Wpf.LocationMindfulWindows
{
    internal class WindowSlot
    {
        #region public functions

        /// <summary>        
        /// </summary>
        /// <param name="window"></param>
        /// <param name="category"></param>
        /// <param name="initialWidth"></param>
        /// <param name="initialHeight"></param>
        public static void InitializeWindow(ILocationMindfulWindow window, string category, double initialWidth,
            double initialHeight)
        {
            window.Category = category;
            List<WindowSlot>? windowSlots;
            if (!WindowSlotsDictionary.TryGetValue(category, out windowSlots))
            {
                windowSlots = new List<WindowSlot>();
                WindowSlotsDictionary[category] = windowSlots;
            }

            WindowSlot? freeWindowSlot = windowSlots.FirstOrDefault(slot => !slot.Occupied);
            if (freeWindowSlot == null)
            {
                var rect = new Rect(Double.NaN, Double.NaN, Double.NaN, Double.NaN);
                if (window.Category != "")
                {
                    RegistryKey? registryKey = GetOrCreateSszRegistryKey();
                    if (registryKey != null)
                    {
                        string? rectString = registryKey.GetValue(window.Category) as string;
                        if (rectString != null)
                        {
                            var registryRect = (RegistryRect)NameValueCollectionValueSerializer<RegistryRect>.Instance.ConvertFromString(rectString);
                            rect = new Rect(registryRect.X, registryRect.Y, registryRect.Width, registryRect.Height);
                        }
                    }
                }

                freeWindowSlot = new WindowSlot
                {
                    Num = windowSlots.Count,
                    Location = rect
                };
                windowSlots.Add(freeWindowSlot);
            }
            freeWindowSlot.Occupied = true;
            window.SlotNum = freeWindowSlot.Num;

            Rect slotLocation = freeWindowSlot.Location;
            if (Double.IsNaN(slotLocation.Width) && !Double.IsNaN(initialWidth))
            {
                slotLocation.Width = initialWidth;
            }
            if (Double.IsNaN(slotLocation.Height) && !Double.IsNaN(initialHeight))
            {
                slotLocation.Height = initialHeight;
            }

            if (Double.IsNaN(window.Left) && !Double.IsNaN(slotLocation.X))
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = slotLocation.X;
            }
            if (Double.IsNaN(window.Top) && !Double.IsNaN(slotLocation.Y))
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Top = slotLocation.Y;
            }
            if (Double.IsNaN(window.Width) && !Double.IsNaN(slotLocation.Width))
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Width = slotLocation.Width;
            }
            if (Double.IsNaN(window.Height) && !Double.IsNaN(slotLocation.Height))
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Height = slotLocation.Height;
            }
        }

        public static void WindowOnLoaded(ILocationMindfulWindow window)
        {  
            Rect screen = ScreenHelper.GetNearestSystemScreen(new Point(window.Left + window.Width / 2, window.Top + window.Height / 2));

            if (screen != Rect.Empty)
            {
                if (window.Width > screen.Width)
                {
                    window.Width = screen.Width;
                }
                if (window.Height > screen.Height)
                {
                    window.Height = screen.Height;
                }

                if ((int)window.Left < (int)screen.X)
                {
                    window.Left = screen.X;
                }
                if ((int)window.Top < (int)screen.Y)
                {
                    window.Top = screen.Y;
                }
                if ((int)(window.Left + window.Width) > (int)(screen.X + screen.Width))
                {
                    window.Left = screen.X + screen.Width - window.Width;
                }
                if ((int)(window.Top + window.Height) > (int)(screen.Y + screen.Height))
                {
                    window.Top = screen.Y + screen.Height - window.Height;
                }
            }
        }

        public static void WindowOnClosed(ILocationMindfulWindow window)
        {
            List<WindowSlot> windowSlots = WindowSlotsDictionary[window.Category];

            var rect = new Rect(window.Left, window.Top, window.Width, window.Height);
            windowSlots[window.SlotNum].Occupied = false;
            windowSlots[window.SlotNum].Location = rect;

            if (window.Category != "")
            {
                RegistryKey? registryKey = GetOrCreateSszRegistryKey();
                if (registryKey != null)
                {
                    string rectString = NameValueCollectionValueSerializer<RegistryRect>.Instance.ConvertToString(
                        new RegistryRect { X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height }
                        );
                    registryKey.SetValue(window.Category, rectString);
                }
            }
        }

        public int Num { get; set; }
        public bool Occupied { get; set; }
        public Rect Location { get; set; }

        #endregion

        #region private functions

        private const string SszSubKeyString = @"SOFTWARE\Ssz\LocationMindfulWindows";

        private static RegistryKey? GetOrCreateSszRegistryKey()
        {            
            try
            {
                return Registry.CurrentUser.CreateSubKey(SszSubKeyString);
            }
            catch (Exception)
            {
            }
            return null;
        }

        #endregion

        #region private fields

        private static readonly CaseInsensitiveDictionary<List<WindowSlot>> WindowSlotsDictionary =
            new CaseInsensitiveDictionary<List<WindowSlot>>();

        #endregion

        private class RegistryRect
        {
            public double Width { get; set; }

            public double Height { get; set; }

            public double X { get; set; }

            public double Y { get; set; }
        }
    }
}