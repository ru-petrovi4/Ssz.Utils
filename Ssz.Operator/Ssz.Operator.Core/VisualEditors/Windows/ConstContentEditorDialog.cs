using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class ConstContentEditorDialog : LocationMindfulWindow
    {
        #region private fields

        private string _resultXaml = @"";

        #endregion

        #region construction and destruction

        public ConstContentEditorDialog()
            : base("ConstContentEditorDialog", 800, 600)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new ConstContentEditorControl();
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _resultXaml = MainControl.Xaml;

            DialogResult = true;
        }

        #endregion

        #region public functions

        public ConstContentEditorControl MainControl
        {
            get => (ConstContentEditorControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        public string Xaml
        {
            get => _resultXaml;
            set => MainControl.Xaml = value;
        }

        #endregion
    }
}