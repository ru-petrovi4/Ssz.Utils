using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    /// <summary>
    ///     The play control of a panorama page: the panorama itself plus the frame page the panorama addon
    ///     overlays it with. Port of the WPF PanoramaPlayControl.
    /// </summary>
    public class PanoramaPlayControl : PlayControlBase
    {
        #region construction and destruction

        public PanoramaPlayControl(IPlayWindow playWindow) :
            base(playWindow)
        {
            Viewport = new PanoramaViewport { IsActive = true };

            FrameContentControl = new ContentControl
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                ZIndex = 2
            };

            // The page of the panorama has to be a live part of the tree - its shapes load their images
            // and evaluate their bindings only then - but nothing of it may show up on its own: the
            // panorama draws it as the texture of the mesh.
            _offscreenHost = new Canvas
            {
                Width = 0,
                Height = 0,
                ClipToBounds = true,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            _mainGrid = new Grid();
            _mainGrid.Children.Add(_offscreenHost);
            _mainGrid.Children.Add(Viewport);
            _mainGrid.Children.Add(FrameContentControl);

            Content = _mainGrid;
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                Viewport.Dispose();

                if (FrameContentControl.Content is IDisposable disposable)
                    disposable.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public PanoramaViewport Viewport { get; }

        public ContentControl FrameContentControl { get; }

        public override async void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {
            var dsPageDrawing = await DsProject.Instance.ReadDsPageInPlayAsync(
                jumpInfo.FileRelativePath,
                jumpInfo.SenderContainerCopy,
                PlayWindow);
            if (dsPageDrawing is null)
                return;

            DsPageDrawing = dsPageDrawing;

            jumpInfo.JumpContext = null;

            var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
            var panoramaDsPageType = dsPageDrawing.DsPageTypeObject as PanoramaDsPageType;
            if (panoramaAddon is null || panoramaDsPageType is null) return;

            // The view of the previous point is kept, so that a jump does not spin the operator around.
            double? viewAzimuth = Viewport.PanoramaDsPageType is not null ? Viewport.GetViewAzimuth() : null;

            var playDsPageDrawingCanvas = new PlayDsPageDrawingCanvas(dsPageDrawing, PlayWindow.MainFrame)
            {
                Width = dsPageDrawing.Width,
                Height = dsPageDrawing.Height
            };
            _offscreenHost.Children.Clear();
            _offscreenHost.Children.Add(playDsPageDrawingCanvas);

            Viewport.Show(dsPageDrawing, playDsPageDrawingCanvas, viewAzimuth);

            await ShowFrameAsync(panoramaAddon, panoramaDsPageType, dsPageDrawing);
        }

        public override bool PrepareWindow(IPlayWindow newWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            return false;
        }

        public override bool PrepareChildWindow(IPlayWindow newChildWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            return false;
        }

        #endregion

        #region private functions

        private async System.Threading.Tasks.Task ShowFrameAsync(PanoramaAddon panoramaAddon,
            PanoramaDsPageType panoramaDsPageType, DsPageDrawing dsPageDrawing)
        {
            string frameDsPageDrawingFileName = panoramaDsPageType.FrameDsPageDrawingFileName;
            if (string.IsNullOrWhiteSpace(frameDsPageDrawingFileName))
                frameDsPageDrawingFileName = panoramaAddon.FrameDsPageDrawingFileName;

            if (FrameContentControl.Content is IDisposable disposable)
                disposable.Dispose();

            if (string.IsNullOrWhiteSpace(frameDsPageDrawingFileName))
            {
                FrameContentControl.Content = null;
                return;
            }

            // DsPageDrawing is parent for generic params resolving.
            var frameDsPageDrawing = await DsProject.Instance.ReadDsPageInPlayAsync(frameDsPageDrawingFileName,
                dsPageDrawing, PlayWindow);

            if (frameDsPageDrawing is null)
            {
                MessageBoxHelper.ShowError(Properties.Resources.ReadFrameDrawingErrorMessage + @" " +
                                           frameDsPageDrawingFileName);
                return;
            }

            var frameDsPageDrawingViewbox = new PlayDsPageDrawingViewbox(frameDsPageDrawing, PlayWindow.MainFrame)
            {
                Background = null
            };
            var playDrawingCanvas = TreeHelper.FindChild<PlayDrawingCanvas>(frameDsPageDrawingViewbox);
            if (playDrawingCanvas is not null)
                playDrawingCanvas.Background = null;

            FrameContentControl.Content = frameDsPageDrawingViewbox;
        }

        #endregion

        #region private fields

        private readonly Grid _mainGrid;
        private readonly Canvas _offscreenHost;

        #endregion
    }
}
