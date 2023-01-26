using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Ssz.Utils.Wpf
{    
    public static class ScreenHelper
    {
        #region public functions        

        /// <summary>        
        ///     First screen in array is primary screen.
        ///     Ordered by device name.
        /// </summary>
        /// <returns></returns>
        public static Rect[] GetSystemScreensWorkingAreas()
        {
            var result = new List<Rect>();

            Screen[] screens = Screen.AllScreens.OrderByDescending(s => s.Primary).ThenBy(s => s.DeviceName).ToArray();

            foreach (Screen screen in screens)
            {
                Rectangle workingArea = screen.WorkingArea;
                Point p1 = new Point(workingArea.X / PrimaryScreenScaleX, workingArea.Y / PrimaryScreenScaleY);
                Point p2 = new Point(workingArea.Right / PrimaryScreenScaleX, workingArea.Bottom / PrimaryScreenScaleY);
                result.Add(new Rect(p1, p2));
            }

            return result.ToArray();
        }

        /// <summary>
        ///     Returns system screen working area containing the point.        
        ///     Returns null, if not found.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Rect? GetSystemScreenWorkingArea(Point point)
        {
            foreach (Rect systemScreensWorkingArea in GetSystemScreensWorkingAreas())
            {
                if (point.X >= systemScreensWorkingArea.Left &&
                    point.Y >= systemScreensWorkingArea.Top &&
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
        /// <returns></returns>
        public static Rect? GetNearestSystemScreenWorkingArea(Point point)
        {
            double minDistance = Double.MaxValue;
            Rect? nearestSystemScreenWorkingArea = null;
            foreach (Rect systemScreensWorkingArea in GetSystemScreensWorkingAreas())
            {
                var distance = GetDistance(point.X, point.Y, systemScreensWorkingArea.Left + systemScreensWorkingArea.Width / 2, systemScreensWorkingArea.Top + systemScreensWorkingArea.Height / 2);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestSystemScreenWorkingArea = systemScreensWorkingArea;
                }
            }
            return nearestSystemScreenWorkingArea;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static bool IsFullyVisible(Rect rect)
        {
            foreach (Rect systemScreensWorkingArea in GetSystemScreensWorkingAreas())
            {
                if (rect.Left >= systemScreensWorkingArea.Left &&
                    rect.Top >= systemScreensWorkingArea.Top &&
                    rect.Right <= systemScreensWorkingArea.Right &&
                    rect.Bottom <= systemScreensWorkingArea.Bottom)
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Set window fully contained in rect.
        ///     All values in WPF coordinates.        
        /// </summary>
        /// <param name="window"></param>
        /// <param name="rect"></param>
        public static void SetFullyVisible(Window window, Rect rect)
        {            
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
        /// 
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static Screen GetScreen(Window window)
        {
            return Screen.FromHandle(new WindowInteropHelper(window).Handle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameworkElement"></param>
        /// <returns></returns>
        public static Rect GetRect(FrameworkElement frameworkElement)
        {
            var window = Window.GetWindow(frameworkElement);            
            Point p1 = frameworkElement.TranslatePoint(new Point(0, 0), window);
            Point p2 = frameworkElement.TranslatePoint(new Point(frameworkElement.ActualWidth, frameworkElement.ActualHeight), window);
            return new Rect(window.Left + p1.X, window.Top + p1.Y, p2.X - p1.X, p2.Y - p1.Y);
        }

        public static Rect GetRect(Window window, Rect rectInPixels)
        {            
            var t = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
            var p1 = t.Transform(rectInPixels.TopLeft);
            var p2 = t.Transform(rectInPixels.BottomRight);
            return new Rect(p1, p2);
        }

        public static double GetScreenScale(Visual visual)
        {
            return PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice.M11;
        }        

        public static void InvalidatePrimaryScreenScale()
        {
            _primaryScreenScaleX = null;
            _primaryScreenScaleY = null;
        }

        /// <summary>
        ///     Pixels coordinate/WPF coordinate ratio on X-axis.
        /// </summary>
        public static double PrimaryScreenScaleX
        {
            get
            {
                if (_primaryScreenScaleX is null) 
                    DefinePrimaryScreenScale();                
                return _primaryScreenScaleX!.Value;
            }
        }

        /// <summary>
        ///     Pixels coordinate/WPF coordinate ratio on Y-axis.
        /// </summary>
        public static double PrimaryScreenScaleY
        {
            get
            {
                if (_primaryScreenScaleY is null)
                    DefinePrimaryScreenScale();
                return _primaryScreenScaleY!.Value;

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

        private static void DefinePrimaryScreenScale()
        {
            Screen? primaryScreen = Screen.AllScreens.Where(s => s.Primary).FirstOrDefault();
            if (primaryScreen is not null)
            {                
                _primaryScreenScaleX = primaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
                _primaryScreenScaleY = primaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight;
            }
            if (_primaryScreenScaleX is null || Double.IsNaN(_primaryScreenScaleX.Value) || Double.IsInfinity(_primaryScreenScaleX.Value) ||
                _primaryScreenScaleY is null || Double.IsNaN(_primaryScreenScaleY.Value) || Double.IsInfinity(_primaryScreenScaleY.Value))
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    _primaryScreenScaleX = g.DpiX / 96;
                    _primaryScreenScaleY = g.DpiY / 96;
                }
            }
            
            //if (Double.IsNaN(_screenScaleX.Value) || Double.IsInfinity(_screenScaleX.Value) ||
            //    Double.IsNaN(_screenScaleY.Value) || Double.IsInfinity(_screenScaleY.Value))
            //{
            //    _screenScaleX = DpiScale.;
            //    _screenScaleY = g.DpiY / 96;
            //}

            //PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice.M11;
        }

        private static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        #endregion

        #region private fields

        private static double? _primaryScreenScaleX;
        private static double? _primaryScreenScaleY;

        #endregion
    }
}