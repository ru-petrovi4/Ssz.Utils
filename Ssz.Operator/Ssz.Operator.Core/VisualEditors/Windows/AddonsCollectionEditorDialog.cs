using System.ComponentModel;
using Ssz.Operator.Core.VisualEditors.AddonsCollectionEditor;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class AddonsCollectionEditorDialog : LocationMindfulWindow
    {
        #region construction and destruction

        public AddonsCollectionEditorDialog() :
            base(@"AddonsCollectionEditorDialog", 800, 600)
        {
            Content = new EditorFrameControl();

            MainControl = new AddonsCollectionEditorControl();

            MainControl.DesiredAdditionalAddonsInfo = DsProject.Instance.DesiredAdditionalAddonsInfo;
        }

        #endregion

        #region public functions

        public AddonsCollectionEditorControl MainControl
        {
            get => (AddonsCollectionEditorControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            DsProject.Instance.DesiredAdditionalAddonsInfo = MainControl.DesiredAdditionalAddonsInfo;

            DialogResult = true;
        }

        #endregion
    }
}