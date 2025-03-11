using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Ssz.Operator.Core
{    
    public static class ScreenHelper
    {
        #region public functions        

        /// <summary>
        ///     First screen in array is primary screen.
        ///     Ordered by device name.
        /// </summary>
        /// <param name="topLevel"></param>
        /// <returns></returns>
        public static PixelRect[] GetSystemScreensWorkingAreas(TopLevel topLevel)
        {
            var result = new List<PixelRect>();

            try
            {
                Screen[]? screens = topLevel.Screens?.All.OrderByDescending(s => s.IsPrimary).ThenBy(s => s.DisplayName).ToArray();
                if (screens is null)
                    return Array.Empty<PixelRect>();
                foreach (Screen screen in screens)
                {
                    //Rectangle workingArea = screen.WorkingArea;
                    //Point p1 = new Point(workingArea.X / PrimaryScreenScaleX, workingArea.Y / PrimaryScreenScaleY);
                    //Point p2 = new Point(workingArea.Right / PrimaryScreenScaleX, workingArea.Bottom / PrimaryScreenScaleY);
                    //result.Add(new Rect(p1, p2));
                    result.Add(screen.WorkingArea);
                }
            }
            catch
            {
            }

            return result.ToArray();
        }

        /// <summary>
        ///     Returns system screen working area containing the point.        
        ///     Returns null, if not found.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="topLevel"></param>
        /// <returns></returns>
        public static PixelRect? GetSystemScreenWorkingArea(PixelPoint point, TopLevel topLevel)
        {
            foreach (PixelRect systemScreensWorkingArea in GetSystemScreensWorkingAreas(topLevel))
            {
                if (point.X >= systemScreensWorkingArea.X &&
                    point.Y >= systemScreensWorkingArea.Y &&
                    point.X <= systemScreensWorkingArea.Right &&
                    point.Y <= systemScreensWorkingArea.Bottom)
                    return systemScreensWorkingArea;
            }
            return null;
        }

        /// <summary>    
        ///     Returns system screen working area nearest to the point.
        ///     Returns null, if not found.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="topLevel"></param>
        /// <returns></returns>
        public static PixelRect? GetNearestSystemScreenWorkingArea(PixelPoint point, TopLevel topLevel)
        {
            double minDistance = Double.MaxValue;
            PixelRect? nearestSystemScreenWorkingArea = null;
            foreach (PixelRect systemScreensWorkingArea in GetSystemScreensWorkingAreas(topLevel))
            {
                var distance = GetDistance(point.X, point.Y, systemScreensWorkingArea.X + systemScreensWorkingArea.Width / 2, systemScreensWorkingArea.Y + systemScreensWorkingArea.Height / 2);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestSystemScreenWorkingArea = systemScreensWorkingArea;
                }
            }
            return nearestSystemScreenWorkingArea;
        }
        
        public static bool IsFullyVisible(PixelRect rect, TopLevel topLevel)
        {
            foreach (PixelRect systemScreensWorkingArea in GetSystemScreensWorkingAreas(topLevel))
            {
                if (rect.X >= systemScreensWorkingArea.X &&
                    rect.Y >= systemScreensWorkingArea.Y &&
                    rect.Right <= systemScreensWorkingArea.Right &&
                    rect.Bottom <= systemScreensWorkingArea.Bottom)
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Set window fully contained in rect.            
        /// </summary>
        /// <param name="window"></param>
        /// <param name="rect"></param>
        public static void SetFullyVisible(Window window, PixelRect rect)
        {            
            if (Double.IsNaN(window.Bounds.Width) || Double.IsNaN(window.Bounds.Height) ||
                    rect == default) 
                return;

            var windowPosition = window.Position;
            if ((int)windowPosition.X < (int)rect.X)
            {
                windowPosition = windowPosition.WithX(rect.X);
            }
            if ((int)windowPosition.Y < (int)rect.Y)
            {
                windowPosition = windowPosition.WithY(rect.Y);
            }
            if ((int)(windowPosition.X + window.Width) > (int)(rect.X + rect.Width))
            {
                windowPosition = windowPosition.WithX(rect.X + rect.Width - (int)window.Width);
            }
            if ((int)(windowPosition.Y + window.Height) > (int)(rect.Y + rect.Height))
            {
                windowPosition = windowPosition.WithY(rect.Y + rect.Height - (int)window.Height);
            }
            window.Position = windowPosition;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameworkElement"></param>
        /// <returns></returns>
        public static PixelRect? GetPixelRect(Control control)
        {
            PixelPoint p1 = control.PointToScreen(new Avalonia.Point(0, 0));
            PixelPoint p2 = control.PointToScreen(new Avalonia.Point(control.Bounds.Width, control.Bounds.Height));
            return new PixelRect(p1, p2);
            //return new Rect(p1.X / PrimaryScreenScaleX, p1.Y / PrimaryScreenScaleY, (p2.X - p1.X) / PrimaryScreenScaleX, (p2.Y - p1.Y) / PrimaryScreenScaleY);
        }

        //public static Rect GetRect(Window window, Rect rectInPixels)
        //{            
        //    var t = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
        //    var p1 = t.Transform(rectInPixels.TopLeft);
        //    var p2 = t.Transform(rectInPixels.BottomRight);
        //    return new Rect(p1, p2);
        //}        

        ///// <summary>
        /////     Pixels coordinate/WPF coordinate ratio on X-axis.
        ///// </summary>
        //public static double PrimaryScreenScaleX
        //{
        //    get
        //    {
        //        if (_primaryScreenScaleX is null) 
        //            DefinePrimaryScreenScale();                
        //        return _primaryScreenScaleX!.Value;
        //    }
        //}

        ///// <summary>
        /////     Pixels coordinate/WPF coordinate ratio on Y-axis.
        ///// </summary>
        //public static double PrimaryScreenScaleY
        //{
        //    get
        //    {
        //        if (_primaryScreenScaleY is null)
        //            DefinePrimaryScreenScale();
        //        return _primaryScreenScaleY!.Value;

        //    }
        //}
        
        /// <summary>
        ///     Returns true if valid.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool IsValidLength(double? length)
        {
            if (!length.HasValue) return false;
            double l = length.Value;
            if (Double.IsNaN(l) || Double.IsInfinity(l)) return false;
            return l > 0.0;
        }

        #endregion

        #region private functions        

        private static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        #endregion        
    }
}