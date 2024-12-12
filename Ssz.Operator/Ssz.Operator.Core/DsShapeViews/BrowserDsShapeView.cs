/////////////////////////////////////////////////////////////////////////////
//
//                              COPYRIGHT (c) 2021
//                                    SIMCODE.
//                              ALL RIGHTS RESERVED
//
//  This software is a copyrighted work and/or information protected as a
//  trade secret. Legal rights of Simcode. in this software is distinct
//  from ownership of any medium in which the software is embodied. Copyright
//  or trade secret notices included must be reproduced in any copies
//  authorised by Simcode.
//
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class BrowserDsShapeView : DsShapeViewBase
    {
        #region private fields

        private readonly ManualResetEvent _browserReadyEvent = new(false);

        #endregion

        #region private functions

        private void onWindowSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => { UpdateWinFormsContainerSize(); }));
        }

        ///// <summary>
        /////     Callback when the ActiveX browser URL is about to change
        ///// </summary>
        ///// <param name="sender">The object sending the notification</param>
        ///// <param name="navigatingCancelEventArgs">The arguments of the notification</param>
        //private void TheWebBrowserOnBeforeNavigate2(object? sender, DWebBrowserEvents2_BeforeNavigate2Event navigatingCancelEventArgs)
        //{            
        //    bool cancelNavigation = UrlHelper.WebBrowserOnNavigating((string) navigatingCancelEventArgs.uRL, DsShapeViewModel.DsShape, DsShapeViewModel);
        //    if (cancelNavigation)
        //    {
        //        navigatingCancelEventArgs.cancel = true;
        //    }
        //    else
        //    {
        //        //Send notification about the pending navigation
        //        if (BeforeNavigation is not null)
        //        {
        //            BeforeNavigation(this, (string) navigatingCancelEventArgs.uRL);
        //        }
        //    }
        //}

        //private void TheWebBrowserOnNavigateComplete2(object? sender, DWebBrowserEvents2_NavigateComplete2Event e)
        //{
        //    //Send notification about the navigation complete
        //    if (NavigationComplete is not null)
        //    {
        //        NavigationComplete(this, (string)e.uRL);
        //    }
        //}

        #endregion

        #region construction and destruction

        public BrowserDsShapeView(BrowserDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            if (VisualDesignMode)
            {
                DesignModeTextBlock = new TextBlock();
                var grid = new Grid();
                grid.Children.Add(
                    new Rectangle
                    {
                        RadiusX = 5,
                        RadiusY = 5,
                        StrokeThickness = 5,
                        Stroke = new SolidColorBrush(Colors.White),
                        Fill = new SolidColorBrush(Colors.Gray)
                    });
                grid.Children.Add(
                    new Viewbox
                    {
                        Margin = new Thickness(10),
                        Stretch = Stretch.Uniform,
                        Child = DesignModeTextBlock
                    });
                Content = grid;
            }
            else
            {
                //TheWebBrowser = new AxWebBrowser();
                //WindowsFormsHost = new WindowsFormsHost
                //{
                //    Child = TheWebBrowser,
                //};

                //WindowsFormsHost.LayoutError += (sender, args) => args.ThrowException = false;
                //TheWebBrowser.BeforeNavigate2 += TheWebBrowserOnBeforeNavigate2;
                //TheWebBrowser.NavigateComplete2 += TheWebBrowserOnNavigateComplete2;

                Content = WindowsFormsHost;

                var window = Frame is not null ? Frame.PlayWindow as Window : null;
                if (window is not null) window.SizeChanged += onWindowSizeChanged;

                Loaded += (sender, args) => _browserReadyEvent.Set();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                if (!VisualDesignMode)
                {
                    var window = Frame is not null ? Frame.PlayWindow as Window : null;
                    if (window is not null) window.SizeChanged -= onWindowSizeChanged;

                    //if (TheWebBrowser is not null) TheWebBrowser.Dispose();                    
                }

                //TheWebBrowser = null;
                WindowsFormsHost = null;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        ////public AxWebBrowser TheWebBrowser { get; private set; }

        ///// <summary>
        /////     Notification that URL navigation is about to occur
        ///// </summary>
        ///// <remarks>
        /////     The URL being navigated to is passed as the arguments
        ///// </remarks>
        /////public event EventHandler<string> BeforeNavigation;

        ///// <summary>
        /////     Notification that URL navigation is completed
        ///// </summary>
        ///// <remarks>
        /////     The URL being navigated to is passed as the arguments
        ///// </remarks>
        ////public event EventHandler<string> NavigationComplete;

        public void UpdateWinFormsContainerSize()
        {
            if (VisualDesignMode) return;

            var window = Frame is not null ? Frame.PlayWindow as Window : null;
            if (window is null) return;

            GeneralTransform transform = TransformToAncestor(window);
            WindowsFormsHost!.Background = Brushes.AliceBlue;
            var bounds = transform.TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));

            //TheWebBrowser.CtlWidth = (int) bounds.Width;
            //TheWebBrowser.CtlHeight = (int) bounds.Height;

            UpdateLayout();
        }


        public void NavigateTo(string url)
        {
            Task.Run(() =>
            {
                _browserReadyEvent.WaitOne();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        //if (TheWebBrowser is not null)
                        //   TheWebBrowser.Navigate(url);
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogDebug(ex, "BrowserDsShapeView: ");
                    }
                }));
            });
        }

        #endregion

        #region protected functions

        protected TextBlock? DesignModeTextBlock { get; }

        protected WindowsFormsHost? WindowsFormsHost { get; private set; }


        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (BrowserDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.Url))
            {
                if (VisualDesignMode)
                {
                    SetDesignTimeText(dsShape.Url);
                }
                else
                {
                    string url;
                    var existingFileInfo = DsProject.Instance.GetExistingDsPageFileInfoOrNull(dsShape.Url);
                    if (existingFileInfo is not null)
                        url = existingFileInfo.FullName;
                    else
                        url = dsShape.Url;
                    NavigateTo(url);
                }
            }
        }

        protected virtual void SetDesignTimeText(string designTimeText)
        {
            if (DesignModeTextBlock is null) throw new InvalidOperationException();
            DesignModeTextBlock.Text = "Browser: " + designTimeText;
        }

        #endregion
    }
}