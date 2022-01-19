//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.UI.Xaml;
//using Windows.Foundation;

//namespace Ssz.Utils.Wpf
//{    
//    public static class ScreenHelper
//    {
//        #region public functions        

//        /// <summary>        
//        ///     First screen in array is primary screen.
//        ///     Ordered by device name.
//        /// </summary>
//        /// <returns></returns>
//        public static Rect[] GetSystemScreensWorkingAreasInPixels()
//        {
//            var result = new List<Rect>();

//            //Screen[] screens = Screen.AllScreens.OrderByDescending(s => s.Primary).ThenBy(s => s.DeviceName).ToArray();

//            //foreach (Screen screen in screens)
//            //{
//            //    Rectangle workingArea = screen.WorkingArea;
//            //    result.Add(new Rect(workingArea.Left, workingArea.Top,
//            //        workingArea.Width, workingArea.Height));
//            //}

//            return result.ToArray();
//        }

//        /// <summary>
//        ///     Returns primary screen working area in WPF coordinates.        
//        /// </summary>
//        /// <returns></returns>
//        public static Rect GetPrimarySystemScreenWorkingAreaInPixels()
//        {
//            Rectangle workingArea = Screen.AllScreens.First(s => s.Primary).WorkingArea;

//            return new Rect(workingArea.Left, workingArea.Top,
//                    workingArea.Width, workingArea.Height);
//        }

//        /// <summary>
//        ///     Returns system screen working area containing the point.
//        ///     All values in pixel coordinates.
//        ///     Returns null, if not found.
//        /// </summary>
//        /// <param name="pointInPixels"></param>
//        /// <returns></returns>
//        public static Rect? GetSystemScreenWorkingAreaInPixels(Point pointInPixels)
//        {
//            foreach (Rect screen in GetSystemScreensWorkingAreasInPixels())
//            {
//                if (pointInPixels.X >= screen.Left &&
//                    pointInPixels.Y >= screen.Top &&
//                    pointInPixels.X <= screen.Right &&
//                    pointInPixels.Y <= screen.Bottom)
//                    return screen;
//            }
//            return null;
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="rectInPixels"></param>
//        /// <returns></returns>
//        public static bool IsFullyVisible(Rect rectInPixels)
//        {
//            foreach (Rect screenInPixels in GetSystemScreensWorkingAreasInPixels())
//            {
//                if (rectInPixels.Left >= screenInPixels.Left &&
//                    rectInPixels.Top >= screenInPixels.Top &&
//                    rectInPixels.Right <= screenInPixels.Right &&
//                    rectInPixels.Bottom <= screenInPixels.Bottom)
//                    return true;
//            }
//            return false;
//        }

//        /// <summary>
//        ///     Set window fully contained in rect.
//        ///     All values in WPF coordinates.        
//        /// </summary>
//        /// <param name="window"></param>
//        /// <param name="rect"></param>
//        public static void SetFullyVisible(Window window, Rect rect)
//        {            
//            if (Double.IsNaN(window.Left) || Double.IsNaN(window.Top) ||
//                Double.IsNaN(window.ActualWidth) || Double.IsNaN(window.ActualHeight) ||
//                rect == Rect.Empty) return;

//            if ((int)window.Left < (int)rect.X)
//            {
//                window.Left = rect.X;
//            }
//            if ((int)window.Top < (int)rect.Y)
//            {
//                window.Top = rect.Y;
//            }
//            if ((int)(window.Left + window.ActualWidth) > (int)(rect.X + rect.Width))
//            {
//                window.Left = rect.X + rect.Width - window.ActualWidth;
//            }
//            if ((int)(window.Top + window.ActualHeight) > (int)(rect.Y + rect.Height))
//            {
//                window.Top = rect.Y + rect.Height - window.ActualHeight;
//            }
//        }

//        /// <summary>
//        ///     All values in pixels coordinates.
//        ///     Returns Rect.Empty if not found.
//        /// </summary>
//        /// <param name="pointInPixels"></param>
//        /// <returns></returns>
//        public static Rect GetNearestSystemScreenWorkingAreaInPixels(Point pointInPixels)
//        {
//            double minDistance = Double.MaxValue;
//            Rect nearestSystemScreen = Rect.Empty;
//            foreach (Rect screen in GetSystemScreensWorkingAreasInPixels())
//            {
//                var distance = GetDistance(pointInPixels.X, pointInPixels.Y, screen.Left + screen.Width / 2, screen.Top + screen.Height / 2);
//                if (distance < minDistance)
//                {
//                    minDistance = distance;
//                    nearestSystemScreen = screen;
//                }
//            }
//            return nearestSystemScreen;
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="window"></param>
//        /// <returns></returns>
//        public static Screen GetScreen(Window window)
//        {
//            return Screen.FromHandle(new WindowInteropHelper(window).Handle);
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="frameworkElement"></param>
//        /// <returns></returns>
//        public static Rect GetRectInPixels(FrameworkElement frameworkElement)
//        {            
//            var p1 = frameworkElement.PointToScreen(new Point(0, 0));
//            var p2 = frameworkElement.PointToScreen(new Point(frameworkElement.ActualWidth, frameworkElement.ActualHeight));
//            return new Rect(p1, p2);
//        }

