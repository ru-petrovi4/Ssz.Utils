using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.MultiValueConverters;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class XamlConverterDialog : LocationMindfulWindow
    {
        #region private fields

        private ValueConverterBase? _resultXamlConverter;

        #endregion

        #region construction and destruction

        public XamlConverterDialog()
            : base(@"ConverterDialog", 1024, 768)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new XamlConverterControl();
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _resultXamlConverter =
                MainControl.XamlConverter;

            DialogResult = true;
        }

        #endregion

        #region public functions

        public XamlConverterControl MainControl
        {
            get => (XamlConverterControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        public ValueConverterBase? XamlConverter
        {
            get => _resultXamlConverter;
            set => MainControl.XamlConverter = value;
        }

        #endregion
    }
}