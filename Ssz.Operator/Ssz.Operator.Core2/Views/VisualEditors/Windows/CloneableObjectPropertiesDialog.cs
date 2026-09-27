using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     The properties of one object, edited on a copy of it so that the original is only replaced
    ///     when the author accepts.
    ///     <para>
    ///         Ported from the WPF editor. Closing accepts, as it did there; the question about saving
    ///         is asked only where the caller asked for it.
    ///     </para>
    /// </summary>
    public class CloneableObjectPropertiesDialog : EditorDialogWindow
    {
        #region construction and destruction

        protected CloneableObjectPropertiesDialog()
            : base(@"CloneableObjectPropertiesDialog", 800, 900)
        {
            MainControl = new ObjectPropertiesControl();
        }

        #endregion

        #region public functions

        /// <returns>What the author made of it, or null when nothing changed or it was not accepted.</returns>
        public static async Task<ICloneable?> ShowDialogAsync(Window? ownerWindow, ICloneable originalObject,
            bool askForSaveChanges = false)
        {
            var dialog = new CloneableObjectPropertiesDialog
            {
                Title = Res.Properties,
                AskForSaveChanges = askForSaveChanges
            };
            dialog._originalObject = originalObject;
            dialog.MainControl.SelectedObject = originalObject.Clone();

            if (ownerWindow is not null)
                await dialog.ShowDialog(ownerWindow);
            else
                dialog.Show();

            if (!dialog.DialogResult) return null;

            return dialog.MainControl.SelectedObject as ICloneable;
        }

        #endregion

        #region protected functions

        protected bool AskForSaveChanges { get; set; }

        protected ObjectPropertiesControl MainControl
        {
            get => (ObjectPropertiesControl) Frame.MainContent!;
            set => Frame.MainContent = value;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (_originalObject is null || _originalObject.Equals(MainControl.SelectedObject))
            {
                DialogResult = false;
                return;
            }

            if (!AskForSaveChanges || _answered)
            {
                if (!_answered) DialogResult = true;
                return;
            }

            // The question cannot be asked while the window is closing, so the closing is held back
            // and taken up again once the author has answered. Cancel leaves the window open, which
            // is what the WPF dialog did by cancelling the close.
            e.Cancel = true;
            _ = AskThenCloseAsync();
        }

        #endregion

        #region private functions

        private async Task AskThenCloseAsync()
        {
            bool? answer = await MessageBoxHelper.AskYesNoCancelAsync(Res.SaveChangesQuestion);
            if (answer is null) return;

            DialogResult = answer.Value;
            _answered = true;
            Close();
        }

        #endregion

        #region private fields

        private object? _originalObject;
        private bool _answered;

        #endregion
    }
}
