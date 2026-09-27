namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     The dialog a brush is chosen in.
    /// </summary>
    public class BrushEditorDialog : EditorDialogWindow
    {
        #region construction and destruction

        public BrushEditorDialog()
            : base(@"BrushEditorDialog", 1024, 600)
        {
            MainControl = new BrushEditorControl();
        }

        #endregion

        #region public functions

        public BrushEditorControl MainControl
        {
            get => (BrushEditorControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        /// <summary>
        ///     What the author chose. It is read after the dialog closes, which is when the tab that is
        ///     open decides the kind of brush.
        /// </summary>
        public object? DsBrush
        {
            get => _resultDsBrush;
            set => MainControl.DsBrush = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            _resultDsBrush = MainControl.DsBrush;
        }

        #endregion

        #region private fields

        private object? _resultDsBrush;

        #endregion
    }
}
