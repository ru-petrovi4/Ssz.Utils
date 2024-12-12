using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Xceed.Wpf.Toolkit.Core.Input;
using Ssz.Xceed.Wpf.Toolkit.Zoombox;

namespace Ssz.Operator.Core.ControlsPlay.ZoomboxPlay
{
    public partial class ZoomboxPlayControl : PlayControlBase
    {
        #region protected functions

        protected PlayDsPageDrawingCanvas? PlayDsPageDrawingCanvas
        {
            get
            {
                var content = ((Zoombox) MainBusyIndicator.Content).Content as Viewbox;
                if (content is null) return null;
                return (PlayDsPageDrawingCanvas) content.Child;
            }
            set => ((Zoombox) MainBusyIndicator.Content).Content = value;
        }

        #endregion

        #region construction and destruction

        public ZoomboxPlayControl(IPlayWindow playWindow) :
            base(playWindow)
        {
            InitializeComponent();

            ZoomboxCursors.ZoomRelative = Cursors.Arrow;
            var zoombox = new Zoombox
            {
                SnapsToDevicePixels = false,
                IsAnimated = true,
                ZoomOn = ZoomboxZoomOn.View,
                AnimationDecelerationRatio = 1,
                DragModifiers = new KeyModifierCollection {KeyModifier.None},
                RelativeZoomModifiers = new KeyModifierCollection {KeyModifier.None}
            };
            zoombox.SetValue(Zoombox.ViewFinderVisibilityProperty, Visibility.Visible);

            MainBusyIndicator.Content = zoombox;

            if (DsProject.Instance.Review)
                InfoTextBlock.Visibility = Visibility.Visible;

            if (DsProject.Instance.GetAddon<ZoomboxAddon>().ShowNavigationPanel == DefaultFalseTrue.False)
                NavigationPanel.Visibility = Visibility.Collapsed;
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                var disposable = PlayDsPageDrawingCanvas as IDisposable;
                if (disposable is not null) disposable.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public override async void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {
            var dsPageDrawing = DsProject.Instance.ReadDsPageInPlay(
                jumpInfo.FileRelativePath,
                jumpInfo.SenderContainerCopy,
                PlayWindow);
            if (dsPageDrawing is null)
                return;

            DsPageDrawing = dsPageDrawing;
            InfoTextBlock.Text = "DsPage: " + DsPageDrawing.Name;

            await Task.Delay(1);

            var zoomboxDsPageType = DsPageDrawing.DsPageTypeObject as ZoomboxDsPageType;
            var zoomboxAddon = DsProject.Instance.GetAddon<ZoomboxAddon>();
            if (zoomboxDsPageType is null || zoomboxAddon is null) return;

            MainBusyIndicator.IsBusy = true;

            var disposable = PlayDsPageDrawingCanvas as IDisposable;
            if (disposable is not null) disposable.Dispose();

            var playDsPageDrawingCanvas =
                new PlayDsPageDrawingCanvas(DsPageDrawing, PlayWindow.MainFrame);
            playDsPageDrawingCanvas.Loaded +=
                async (sender, args) =>
                {
                    MainBusyIndicator.IsBusy = false;

                    await Task.Delay(400);

                    await Dispatcher.BeginInvoke(
                        new Action(
                            () => ((Zoombox) MainBusyIndicator.Content).FitToBounds()),
                        DispatcherPriority.Background);
                };

            PlayDsPageDrawingCanvas =
                playDsPageDrawingCanvas;
        }

        public override bool PrepareWindow(IPlayWindow newWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            if (showWindowDsCommandOptions.WindowFullScreen == DefaultFalseTrue.Default)
                showWindowDsCommandOptions.WindowFullScreen = DefaultFalseTrue.True;

            return false;
        }

        #endregion

        #region private functions

        private void BackOnClick(object? sender, RoutedEventArgs e)
        {
            CommandsManager.NotifyCommand(PlayWindow.MainFrame, CommandsManager.JumpBackCommand,
                new JumpBackDsCommandOptions
                {
                    TargetWindow = TargetWindow.CurrentWindow
                });
        }

        private void ForwardOnClick(object? sender, RoutedEventArgs e)
        {
            CommandsManager.NotifyCommand(PlayWindow.MainFrame, CommandsManager.JumpForwardCommand,
                new JumpForwardDsCommandOptions
                {
                    TargetWindow = TargetWindow.CurrentWindow
                });
        }

        private void HomeOnClick(object? sender, RoutedEventArgs e)
        {
            WindowProps rootWindowProps = DsProject.Instance.RootWindowProps;

            CommandsManager.NotifyCommand(PlayWindow.MainFrame, CommandsManager.JumpCommand,
                new JumpDsCommandOptions
                {
                    TargetWindow = TargetWindow.CurrentWindow,
                    FileRelativePath = rootWindowProps.FileRelativePath
                });
        }

        #endregion
    }
}