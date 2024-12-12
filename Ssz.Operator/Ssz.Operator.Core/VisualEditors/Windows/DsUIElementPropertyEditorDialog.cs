using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class DsUIElementPropertyEditorDialog : LocationMindfulWindow
    {
        #region private fields

        private DsUIElementProperty _resultStyleInfo = null!;

        #endregion

        #region construction and destruction

        public DsUIElementPropertyEditorDialog(Type propertyInfoSupplierType)
            : base(@"Property", 800, 600)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new DsUIElementPropertyEditorControl(propertyInfoSupplierType);
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _resultStyleInfo =
                MainControl.StyleInfo;

            DialogResult = true;
        }

        #endregion

        #region public functions

        public DsUIElementPropertyEditorControl MainControl
        {
            get => (DsUIElementPropertyEditorControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        public DsUIElementProperty StyleInfo
        {
            get => _resultStyleInfo;
            set => MainControl.StyleInfo = value;
        }

        #endregion
    }
}