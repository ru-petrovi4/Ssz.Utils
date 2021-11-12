using Ssz.Utils.Wpf.WpfScreenHelper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace Ssz.Utils.Wpf
{
    /// <summary>
    ///     All functions work with WPF coordinates.
    ///     Warning! frameworkElement.PointToScreen returns values in screen coordinates, not WPF coordinates.
    /// </summary>
    public static class ScreenHelper
    {
        #region public functions

        /// <summary>
        ///     Returns screens working areas in WPF coordinates.
        ///     First screen in array is primary screen.
        ///     result is not null
        /// </summary>
        /// <returns></returns>
        public static Rect[] GetSystemScreens()
        {
            var result = new List<Rect>();

            Screen[] screens = Screen.AllScreens.OrderByDescending(s => s.Primary).ThenBy(s => s.DeviceName).ToArray();

            foreach (Screen screen in screens)
            {
                Rectangle workingArea = screen.WorkingArea;
                result.Add(new Rect(workingArea.Left / ScreenScaleX, workingArea.Top / ScreenScaleY,
                    workingArea.Width / ScreenScaleX, workingArea.Height / ScreenScaleY));
            }

            return result.ToArray();
        }

        /// <summary>
        ///     Returns primary screen working area in WPF coordinates.        
        /// </summary>
        /// <returns></returns>
        public static Rect GetPrimarySystemScreen()
        {
            Rectangle workingArea = Screen.AllScreens.First(s => s.Primary).WorkingArea;

            return new Rect(workingArea.Left / ScreenScaleX, workingArea.Top / ScreenScaleY,
                    workingArea.Width / ScreenScaleX, workingArea.Height / ScreenScaleY);
        }

        /// <summary>
        ///     Returns system screen containing the point.
        ///     All values in WPF coordinates.
        ///     Returns null, if not found.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rect? GetSystemScreen(Point point)
        {
            foreach (Rect screen in GetSystemScreens())
            {
                if (point.X >= screen.Left &&
                    point.Y >= screen.Top &&
                    point.X <= screen.Right &&
                    point.Y <= screen.Bottom)
                    return screen;
            }
            return null;
        }

        /// <summary>
        ///     All values in WPF coordinates.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static bool IsFullyVisible(Rect rect)
        {
            foreach (Rect screen in GetSystemScreens())
            {
                if (rect.Left >= screen.Left &&
                    rect.Top >= screen.Top &&
                    rect.Right <= screen.Right &&
                    rect.Bottom <= screen.Bottom)
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Set window fully contained in rect.
        ///     All values in WPF coordinates.
        ///     window is not null
        /// </summary>
        /// <param name="window"></param>
        /// <param name="rect"></param>
        public static void SetFullyVisible(Window window, Rect rect)
        {
            if (window is null) throw new ArgumentNullException("window");
            if (Double.IsNaN(window.Left) || Double.IsNaN(window.Top) ||
                Double.IsNaN(window.ActualWidth) || Double.IsNaN(window.ActualHeight) ||
                rect == Rect.Empty) return;

            if ((int)window.Left < (int)rect.X)
            {
                window.Left = rect.X;
            }
            if ((int)window.Top < (int)rect.Y)
            {
                window.Top = rect.Y;
            }
            if ((int)(window.Left + window.ActualWidth) > (int)(rect.X + rect.Width))
            {
                window.Left = rect.X + rect.Width - window.ActualWidth;
            }
            if ((int)(window.Top + window.ActualHeight) > (int)(rect.Y + rect.Height))
            {
                window.Top = rect.Y + rect.Height - window.ActualHeight;
            }
        }

        /// <summary>
        ///     All values in WPF coordinates.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rect GetNearestSystemScreen(Point point)
        {
            double minDistance = Double.MaxValue;
            Rect nearestSystemScreen = Rect.Empty;
            foreach (Rect screen in GetSystemScreens())
            {
                var distance = GetDistance(point.X, point.Y, screen.Left + screen.Width / 2, screen.Top + screen.Height / 2);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestSystemScreen = screen;
                }
            }
            return nearestSystemScreen;
        }

        /// <summary>
        ///     Gets location of frameworkElement on system screen.
        ///     All values in WPF coordinates.
        ///     Warning! frameworkElement.PointToScreen returns values in screen coordinates, not WPF coordinates.
        ///     frameworkElement is not null
        /// </summary>
        /// <param name="frameworkElement"></param>
        /// <returns></returns>
        public static Rect GetRect(FrameworkElement frameworkElement)
        {
            if (frameworkElement is null) throw new ArgumentNullException("frameworkElement");

            var p1 = frameworkElement.PointToScreen(new Point(0, 0));
            var p2 = frameworkElement.PointToScreen(new Point(frameworkElement.ActualWidth, frameworkElement.ActualHeight));
            return new Rect(new Point(p1.X / ScreenScaleX, p1.Y / ScreenScaleY),
                new Point(p2.X / ScreenScaleX, p2.Y / ScreenScaleY));
        }

        /// <summary>
        ///     Screen coordinate/WPF coordinate ratio on X-axis.
        /// </summary>
        public static double ScreenScaleX
        {
            get
            {
                if (_screenScaleX is null) DefineScreenScale();
                if (_screenScaleX is null) return 1;
                var value = _screenScaleX.Value;
                if (Double.IsNaN(value) || Double.IsInfinity(value)) return 1;
                return value;
            }
        }

        /// <summary>
        ///     Screen coordinate/WPF coordinate ratio on Y-axis.
        /// </summary>
        public static double ScreenScaleY
        {
            get
            {
                if (_screenScaleY is null) DefineScreenScale();
                if (_screenScaleY is null) return 1;
                var value = _screenScaleY.Value;
                if (Double.IsNaN(value) || Double.IsInfinity(value)) return 1;
                return value;

            }
        }

        /// <summary>
        ///     Returns true if valid.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool IsValidCoordinate(double? length)
        {
            if (!length.HasValue) return false;
            double l = length.Value;
            if (Double.IsNaN(l) || Double.IsInfinity(l)) return false;
            return true;
        }

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

        private static void DefineScreenScale()
        {
            Screen primaryScreen = Screen.AllScreens.Where(s => s.Primary).First();
            _screenScaleX = primaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
            _screenScaleY = primaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight;
        }

        private static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        #endregion

        #region private fields

        private static double? _screenScaleX;
        private static double? _screenScaleY;

        #endregion
    }
}