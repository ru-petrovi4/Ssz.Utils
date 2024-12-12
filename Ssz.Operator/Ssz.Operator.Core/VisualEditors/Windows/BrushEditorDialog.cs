using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class BrushEditorDialog : LocationMindfulWindow
    {
        #region private fields

        private object? _resultDsBrush;

        #endregion

        #region construction and destruction

        public BrushEditorDialog()
            : base(@"BrushEditorDialog", 1024, 600)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new BrushEditorControl();
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _resultDsBrush =
                MainControl.DsBrush;

            DialogResult = true;
        }

        #endregion

        #region public functions

        public BrushEditorControl MainControl
        {
            get => (BrushEditorControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        public object? DsBrush
        {
            get => _resultDsBrush;
            set => MainControl.DsBrush = value;
        }

        #endregion
    }
}