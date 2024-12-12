using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.MultiValueConverters;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class BrushConverterDialog : LocationMindfulWindow
    {
        #region private fields

        private ValueConverterBase? _resultDsBrushConverter;

        #endregion

        #region construction and destruction

        public BrushConverterDialog()
            : base(@"ConverterDialog", 1024, 768)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new BrushConverterControl();
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _resultDsBrushConverter =
                MainControl.DsBrushConverter;

            DialogResult = true;
        }

        #endregion

        #region public functions

        public BrushConverterControl MainControl
        {
            get => (BrushConverterControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        public ValueConverterBase? DsBrushConverter
        {
            get => _resultDsBrushConverter;
            set => MainControl.DsBrushConverter = value;
        }

        #endregion
    }
}