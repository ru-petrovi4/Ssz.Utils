using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Utils;
using Ssz.Utils.Wpf;
using static Ssz.Operator.Core.ControlsPlay.PlayControlWrapper;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.ControlsPlay.GenericPlay
{
    public partial class GenericGraphicPlayControl : PlayControlBase
    {
        #region construction and destruction

        public GenericGraphicPlayControl(IPlayWindow playWindow) :
            base(playWindow)
        {
            InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
                RootPlayDsPageDrawingViewbox?.Dispose();

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public override async void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {            
            var genericGraphicDsPageType =
                (GenericGraphicDsPageType) (dsPageDrawingInfo.DsPageTypeObject!);
            var genericEmulationAddon = DsProject.Instance.GetAddon<GenericEmulationAddon>();            

            string framesDsPageFileRelativePath;
            if (jumpInfo.JumpContext is string jumpContextString)
            {
                framesDsPageFileRelativePath = jumpContextString;
            }
            else
            {
                framesDsPageFileRelativePath = genericGraphicDsPageType.FramesDsPageFileRelativePath;
                if (string.IsNullOrEmpty(framesDsPageFileRelativePath))
                    framesDsPageFileRelativePath = genericEmulationAddon.FramesDsPageFileRelativePath;
                if (string.IsNullOrEmpty(framesDsPageFileRelativePath) && RootPlayDsPageDrawingViewbox is not null)
                {
                    var frameDsShapeView =
                        TreeHelper.FindChild<FrameDsShapeView>(
                            RootPlayDsPageDrawingViewbox,
                            fsv => string.IsNullOrEmpty(fsv.FrameName) && fsv.Visibility == Visibility.Visible && fsv.IsEnabled);
                    if (frameDsShapeView is not null)
                        framesDsPageFileRelativePath = DsProject.Instance.GetFileRelativePath(RootPlayDsPageDrawingViewbox.PlayDrawingViewModel.Drawing.FileFullName);
                }

                jumpInfo.JumpContext = framesDsPageFileRelativePath ?? "";
            }

            PlayDsPageDrawingViewbox? rootPlayDsPageDrawingViewbox = null;            

            if (!String.IsNullOrEmpty(framesDsPageFileRelativePath))
            {
                if (!String.Equals(framesDsPageFileRelativePath, DsProject.Instance.GetFileRelativePath(DsPageDrawing?.FileFullName), StringComparison.InvariantCultureIgnoreCase))
                {
                    DsPageDrawing = DsProject.Instance.ReadDsPageInPlay(
                        framesDsPageFileRelativePath,
                        dsPageDrawingInfo,
                        PlayWindow);
                    if (DsPageDrawing is not null)
                    {
                        FrameDsShape? mainFrameDsShape = null;

                        foreach (FrameDsShape frameDsShape in DsPageDrawing.FindDsShapes<FrameDsShape>())
                        {
                            if (String.IsNullOrEmpty(frameDsShape.FrameName))
                            {
                                mainFrameDsShape = frameDsShape;
                                mainFrameDsShape.StartDsPageFileRelativePath = jumpInfo.FileRelativePath;
                            }
                            else
                            {
                                var frameInitializationInfo = genericGraphicDsPageType.FrameInitializationInfosCollection
                                    .FirstOrDefault(fii => String.Equals(fii.FrameName, frameDsShape.FrameName, StringComparison.InvariantCultureIgnoreCase));
                                if (frameInitializationInfo is not null)
                                    frameDsShape.StartDsPageFileRelativePath = frameInitializationInfo.StartDsPageFileRelativePath;
                            }
                        }

                        if (mainFrameDsShape is null)
                        {
                            MessageBoxHelper.ShowError(Properties.Resources
                                .RootPlayDsPageDrawingViewbox_MainFrame_ErrorMessage);
                        }

                        Mouse.OverrideCursor = Cursors.Wait;
                        await Task.Delay(1);
                        MainBusyIndicator.IsBusy = true;

                        rootPlayDsPageDrawingViewbox = new PlayDsPageDrawingViewbox(DsPageDrawing, PlayWindow.MainFrame);
                        rootPlayDsPageDrawingViewbox.Loaded +=
                            (sender, args) =>
                            {
                                MainBusyIndicator.IsBusy = false;
                                Mouse.OverrideCursor = null;
                            };
                    }
                }
                else
                {
                    rootPlayDsPageDrawingViewbox = RootPlayDsPageDrawingViewbox!;

                    FrameDsShapeView? mainFrameDsShapeView = null;

                    foreach (FrameDsShapeView frameDsShapeView in TreeHelper.FindChilds<FrameDsShapeView>(
                            rootPlayDsPageDrawingViewbox,
                            fsv => fsv.Visibility == Visibility.Visible && fsv.IsEnabled))
                    {
                        if (String.IsNullOrEmpty(frameDsShapeView.FrameName))
                        {
                            mainFrameDsShapeView = frameDsShapeView;
                            frameDsShapeView.CurrentJumpInfo = new JumpInfo(
                                jumpInfo.FileRelativePath,
                                jumpInfo.SenderContainerCopy);                            
                        }
                        else
                        {
                            var frameInitializationInfo = genericGraphicDsPageType.FrameInitializationInfosCollection
                                .FirstOrDefault(fii => String.Equals(fii.FrameName, frameDsShapeView.FrameName, StringComparison.InvariantCultureIgnoreCase));
                            if (frameInitializationInfo is not null)
                            {
                                frameDsShapeView.CurrentJumpInfo = new JumpInfo(
                                    frameInitializationInfo.StartDsPageFileRelativePath,
                                    PlayDsProjectView.GetGenericContainerCopy(frameDsShapeView.DsShapeViewModel.DsShape.ParentItem));
                            }
                        }
                    }

                    if (mainFrameDsShapeView is null)
                    {
                        MessageBoxHelper.ShowError(Properties.Resources
                            .RootPlayDsPageDrawingViewbox_MainFrame_ErrorMessage);
                    }

                    rootPlayDsPageDrawingViewbox.DsShapeViewsReInitialize();
                }
            }            

            if (rootPlayDsPageDrawingViewbox is null)
            {
                DsPageDrawing = DsProject.Instance.ReadDsPageInPlay(
                    jumpInfo.FileRelativePath,
                    jumpInfo.SenderContainerCopy,
                    PlayWindow);                
                if (DsPageDrawing is not null)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await Task.Delay(1);
                    MainBusyIndicator.IsBusy = true;

                    rootPlayDsPageDrawingViewbox = new PlayDsPageDrawingViewbox(DsPageDrawing, PlayWindow.MainFrame);
                    rootPlayDsPageDrawingViewbox.Loaded +=
                        (sender, args) =>
                        {
                            MainBusyIndicator.IsBusy = false;
                            Mouse.OverrideCursor = null;
                        };
                }
            }            

            if (!ReferenceEquals(RootPlayDsPageDrawingViewbox, rootPlayDsPageDrawingViewbox))
            {
                RootPlayDsPageDrawingViewbox?.Dispose();
                RootPlayDsPageDrawingViewbox = rootPlayDsPageDrawingViewbox;
            }            
        }

        public override bool PrepareWindow(IPlayWindow newWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            if (DsPageDrawing is null) 
                return true;
            if (!showWindowDsCommandOptions.ContentWidth.HasValue)
                showWindowDsCommandOptions.ContentWidth = DsPageDrawing.Width;
            if (!showWindowDsCommandOptions.ContentHeight.HasValue)
                showWindowDsCommandOptions.ContentHeight = DsPageDrawing.Height;

            return false;
        }

        public override bool PrepareChildWindow(IPlayWindow newChildWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            return false;
        }

        #endregion

        #region protected functions

        protected PlayDsPageDrawingViewbox? RootPlayDsPageDrawingViewbox
        {
            get => MainBusyIndicator.Content as PlayDsPageDrawingViewbox;
            set => MainBusyIndicator.Content = value;
        }

        #endregion
    }
}