using Avalonia.Controls;
using Ssz.Operator.Core.MultiValueConverters;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     The dialog the rules of a value converter are written in.
    /// </summary>
    public class StructConverterDialog : EditorDialogWindow
    {
        #region construction and destruction

        public StructConverterDialog(bool showDataSourceToUiTab, bool showUiToDataSourceTab)
            : base(@"ConverterDialog", 1024, 768)
        {
            MainControl = new StructConverterControl(showDataSourceToUiTab, showUiToDataSourceTab);
        }

        #endregion

        #region public functions

        public StructConverterControl MainControl
        {
            get => (StructConverterControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        public ValueConverterBase? LocalizedConverter
        {
            get => _resultLocalizedConverter;
            set => MainControl.LocalizedConverter = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            _resultLocalizedConverter = MainControl.LocalizedConverter;
        }

        #endregion

        #region private fields

        private ValueConverterBase? _resultLocalizedConverter;

        #endregion
    }

    /// <summary>
    ///     The dialog the rules that choose a piece of content are written in.
    /// </summary>
    public class XamlConverterDialog : EditorDialogWindow
    {
        #region construction and destruction

        public XamlConverterDialog()
            : base(@"XamlConverterDialog", 1024, 768)
        {
            MainControl = new XamlConverterControl();
        }

        #endregion

        #region public functions

        public XamlConverterControl MainControl
        {
            get => (XamlConverterControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        public ValueConverterBase? XamlConverter
        {
            get => _resultXamlConverter;
            set => MainControl.XamlConverter = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            _resultXamlConverter = MainControl.XamlConverter;
        }

        #endregion

        #region private fields

        private ValueConverterBase? _resultXamlConverter;

        #endregion
    }

    /// <summary>
    ///     The dialog the rules that choose a brush are written in.
    /// </summary>
    public class BrushConverterDialog : EditorDialogWindow
    {
        #region construction and destruction

        public BrushConverterDialog()
            : base(@"BrushConverterDialog", 1024, 768)
        {
            MainControl = new BrushConverterControl();
        }

        #endregion

        #region public functions

        public BrushConverterControl MainControl
        {
            get => (BrushConverterControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        public ValueConverterBase? DsBrushConverter
        {
            get => _resultDsBrushConverter;
            set => MainControl.DsBrushConverter = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            _resultDsBrushConverter = MainControl.DsBrushConverter;
        }

        #endregion

        #region private fields

        private ValueConverterBase? _resultDsBrushConverter;

        #endregion
    }
}
