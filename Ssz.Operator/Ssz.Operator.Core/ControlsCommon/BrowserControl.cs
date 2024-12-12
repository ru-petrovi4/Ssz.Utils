//using AxSHDocVw;
//using Ssz.Operator.Core.Utils; using Ssz.Utils; 
//using Ssz.Operator.Core.Commands.DsCommandOptions;
//using Ssz.Operator.Core.DataAccess;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Forms.Integration;
//using System.Windows.Media;
//using System.Windows.Threading;

//namespace Ssz.Operator.Core.ControlsCommon
//{
//    public class BrowserControl : ContentControl
//    {
//        #region construction and destruction

//        public BrowserControl()
//        {
//            WebBrowser = new AxWebBrowser();
//            WebBrowser.BeforeNavigate2 += WebBrowserOnBeforeNavigate2;

//            _windowsFormsHost = new WindowsFormsHost
//            {
//                Background = Brushes.AliceBlue,
//                Child = WebBrowser,
//            };
//            _windowsFormsHost.LayoutError += (sender, args) => args.ThrowException = false;

//            Content = _windowsFormsHost;

//            Loaded += (sender, args) =>
//            {
//                _window = Window.GetWindow(this);
//                _window.SizeChanged += WindowOnSizeChanged;
//                UpdateSize();
//                _browserReadyEvent.Set();
//            };

//            Unloaded += (sender, args) =>
//            {
//                _window.SizeChanged -= WindowOnSizeChanged;
//                WebBrowser.Dispose();
//                _windowsFormsHost.Dispose();
//            };
//        }

//        #endregion

//        #region public functions

//        public string Url
//        {
//            get
//            {
//                return _url;
//            }
//            set
//            {
//                if (value == _url) return;
//                _url = value;
//                string urlToNavigate;
//                FileInfo existingFileInfo = DsProject.Instance.GetExistingFileInfoOrNull(_url);
//                if (existingFileInfo is not null)
//                {
//                    urlToNavigate = existingFileInfo.FullName;
//                }
//                else
//                {
//                    urlToNavigate = _url;
//                }
//                Task.Run(() =>
//                {
//                    _browserReadyEvent.WaitOne();
//                    Dispatcher.BeginInvoke(new Action(() =>
//                    {
//                        try
//                        {
//                            WebBrowser.Navigate(urlToNavigate);
//                        }
//                        catch (Exception ex)
//                        {
//                            DsProject.LoggersSet.Logger.Verbose(ex, "BrowserControl: ");
//                        }
//                    }));
//                });
//            }
//        }

//        public Stretch Stretch { get; set; }        

//        public AxWebBrowser WebBrowser { get; private set; }


//        public event EventHandler<string> BeforeNavigation;

//        public void UpdateSize()
//        {            
//            GeneralTransform transform = TransformToAncestor(_window);            
//            Rect bounds = transform.TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));
//            WebBrowser.CtlWidth = (int)bounds.Width - 1;
//            WebBrowser.CtlHeight = (int)bounds.Height - 1;

//            UpdateLayout();
//        }

//        #endregion

//        #region private functions

//        private void WindowOnSizeChanged(object? sender, SizeChangedEventArgs e)
//        {
//            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
//            {
//                UpdateSize();
//            }));
//        }


//        private void WebBrowserOnBeforeNavigate2(object? sender, DWebBrowserEvents2_BeforeNavigate2Event navigatingCancelEventArgs)
//        {
//            bool cancelNavigation = UrlHelper.WebBrowserOnNavigating((string)navigatingCancelEventArgs.uRL, DsProject.Instance, DataContext as DataViewModel);
//            if (cancelNavigation)
//            {
//                navigatingCancelEventArgs.cancel = true;
//            }
//            else
//            {
//                //Send notification about the pending navigation
//                if (BeforeNavigation is not null)
//                {
//                    BeforeNavigation(this, (string)navigatingCancelEventArgs.uRL);
//                }
//            }
//        }

//        #endregion

//        #region private fields

//        private string _url;
//        private readonly ManualResetEvent _browserReadyEvent = new ManualResetEvent(false);
//        private Window _window;
//        private WindowsFormsHost _windowsFormsHost;

//        #endregion
//    }
//}

