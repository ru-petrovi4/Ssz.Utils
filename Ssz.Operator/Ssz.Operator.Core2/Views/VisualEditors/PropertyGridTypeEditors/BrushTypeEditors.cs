using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     A brush of any kind: one colour, a gradient, blinking, or a number of the parameter that
    ///     carries it.
    /// </summary>
    public class BrushTypeEditor : DialogTypeEditorBase
    {
        #region protected functions

        protected override IValueConverter ValueConverter => DsBrushToContentConverter.Instance;

        protected override async Task OnButtonClickAsync()
        {
            var dialog = new BrushEditorDialog
            {
                DsBrush = PropertyItem.Value as DsBrushBase
            };

            dialog.MainControl.ParamNumBrushTabItem.IsVisible = false;

            if (await ShowDialogAsync(dialog)) PropertyItem.Value = dialog.DsBrush;
        }

        #endregion
    }

    /// <summary>
    ///     A brush that may only be one colour.
    /// </summary>
    public class SolidBrushTypeEditor : DialogTypeEditorBase
    {
        #region protected functions

        protected override IValueConverter ValueConverter => DsBrushToContentConverter.Instance;

        protected override async Task OnButtonClickAsync()
        {
            var dialog = new BrushEditorDialog
            {
                DsBrush = PropertyItem.Value as DsBrushBase
            };

            dialog.MainControl.GradientBrushTabItem.IsVisible = false;
            dialog.MainControl.BlinkingBrushTabItem.IsVisible = false;
            dialog.MainControl.ParamNumBrushTabItem.IsVisible = false;
            dialog.MainControl.DefaultBrushTabItem.IsVisible = false;

            if (await ShowDialogAsync(dialog)) PropertyItem.Value = dialog.DsBrush;
        }

        #endregion
    }

    /// <summary>
    ///     A brush that may be one colour or nothing at all.
    /// </summary>
    public class SolidBrushOrNullTypeEditor : DialogTypeEditorBase
    {
        #region protected functions

        protected override IValueConverter ValueConverter => DsBrushToContentConverter.Instance;

        protected override async Task OnButtonClickAsync()
        {
            var dialog = new BrushEditorDialog
            {
                DsBrush = PropertyItem.Value as DsBrushBase
            };

            dialog.MainControl.GradientBrushTabItem.IsVisible = false;
            dialog.MainControl.BlinkingBrushTabItem.IsVisible = false;
            dialog.MainControl.ParamNumBrushTabItem.IsVisible = false;

            if (await ShowDialogAsync(dialog)) PropertyItem.Value = dialog.DsBrush;
        }

        #endregion
    }
}
