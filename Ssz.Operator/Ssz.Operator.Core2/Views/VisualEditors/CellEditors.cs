using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     What one rule of a converter is edited with, in the cell of its row: a button showing the
    ///     value, which opens the same dialog the property grid would.
    /// </summary>
    public abstract class StatementCellEditor : UserControl
    {
        #region construction and destruction

        protected StatementCellEditor()
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
                    DsProject.LoggersSet.Logger.LogError(ex, @"The editor of a converter rule failed.");
                }
            };

            Content = MainButton;
        }

        #endregion

        #region protected functions

        protected EditorButton MainButton { get; }

        protected StatementViewModel Statement => (StatementViewModel) DataContext!;

        protected abstract System.Threading.Tasks.Task OnButtonClickAsync();

        protected async System.Threading.Tasks.Task<bool> ShowDialogAsync(EditorDialogWindow dialog)
        {
            if (TopLevel.GetTopLevel(this) is not Window ownerWindow) return false;

            await dialog.ShowDialog(ownerWindow);

            return dialog.DialogResult;
        }

        #endregion
    }

    /// <summary>
    ///     The content one rule of a XAML converter puts on the shape.
    /// </summary>
    public class XamlEditor : StatementCellEditor
    {
        #region construction and destruction

        public XamlEditor()
        {
            Height = 64;

            MainButton.Bind(ContentProperty, new Binding(nameof(StatementViewModel.ConstXaml))
            {
                Converter = XamlToContentConverter.Instance
            });
        }

        #endregion

        #region protected functions

        protected override async System.Threading.Tasks.Task OnButtonClickAsync()
        {
            var dialog = new ConstContentEditorDialog
            {
                Title = Properties.Resources.ConverterWindowDataSourceToUiValueExpression,
                Xaml = Statement.ConstXaml?.Xaml ?? @""
            };

            if (await ShowDialogAsync(dialog)) Statement.ConstXaml = new DsXaml { Xaml = dialog.Xaml };
        }

        #endregion
    }

    /// <summary>
    ///     The brush one rule of a brush converter paints the shape with.
    /// </summary>
    public class BrushEditor : StatementCellEditor
    {
        #region construction and destruction

        public BrushEditor()
        {
            MainButton.Bind(ContentProperty, new Binding(nameof(StatementViewModel.ConstDsBrushOrParamNum))
            {
                Converter = DsBrushToContentConverter.Instance
            });
        }

        #endregion

        #region protected functions

        protected override async System.Threading.Tasks.Task OnButtonClickAsync()
        {
            var dialog = new BrushEditorDialog
            {
                Title = Properties.Resources.ConverterWindowDataSourceToUiValueExpression,
                DsBrush = Statement.ConstDsBrushOrParamNum
            };

            if (await ShowDialogAsync(dialog)) Statement.ConstDsBrushOrParamNum = dialog.DsBrush;
        }

        #endregion
    }
}
