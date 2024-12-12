using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class DsFontEditorDialog : LocationMindfulWindow
    {
        #region private fields

        private DsFont? _resultDsFont;

        #endregion

        #region construction and destruction

        public DsFontEditorDialog()
            : base(@"DsFontEditorDialog", 800, 600)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new DsFontEditorControl();
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _resultDsFont =
                MainControl.DsFont;

            DialogResult = true;
        }

        #endregion

        #region public functions

        public DsFontEditorControl MainControl
        {
            get => (DsFontEditorControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }


        public DsFont? DsFont
        {
            get => _resultDsFont;
            set => MainControl.DsFont = value;
        }

        #endregion
    }
}