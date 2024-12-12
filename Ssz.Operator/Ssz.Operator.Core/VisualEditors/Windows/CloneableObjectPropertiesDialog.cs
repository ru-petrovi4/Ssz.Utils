using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Utils.Wpf.WpfMessageBox;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public class CloneableObjectPropertiesDialog : LocationMindfulWindow
    {
        #region private fields

        private object? _originalObject;

        #endregion

        #region construction and destruction

        protected CloneableObjectPropertiesDialog() :
            base(@"CloneableObjectPropertiesDialog", 800, 900)
        {
            Icon =
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Ssz.Operator.Core;component/Resources/Images/Properties.png",
                        UriKind.RelativeOrAbsolute));

            Content = new EditorFrameControl();

            MainControl = new ObjectPropertiesControl();
        }

        #endregion

        #region public functions

        public static ICloneable? ShowDialog(ICloneable originalObject, bool askForSaveChanges = false)
        {
            var dialog = new CloneableObjectPropertiesDialog
            {
                Owner = MessageBoxHelper.GetRootWindow(),
                Title = Properties.Resources.Properties,
                AskForSaveChanges = askForSaveChanges
            };
            dialog._originalObject = originalObject;
            dialog.MainControl.SelectedObject = originalObject.Clone();

            if (dialog.ShowDialog() != true) return null;

            return (ICloneable) dialog.MainControl.SelectedObject;
        }

        #endregion

        #region protected functions

        protected bool AskForSaveChanges { get; set; }

        protected ObjectPropertiesControl MainControl
        {
            get => (ObjectPropertiesControl) ((EditorFrameControl) Content).MainContent;
            set => ((EditorFrameControl) Content).MainContent = value;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_originalObject is null || _originalObject.Equals(MainControl.SelectedObject))
            {
                DialogResult = false;
                return;
            }

            if (AskForSaveChanges)
            {
                var messageBoxResult = WpfMessageBox.Show(this, Properties.Resources.SaveChangesQuestion,
                    Properties.Resources.QuestionMessageBoxCaption,
                    WpfMessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                switch (messageBoxResult)
                {
                    case WpfMessageBoxResult.Yes:
                        DialogResult = true;
                        break;
                    case WpfMessageBoxResult.No:
                        DialogResult = false;
                        break;
                    case WpfMessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
            else
            {
                DialogResult = true;
            }
        }

        #endregion
    }
}