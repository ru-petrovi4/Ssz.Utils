using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.MultiValueConverters;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class StructConverterDialog : LocationMindfulWindow
    {
        #region private fields

        private ValueConverterBase? _resultLocalizedConverter;

        #endregion

        #region construction and destruction

        public StructConverterDialog(bool showDataSourceToUiTab, bool showUiToDataSourceTab)
            : base(@"ConverterDialog", 1024, 768)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new StructConverterControl(showDataSourceToUiTab, showUiToDataSourceTab);
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _resultLocalizedConverter =
                MainControl.LocalizedConverter;

            DialogResult = true;
        }

        #endregion

        #region public functions

        public StructConverterControl MainControl
        {
            get => (StructConverterControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        public ValueConverterBase? LocalizedConverter
        {
            get => _resultLocalizedConverter;
            set => MainControl.LocalizedConverter = value;
        }

        #endregion
    }
}