//        /// <summary>
//        ///     Returns in WPF coordinates.
//        /// </summary>
//        /// <param name="frameworkElement"></param>
//        /// <returns></returns>
//        public static Rect GetRect(FrameworkElement frameworkElement)
//        {
//            var p1 = frameworkElement.PointToScreen(new Point(0, 0));
//            var p2 = frameworkElement.PointToScreen(new Point(frameworkElement.ActualWidth, frameworkElement.ActualHeight));
//            var window = Window.GetWindow(frameworkElement);
//            var t = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
//            p1 = t.Transform(p1);
//            p2 = t.Transform(p2);            
//            return new Rect(p1, p2);
//        }

//        public static Rect GetRect(Window window, Rect rectInPixels)
//        {            
//            var t = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
//            var p1 = t.Transform(rectInPixels.TopLeft);
//            var p2 = t.Transform(rectInPixels.BottomRight);
//            return new Rect(p1, p2);
//        }

//        public static double GetScreenScale(Visual visual)
//        {
//            return PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice.M11;
//        }        

//        public static void InvalidatePrimaryScreenScale()
//        {
//            _primaryScreenScaleX = null;
//            _primaryScreenScaleY = null;
//        }

//        /// <summary>
//        ///     Pixels coordinate/WPF coordinate ratio on X-axis.
//        /// </summary>
//        public static double PrimaryScreenScaleX
//        {
//            get
//            {
//                if (_primaryScreenScaleX is null) 
//                    DefinePrimaryScreenScale();                
//                return _primaryScreenScaleX!.Value;
//            }
//        }

//        /// <summary>
//        ///     Pixels coordinate/WPF coordinate ratio on Y-axis.
//        /// </summary>
//        public static double PrimaryScreenScaleY
//        {
//            get
//            {
//                if (_primaryScreenScaleY is null)
//                    DefinePrimaryScreenScale();
//                return _primaryScreenScaleY!.Value;

//            }
//        }

//        /// <summary>
//        ///     Returns true if valid.
//        /// </summary>
//        /// <param name="length"></param>
//        /// <returns></returns>
//        public static bool IsValidCoordinate(double? length)
//        {
//            if (!length.HasValue) return false;
//            double l = length.Value;
//            if (Double.IsNaN(l) || Double.IsInfinity(l)) return false;
//            return true;
//        }

//        /// <summary>
//        ///     Returns true if valid.
//        /// </summary>
//        /// <param name="length"></param>
//        /// <returns></returns>
//        public static bool IsValidLength(double? length)
//        {
//            if (!length.HasValue) return false;
//            double l = length.Value;
//            if (Double.IsNaN(l) || Double.IsInfinity(l)) return false;
//            return l > 0.0;
//        }

//        #endregion

//        #region private functions

//        private static void DefinePrimaryScreenScale()
//        {
//            Screen? primaryScreen = Screen.AllScreens.Where(s => s.Primary).FirstOrDefault();
//            if (primaryScreen is not null)
//            {                
//                _primaryScreenScaleX = primaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
//                _primaryScreenScaleY = primaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight;
//            }
//            if (_primaryScreenScaleX is null || Double.IsNaN(_primaryScreenScaleX.Value) || Double.IsInfinity(_primaryScreenScaleX.Value) ||
//                _primaryScreenScaleY is null || Double.IsNaN(_primaryScreenScaleY.Value) || Double.IsInfinity(_primaryScreenScaleY.Value))
//            {
//                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
//                {
//                    _primaryScreenScaleX = g.DpiX / 96;
//                    _primaryScreenScaleY = g.DpiY / 96;
//                }
//            }
            
//            //if (Double.IsNaN(_screenScaleX.Value) || Double.IsInfinity(_screenScaleX.Value) ||
//            //    Double.IsNaN(_screenScaleY.Value) || Double.IsInfinity(_screenScaleY.Value))
//            //{
//            //    _screenScaleX = DpiScale.;
//            //    _screenScaleY = g.DpiY / 96;
//            //}

//            //PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice.M11;
//        }

//        private static double GetDistance(double x1, double y1, double x2, double y2)
//        {
//            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
//        }

//        #endregion

//        #region private fields

//        private static double? _primaryScreenScaleX;
//        private static double? _primaryScreenScaleY;

//        #endregion
//    }
//}