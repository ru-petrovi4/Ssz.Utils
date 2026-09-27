using Avalonia;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     The dialog the content of a shape is chosen in.
    /// </summary>
    public class ConstContentEditorDialog : EditorDialogWindow
    {
        #region construction and destruction

        public ConstContentEditorDialog()
            : base(@"ConstContentEditorDialog", 1024, 700)
        {
            MainControl = new ConstContentEditorControl();
        }

        #endregion

        #region public functions

        public ConstContentEditorControl MainControl
        {
            get => (ConstContentEditorControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        public string Xaml
        {
            get => _resultXaml;
            set => MainControl.Xaml = value;
        }

        /// <summary>
        ///     How big the content is in itself, when it says so. A shape that has just been given a
        ///     picture takes its size from this.
        /// </summary>
        public Size? ContentOriginalSize { get; private set; }

        #endregion

        #region protected functions

        protected override void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            _resultXaml = MainControl.Xaml;
            ContentOriginalSize = MainControl.ContentOriginalSize;
        }

        #endregion

        #region private fields

        private string _resultXaml = @"";

        #endregion
    }
}
