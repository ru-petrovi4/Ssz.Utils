using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.VisualEditors.Windows;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     What every property edited in a dialog looks like in the grid: a button showing the value,
    ///     which opens the dialog.
    ///     <para>
    ///         Ported from the WPF editors, which were all this same button with a different dialog
    ///         behind it. What changed is that an Avalonia dialog is awaited rather than blocking.
    ///     </para>
    /// </summary>
    public abstract class DialogTypeEditorBase : UserControl, ITypeEditor
    {
        #region construction and destruction

        protected DialogTypeEditorBase()
        {
            MainButton = new EditorButton();
            MainButton.Click += async (sender, e) =>
            {
                try
                {
                    await OnButtonClickAsync();
                }
                catch (Exception ex)
                {
                    // A dialog that fails leaves the property as it was, but it is worth knowing.
                    DsProject.LoggersSet.Logger.LogError(ex, @"The editor of a property failed.");
                }
            };

            Content = MainButton;
        }

        #endregion

        #region public functions

        public virtual Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            Bind(IsEnabledProperty, new Binding(nameof(PropertyItemViewModel.IsReadOnly))
            {
                Source = propertyItem,
                Converter = NotConverter
            });

            MainButton.Bind(ContentProperty, new Binding(nameof(PropertyItemViewModel.Value))
            {
                Source = propertyItem,
                Converter = ValueConverter
            });

            return this;
        }

        #endregion

        #region protected functions

        protected EditorButton MainButton { get; }

        protected PropertyItemViewModel PropertyItem => (PropertyItemViewModel) DataContext!;

        /// <summary>
        ///     What the button shows for the value. Text by default; the editors of brushes and of
        ///     content answer with something to look at.
        /// </summary>
        protected virtual IValueConverter ValueConverter => ValueConverters.ObjectToTextConverter.Instance;

        protected abstract Task OnButtonClickAsync();

        /// <summary>
        ///     The window a dialog is opened over, or null when the editor is not on screen.
        /// </summary>
        protected Window? OwnerWindow => TopLevel.GetTopLevel(this) as Window;

        /// <summary>
        ///     Opens a dialog over the window this editor sits in, and waits for it.
        /// </summary>
        protected async Task<bool> ShowDialogAsync(EditorDialogWindow dialog)
        {
            Window? ownerWindow = OwnerWindow;
            if (ownerWindow is null) return false;

            // The WPF dialogs carried no title of their own; naming the property is more use than
            // the word Window that Avalonia would otherwise put there.
            dialog.Title = PropertyItem.DisplayName;

            await dialog.ShowDialog(ownerWindow);

            return dialog.DialogResult;
        }

        #endregion

        #region private fields

        private static readonly IValueConverter NotConverter =
            new FuncValueConverter<bool, bool>(value => !value);

        #endregion
    }
}
