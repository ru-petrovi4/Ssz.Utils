using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Threading;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;

namespace Ssz.Operator.Core.ControlsPlay.ZoomboxPlay;

public partial class ZoomboxPlayControl : PlayControlBase
{
    #region construction and destruction

    public ZoomboxPlayControl(IPlayWindow playWindow) :
        base(playWindow)
    {
        InitializeComponent();        

        if (DsProject.Instance.Review)
            InfoTextBlock.IsVisible = true;

        if (DsProject.Instance.GetAddon<ZoomboxAddon>().ShowNavigationPanel == DefaultFalseTrue.False)
            NavigationPanel.IsVisible = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;
        if (disposing)
        {
            if (PlayDsPageDrawingCanvas is IDisposable disposable)
                disposable.Dispose();
        }

        base.Dispose(disposing);
    }

    #endregion

    #region public functions

    public override async void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
    {
        DsPageDrawing = await DsProject.Instance.ReadDsPageInPlayAsync(
                jumpInfo.FileRelativePath,
                jumpInfo.SenderContainerCopy,
                PlayWindow);
        if (DsPageDrawing is null)
            return;

        InfoTextBlock.Text = "DsPage: " + DsPageDrawing.Name;

        await Task.Delay(1);

        var zoomboxDsPageType = DsPageDrawing.DsPageTypeObject as ZoomboxDsPageType;
        var zoomboxAddon = DsProject.Instance.GetAddon<ZoomboxAddon>();
        if (zoomboxDsPageType is null || zoomboxAddon is null) return;

        IsBusy = true;

        if (PlayDsPageDrawingCanvas is IDisposable d)
            d.Dispose();

        var playDsPageDrawingCanvas =
            new PlayDsPageDrawingCanvas(DsPageDrawing, PlayWindow.MainFrame);

        // Аналог Loaded += ... FitToBounds()
        // В Avalonia используем событие Loaded на Control
        playDsPageDrawingCanvas.Loaded +=
            async (sender, args) =>
            {
                IsBusy = false;

                await Task.Delay(400);

                // Аналог Dispatcher.BeginInvoke(..., DispatcherPriority.Background)
                //await Dispatcher.UIThread.InvokeAsync(
                //    () => ZoomBorder.Fill(),          // Fill() = FitToBounds()
                //    DispatcherPriority.Background);
            };

        PlayDsPageDrawingCanvas = playDsPageDrawingCanvas;
    }

    public override bool PrepareWindow(IPlayWindow newWindow,
        ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
    {
        if (showWindowDsCommandOptions.WindowFullScreen == DefaultFalseTrue.Default)
            showWindowDsCommandOptions.WindowFullScreen = DefaultFalseTrue.True;

        return false;
    }

    #endregion

    #region protected functions

    protected PlayDsPageDrawingCanvas? PlayDsPageDrawingCanvas
    {
        get => ZoomBorder.Child as PlayDsPageDrawingCanvas;
        set => ZoomBorder.Child = value;
    }

    #endregion

    #region private properties
    
    private bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            // BusyOverlay — Border, объявленный в AXAML
            if (BusyOverlay is not null)
                BusyOverlay.IsVisible = value;
        }
    }

    #endregion                

    #region private functions

    private void BackOnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CommandsManager.NotifyCommand(PlayWindow.MainFrame, CommandsManager.JumpBackCommand,
            new JumpBackDsCommandOptions
            {
                TargetWindow = TargetWindow.CurrentWindow
            });
    }

    private void ForwardOnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CommandsManager.NotifyCommand(PlayWindow.MainFrame, CommandsManager.JumpForwardCommand,
            new JumpForwardDsCommandOptions
            {
                TargetWindow = TargetWindow.CurrentWindow
            });
    }

    private void HomeOnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

    #region private fields

    private bool _isBusy;

    #endregion
}
