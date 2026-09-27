using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Ssz.Operator.Core.VisualEditors.AddonsCollectionEditor;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     The dialog the addons of a project are chosen in.
    /// </summary>
    public class AddonsCollectionEditorDialog : EditorDialogWindow
    {
        #region construction and destruction

        public AddonsCollectionEditorDialog()
            : base(@"AddonsCollectionEditorDialog", 800, 600)
        {
            MainControl = new AddonsCollectionEditorControl();
            MainControl.DesiredAdditionalAddonsInfo = DsProject.Instance.DesiredAdditionalAddonsInfo;
        }

        #endregion

        #region public functions

        public AddonsCollectionEditorControl MainControl
        {
            get => (AddonsCollectionEditorControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            DsProject.Instance.DesiredAdditionalAddonsInfo = MainControl.DesiredAdditionalAddonsInfo;
        }

        #endregion
    }

    /// <summary>
    ///     The dialog a list of objects of one kind is edited in.
    /// </summary>
    public class SameTypeCloneableObjectsListEditorDialog : EditorDialogWindow
    {
        #region construction and destruction

        public SameTypeCloneableObjectsListEditorDialog()
            : base(@"SameTypeCloneableObjectsListEditorDialog", 800, 600)
        {
            MainControl = new SameTypeCloneableObjectsListEditorControl();
        }

        #endregion

        #region public functions

        public SameTypeCloneableObjectsListEditorControl MainControl
        {
            get => (SameTypeCloneableObjectsListEditorControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        public object Collection
        {
            get => MainControl.Collection;
            set => MainControl.Collection = value;
        }

        #endregion
    }

    /// <summary>
    ///     The dialog the look of a control of a shape is chosen in.
    /// </summary>
    public class DsUIElementPropertyEditorDialog : EditorDialogWindow
    {
        #region construction and destruction

        public DsUIElementPropertyEditorDialog(Type propertyInfoSupplierType)
            : base(@"Property", 800, 600)
        {
            MainControl = new DsUIElementPropertyEditorControl(propertyInfoSupplierType);
        }

        #endregion

        #region public functions

        public DsUIElementPropertyEditorControl MainControl
        {
            get => (DsUIElementPropertyEditorControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        public DsUIElementProperty StyleInfo
        {
            get => _resultStyleInfo!;
            set => MainControl.StyleInfo = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            _resultStyleInfo = MainControl.StyleInfo;
        }

        #endregion

        #region private fields

        private DsUIElementProperty? _resultStyleInfo;

        #endregion
    }

    /// <summary>
    ///     What a toolkit operation is to be run with, asked before it starts.
    ///     <para>
    ///         Unlike the other editor dialogs this one has a Cancel: an operation that runs over the
    ///         whole project is not started by merely closing a window.
    ///     </para>
    /// </summary>
    public class ToolkitOperationOptionsDialog : EditorDialogWindow
    {
        #region construction and destruction

        protected ToolkitOperationOptionsDialog()
            : base(@"ToolkitOperationPropertiesDialog", 800, 900)
        {
            MainControl = new ToolkitOperationOptionsControl();
            MainControl.OkEvent += MainControlOnOkEvent;
            MainControl.CancelEvent += MainControlOnCancelEvent;
        }

        #endregion

        #region public functions

        /// <returns>What the author chose, or null when they did not start the operation.</returns>
        public static async Task<ICloneable?> ShowDialogAsync(ICloneable originalObject, string description)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new ToolkitOperationOptionsDialog
                {
                    Title = Res.Properties,
                    Description = description
                };
                dialog.SelectedObject = originalObject.Clone();

                Window? ownerWindow = MessageBoxHelper.GetRootWindow();
                if (ownerWindow is not null)
                    await dialog.ShowDialog(ownerWindow);
                else
                    dialog.Show();

                if (!dialog.DialogResult) return null;

                return dialog.SelectedObject as ICloneable;
            });
        }

        public ToolkitOperationOptionsControl MainControl
        {
            get => (ToolkitOperationOptionsControl) Frame.MainContent!;
            private set => Frame.MainContent = value;
        }

        public object? SelectedObject
        {
            get => MainControl.SelectedObject;
            set => MainControl.SelectedObject = value;
        }

        public string? Description
        {
            get => MainControl.Description;
            set => MainControl.Description = value;
        }

        #endregion

        #region protected functions

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            // Closing the window is not starting the operation; only the button is.
            DialogResult = _started;
        }

        #endregion

        #region private functions

        private void MainControlOnOkEvent()
        {
            _started = true;
            Close();
        }

        private void MainControlOnCancelEvent()
        {
            _started = false;
            Close();
        }

        #endregion

        #region private fields

        private bool _started;

        #endregion
    }
}
