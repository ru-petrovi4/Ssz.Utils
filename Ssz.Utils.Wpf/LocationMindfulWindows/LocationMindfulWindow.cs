using System;
using System.Windows;

namespace Ssz.Utils.Wpf.LocationMindfulWindows
{
    public class LocationMindfulWindow : Window, ILocationMindfulWindow
    {
        #region construction and destruction

        /// <summary>
        ///     For using in VS editor
        /// </summary>
        public LocationMindfulWindow()
        {
        }

        public LocationMindfulWindow(string category, double initialWidth = Double.NaN,
            double initialHeight = Double.NaN)
        {
            WindowSlot.InitializeWindow(this, category, initialWidth, initialHeight);

            Loaded += (sender, args) => WindowSlot.WindowOnLoaded(this);
            Closed += (sender, args) => WindowSlot.WindowOnClosed(this);
        }

        #endregion

        #region public functions

        public string Category { get; set; } = "";
        public int SlotNum { get; set; }

        #endregion
    }
}