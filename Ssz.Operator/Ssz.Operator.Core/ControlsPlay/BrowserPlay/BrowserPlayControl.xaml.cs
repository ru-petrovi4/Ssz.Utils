using System;
using System.Windows.Navigation;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Drawings;

namespace Ssz.Operator.Core.ControlsPlay.BrowserPlay
{
    public partial class BrowserPlayControl : PlayControlBase
    {
        #region private functions

        private void MainWebBrowserOnNavigating(object? sender, NavigatingCancelEventArgs navigatingCancelEventArgs)
        {
            var cancelNavigation = UrlHelper.WebBrowserOnNavigating(navigatingCancelEventArgs.Uri.OriginalString,
                DsProject.Instance, (DataValueViewModel) DataContext);
            if (cancelNavigation) navigatingCancelEventArgs.Cancel = true;
        }

        #endregion

        #region construction and destruction

        public BrowserPlayControl(IPlayWindow playWindow) :
            base(playWindow)
        {
            InitializeComponent();

            MainWebBrowser.Navigating += MainWebBrowserOnNavigating;
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
                // Release and Dispose managed resources.
                MainWebBrowser.Navigating -= MainWebBrowserOnNavigating;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public override void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {
            throw new NotImplementedException();
        }

        public override bool Jump(JumpInfo jumpInfo)
        {
            string fileFullName = DsProject.Instance.GetFileFullName(jumpInfo.FileRelativePath);

            MainWebBrowser.Source = new Uri(fileFullName, UriKind.Absolute);

            return true;
        }

        public override bool PrepareWindow(IPlayWindow newWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            if (newWindow.IsRootWindow)
                if (showWindowDsCommandOptions.WindowFullScreen == DefaultFalseTrue.Default)
                    showWindowDsCommandOptions.WindowFullScreen = DefaultFalseTrue.True;

            return false;
        }

        #endregion
    }
}