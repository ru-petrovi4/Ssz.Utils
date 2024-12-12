using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class SameTypeCloneableObjectsListEditorDialog : LocationMindfulWindow
    {
        #region construction and destruction

        public SameTypeCloneableObjectsListEditorDialog()
            : base(@"SameTypeCloneableObjectsListEditorDialog", 800, 600)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new SameTypeCloneableObjectsListEditorControl();
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            DialogResult = true;
        }

        #endregion

        #region public functions

        public SameTypeCloneableObjectsListEditorControl MainControl
        {
            get => (SameTypeCloneableObjectsListEditorControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        public object Collection
        {
            get => MainControl.Collection;
            set => MainControl.Collection = value;
        }

        #endregion
    }
}