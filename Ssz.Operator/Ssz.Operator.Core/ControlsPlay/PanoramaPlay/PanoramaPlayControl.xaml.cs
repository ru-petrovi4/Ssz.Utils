using System;
using System.Windows;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    public partial class PanoramaPlayControl : PlayControlBase
    {
        #region construction and destruction

        public PanoramaPlayControl(IPlayWindow playWindow) :
            base(playWindow)
        {
            InitializeComponent();

            FirstPanoramaViewport3D.Init(this);
            SecondPanoramaViewport3D.Init(this);
            PanoramaDesignViewport3D.Init(this);

            PreviousPanoramaViewport3D = FirstPanoramaViewport3D;

            RegisterName(FirstPanoramaViewport3D.Name + @"PanoramaRotation3D",
                FirstPanoramaViewport3D.PanoramaRotation3D);
            RegisterName(SecondPanoramaViewport3D.Name + @"PanoramaRotation3D",
                SecondPanoramaViewport3D.PanoramaRotation3D);

            var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
            if (panoramaAddon.ShowLines)
            {
                LinesViewport3D.Init(panoramaAddon.PanoPointsCollection);
                RegisterName(@"MapTranslateTransform3D", LinesViewport3D.MapTranslateTransform3D);
                RegisterName(@"MapRotation3D", LinesViewport3D.MapRotation3D);
            }

            if (DsProject.Instance.Review ||
                DsProject.Instance.GetAddon<PanoramaAddon>().AllowDesignModeInPlay)
                InfoTextBlock.Visibility = Visibility.Visible;
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.
                FirstPanoramaViewport3D.Dispose();
                SecondPanoramaViewport3D.Dispose();
                LinesViewport3D.Dispose();
                PanoramaDesignViewport3D.Dispose();

                var disposable = FrameContentControl.Content as IDisposable;
                if (disposable is not null) disposable.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public PanoramaViewport3D PreviousPanoramaViewport3D { get; set; }

        public PanoramaViewport3D? CurrentPanoramaViewport3D { get; set; }


        public override void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {
            var dsPageDrawing = DsProject.Instance.ReadDsPageInPlay(
                jumpInfo.FileRelativePath,
                jumpInfo.SenderContainerCopy,
                PlayWindow);
            if (dsPageDrawing is null)
                return;

            DsPageDrawing = dsPageDrawing;

            var panoramaJumpDsCommandOptions = jumpInfo.JumpContext as PanoramaJumpDsCommandOptions;
            jumpInfo.JumpContext = null;            

            InfoTextBlock.Text = "Point: " + DsPageDrawing.Name;

            var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
            var panoramaDsPageType = DsPageDrawing.DsPageTypeObject as PanoramaDsPageType;
            if (panoramaAddon is null || panoramaDsPageType is null) return;

            string frameDsPageDrawingFileName = panoramaDsPageType.FrameDsPageDrawingFileName;
            if (string.IsNullOrWhiteSpace(frameDsPageDrawingFileName))
                frameDsPageDrawingFileName = panoramaAddon.FrameDsPageDrawingFileName;

            var disposable = FrameContentControl.Content as IDisposable;
            if (disposable is not null) disposable.Dispose();
            if (string.IsNullOrWhiteSpace(frameDsPageDrawingFileName))
            {
                FrameContentControl.Content = null;
            }
            else
            {
                // DsPageDrawing is parent for generic params resolving.
                var frameDsPageDrawing = DsProject.Instance.ReadDsPageInPlay(frameDsPageDrawingFileName,
                    DsPageDrawing, PlayWindow);

                if (frameDsPageDrawing is null)
                {
                    MessageBoxHelper.ShowError(Properties.Resources.ReadFrameDrawingErrorMessage + @" " +
                                               frameDsPageDrawingFileName);
                    return;
                }

                var frameDsPageDrawingViewbox =
                    new PlayDsPageDrawingViewbox(frameDsPageDrawing, PlayWindow.MainFrame);
                frameDsPageDrawingViewbox.Background = null;
                (TreeHelper.FindChild<PlayDrawingCanvas>(frameDsPageDrawingViewbox) ??
                 throw new InvalidOperationException()).Background = null;
                FrameContentControl.Content = frameDsPageDrawingViewbox;
            }

            if (CurrentPanoramaViewport3D is null)
            {
                PreviousPanoramaViewport3D = SecondPanoramaViewport3D;
                CurrentPanoramaViewport3D = FirstPanoramaViewport3D;
            }
            else
            {
                PreviousPanoramaViewport3D = CurrentPanoramaViewport3D;
                if (ReferenceEquals(CurrentPanoramaViewport3D, FirstPanoramaViewport3D))
                    CurrentPanoramaViewport3D = SecondPanoramaViewport3D;
                else
                    CurrentPanoramaViewport3D = FirstPanoramaViewport3D;
            }

            CurrentPanoramaViewport3D.JumpAsync(panoramaJumpDsCommandOptions, DsPageDrawing);
        }

        #endregion
    }
}