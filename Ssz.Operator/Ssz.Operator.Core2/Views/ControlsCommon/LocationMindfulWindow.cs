//using Ssz.Utils.Wpf;
using System;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;

namespace Ssz.Operator.Core.ControlsCommon
{    
    public class LocationMindfulWindow : Window
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
            //WindowLocationHelper.InitializeWindow(this, category, true, initialWidth, initialHeight);
        }

        #endregion
    }
}