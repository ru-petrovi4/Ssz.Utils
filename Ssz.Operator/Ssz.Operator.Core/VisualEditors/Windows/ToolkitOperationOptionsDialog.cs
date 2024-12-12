using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class ToolkitOperationOptionsDialog : LocationMindfulWindow
    {
        #region construction and destruction

        protected ToolkitOperationOptionsDialog() :
            base(@"ToolkitOperationPropertiesDialog", 800, 900)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new ToolkitOperationOptionsControl();

            MainControl.OkEvent += MainControlOnOkEvent;
        }

        #endregion

        #region private functions

        private void MainControlOnOkEvent()
        {
            DialogResult = true;

            Close();
        }

        #endregion

        #region public functions

        public static ICloneable? ShowDialog(ICloneable originalObject, string description)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new ToolkitOperationOptionsDialog
                {
                    Owner = MessageBoxHelper.GetRootWindow(),
                    Title = Properties.Resources.Properties
                };
                dialog.Description = description;
                dialog.SelectedObject = originalObject.Clone();

                if (dialog.ShowDialog() != true) return null;

                return (ICloneable) dialog.SelectedObject;
            });
        }

        public ToolkitOperationOptionsControl MainControl
        {
            get => (ToolkitOperationOptionsControl) ((EditorFrameControl) Content).MainContent;
            private set => ((EditorFrameControl) Content).MainContent = value;
        }

        public object? SelectedObject
        {
            get => MainControl.SelectedObject;
            set => MainControl.SelectedObject = value;
        }

        public string Description
        {
            get => MainControl.Description;
            set => MainControl.Description = value;
        }

        #endregion
    }
}