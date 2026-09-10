using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    /// <summary>
    ///     The play control of a panorama page: the panorama itself plus the frame page the panorama addon
    ///     overlays it with, and the compass in the corner. Port of the WPF PanoramaPlayControl.
    ///     Two viewports take turns, as they did in WPF: a jump to a neighbouring point is a movement, and
    ///     the point that is being left has to stay on the screen while the new one turns into place.
    /// </summary>
    public class PanoramaPlayControl : PlayControlBase
    {
        #region construction and destruction

        public PanoramaPlayControl(IPlayWindow playWindow) :
            base(playWindow)
        {
            _firstViewport = new PanoramaViewport();
            _secondViewport = new PanoramaViewport();
            _previousViewport = _firstViewport;

            FrameContentControl = new ContentControl
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                ZIndex = 2
            };

            CompassControl = new CompassControl
            {
                Width = 128,
                Height = 128,
                Margin = new Avalonia.Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                IsHitTestVisible = false,
                Opacity = 0.7,
                ZIndex = 5
            };

            // The page of a panorama has to be a live part of the tree - its shapes load their images
            // and evaluate their bindings only then - but nothing of it may show up on its own: the
            // panorama draws it as the texture of the mesh.
            _firstHost = NewOffscreenHost();
            _secondHost = NewOffscreenHost();

            _mainGrid = new Grid();
            _mainGrid.Children.Add(_firstHost);
            _mainGrid.Children.Add(_secondHost);
            _mainGrid.Children.Add(_firstViewport);
            _mainGrid.Children.Add(_secondViewport);
            _mainGrid.Children.Add(FrameContentControl);
            _mainGrid.Children.Add(CompassControl);

            _firstViewport.CameraChanged += () => OnViewportCameraChanged(_firstViewport);
            _secondViewport.CameraChanged += () => OnViewportCameraChanged(_secondViewport);

            Content = _mainGrid;
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                StopTransition();

                _firstViewport.Dispose();
                _secondViewport.Dispose();

                if (FrameContentControl.Content is IDisposable disposable)
                    disposable.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public ContentControl FrameContentControl { get; }

        public CompassControl CompassControl { get; }

        public PanoramaViewport? CurrentViewport => _currentViewport;

        public override async void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {
            var dsPageDrawing = await DsProject.Instance.ReadDsPageInPlayAsync(
                jumpInfo.FileRelativePath,
                jumpInfo.SenderContainerCopy,
                PlayWindow);
            if (dsPageDrawing is null)
                return;

            DsPageDrawing = dsPageDrawing;

            var panoramaJumpDsCommandOptions = jumpInfo.JumpContext as PanoramaJumpDsCommandOptions;
            jumpInfo.JumpContext = null;

            var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
            var panoramaDsPageType = dsPageDrawing.DsPageTypeObject as PanoramaDsPageType;
            if (panoramaAddon is null || panoramaDsPageType is null) return;

            StopTransition();

            // The two viewports take turns: the one that showed the point so far keeps showing it until
            // the transition to the new one has finished.
            if (_currentViewport is null)
            {
                _previousViewport = _secondViewport;
                _currentViewport = _firstViewport;
            }
            else
            {
                _previousViewport = _currentViewport;
                _currentViewport = ReferenceEquals(_currentViewport, _firstViewport)
                    ? _secondViewport
                    : _firstViewport;
            }

            var animationDurationMs = panoramaAddon.AnimationDurationMs;

            // A jump straight up or down is not a movement through the scene, so it is not animated.
            var smoothJump = panoramaJumpDsCommandOptions is not null &&
                             _previousViewport.PanoramaDsPageType is not null &&
                             Math.Abs(new Any(panoramaJumpDsCommandOptions.VerticalDelta).ValueAsDouble(false)) <
                             0.5 &&
                             animationDurationMs > 0 && animationDurationMs < 30000;

            double viewAzimuth;
            if (_previousViewport.PanoramaDsPageType is not null)
                viewAzimuth = smoothJump
                    ? PanoramaUtils.NormalizeAngleInDegrees(
                        _previousViewport.PanoramaDsPageType.LeftEdgeAzimuth +
                        panoramaJumpDsCommandOptions!.JumpHorizontalK * 360)
                    : _previousViewport.GetViewAzimuth();
            else
                viewAzimuth = panoramaDsPageType.DefaultViewAzimuth;

            _previousViewport.IsActive = false;
            _currentViewport.IsActive = false;

            var host = ReferenceEquals(_currentViewport, _firstViewport) ? _firstHost : _secondHost;
            var playDsPageDrawingCanvas = new PlayDsPageDrawingCanvas(dsPageDrawing, PlayWindow.MainFrame)
            {
                Width = dsPageDrawing.Width,
                Height = dsPageDrawing.Height
            };
            host.Children.Clear();
            host.Children.Add(playDsPageDrawingCanvas);

            _currentViewport.Show(dsPageDrawing, playDsPageDrawingCanvas, viewAzimuth);

            await ShowFrameAsync(panoramaAddon, panoramaDsPageType, dsPageDrawing);

            // The page needs a moment to be laid out and drawn before it may be shown.
            await System.Threading.Tasks.Task.Delay(50);

            if (Disposed) return;

            if (smoothJump)
                StartTransition(animationDurationMs);
            else
                OnTransitionFinished();
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

        private static Canvas NewOffscreenHost()
        {
            return new Canvas
            {
                Width = 0,
                Height = 0,
                ClipToBounds = true,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
        }

        private void OnViewportCameraChanged(PanoramaViewport viewport)
        {
            if (!ReferenceEquals(viewport, _currentViewport)) return;

            CompassControl.SetViewAzimuth(viewport.GetViewAzimuth());
        }

        /// <summary>
        ///     The transition of the WPF control, animated by hand: Avalonia animates properties of
        ///     controls, and the camera of a panorama is none.
        ///     The point that is being left fades out and zooms in over the first part of the time, while
        ///     both points turn from the old view direction to the new one.
        /// </summary>
        private void StartTransition(int animationDurationMs)
        {
            var previousViewport = _previousViewport;
            var currentViewport = _currentViewport;
            if (currentViewport is null) return;

            var azimuthFrom = previousViewport.GetViewAzimuth();
            var azimuthTo = currentViewport.GetViewAzimuth();
            var rotationZDelta = PanoramaUtils.NormalizeAngle2InDegrees(azimuthTo - azimuthFrom);

            _transitionPreviousRotationZFrom = previousViewport.RotationZ;
            _transitionPreviousRotationZTo = previousViewport.RotationZ + rotationZDelta;
            _transitionCurrentRotationZFrom = currentViewport.RotationZ - rotationZDelta;
            _transitionCurrentRotationZTo = currentViewport.RotationZ;
            _transitionRotationYFrom = previousViewport.RotationY;
            _transitionRotationYTo = currentViewport.RotationY;
            _transitionPreviousFieldOfViewFrom = previousViewport.FieldOfView;
            _transitionCurrentFieldOfView = currentViewport.FieldOfView;
            _transitionDurationMs = animationDurationMs;

            // The point that is being left stays on top and fades away, showing the new one behind it.
            previousViewport.ZIndex = 1;
            currentViewport.ZIndex = 0;

            _transitionStopwatch = Stopwatch.StartNew();
            _transitionTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render,
                (sender, args) => OnTransitionTimer());
            _transitionTimer.Start();

            OnTransitionTimer();
        }

        private void OnTransitionTimer()
        {
            var currentViewport = _currentViewport;
            if (Disposed || currentViewport is null || _transitionStopwatch is null)
            {
                StopTransition();
                return;
            }

            var time = _transitionDurationMs <= 0
                ? 1.0
                : Math.Min(1.0, _transitionStopwatch.Elapsed.TotalMilliseconds / _transitionDurationMs);

            var turned = Decelerate(time);
            var rotationY = _transitionRotationYFrom + (_transitionRotationYTo - _transitionRotationYFrom) * turned;

            // The fading and the zoom of the old point are over well before the turn is.
            var fadeTime = Math.Min(1.0, time / FirstPartRatio);

            _previousViewport.SetCamera(
                _transitionPreviousFieldOfViewFrom +
                (PreviousFieldOfViewTo - _transitionPreviousFieldOfViewFrom) * fadeTime,
                rotationY,
                _transitionPreviousRotationZFrom +
                (_transitionPreviousRotationZTo - _transitionPreviousRotationZFrom) * turned);
            _previousViewport.Opacity = 1.0 - fadeTime;

            currentViewport.SetCamera(_transitionCurrentFieldOfView, rotationY,
                _transitionCurrentRotationZFrom +
                (_transitionCurrentRotationZTo - _transitionCurrentRotationZFrom) * turned);

            if (time >= 1.0)
            {
                StopTransition();
                OnTransitionFinished();
            }
        }

        /// <summary>
        ///     What the DecelerationRatio of the WPF animation did: the turn keeps its speed and comes to
        ///     a stop over the last tenth of the time.
        /// </summary>
        private static double Decelerate(double time)
        {
            const double decelerationRatio = 0.1;
            var speed = 1.0 / (1.0 - decelerationRatio / 2);

            if (time <= 1.0 - decelerationRatio) return speed * time;

            var decelerationTime = (time - (1.0 - decelerationRatio)) / decelerationRatio;
            return speed * (1.0 - decelerationRatio) +
                   speed * decelerationRatio * (decelerationTime - decelerationTime * decelerationTime / 2);
        }

        private void StopTransition()
        {
            _transitionTimer?.Stop();
            _transitionTimer = null;
            _transitionStopwatch = null;
        }

        private void OnTransitionFinished()
        {
            var currentViewport = _currentViewport;
            if (currentViewport is null) return;

            var previousHost = ReferenceEquals(_previousViewport, _firstViewport) ? _firstHost : _secondHost;
            if (!ReferenceEquals(_previousViewport, currentViewport))
            {
                _previousViewport.ReleasePage();
                previousHost.Children.Clear();
            }

            _previousViewport.IsActive = false;
            _previousViewport.Opacity = 1.0;
            _previousViewport.ZIndex = 0;

            currentViewport.ZIndex = 1;
            currentViewport.IsActive = true;

            CompassControl.SetViewAzimuth(currentViewport.GetViewAzimuth());
        }

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

        private const double FirstPartRatio = 0.7;
        private const double PreviousFieldOfViewTo = 70;

        private readonly Grid _mainGrid;
        private readonly Canvas _firstHost;
        private readonly Canvas _secondHost;
        private readonly PanoramaViewport _firstViewport;
        private readonly PanoramaViewport _secondViewport;

        private PanoramaViewport _previousViewport;
        private PanoramaViewport? _currentViewport;

        private DispatcherTimer? _transitionTimer;
        private Stopwatch? _transitionStopwatch;
        private int _transitionDurationMs;
        private double _transitionPreviousRotationZFrom;
        private double _transitionPreviousRotationZTo;
        private double _transitionCurrentRotationZFrom;
        private double _transitionCurrentRotationZTo;
        private double _transitionRotationYFrom;
        private double _transitionRotationYTo;
        private double _transitionPreviousFieldOfViewFrom;
        private double _transitionCurrentFieldOfView;

        #endregion
    }
}
