namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     The dialog a font is chosen in.
    /// </summary>
    public class DsFontEditorDialog : EditorDialogWindow
    {
        #region construction and destruction

        public DsFontEditorDialog()
            : base(@"DsFontEditorDialog", 800, 500)
        {
            MainControl = new DsFontEditorControl();
        }

        #endregion

        #region public functions

        public DsFontEditorControl MainControl
        {
            get => (DsFontEditorControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        public DsFont? DsFont
        {
            get => _resultDsFont;
            set => MainControl.DsFont = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            _resultDsFont = MainControl.DsFont;
        }

        #endregion

        #region private fields

        private DsFont? _resultDsFont;

        #endregion
    }
}
