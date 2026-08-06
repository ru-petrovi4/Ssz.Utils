using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Egorozh.ColorPicker.Dialog;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public static class ColorDialogHelper
    {
        #region public functions

        /// <summary>
        ///     Returns null, if user cancelled the dialog or there is no window to own it.
        /// </summary>
        public static async Task<Color?> ShowAsync(Window? owner, Color initialColor)
        {
            owner ??= MessageBoxHelper.GetRootWindow();
            if (owner is null)
                return null;

            var dialog = new ColorPickerDialog
            {
                Color = initialColor
            };

            bool result = await dialog.ShowDialog<bool>(owner);
            if (!result)
                return null;

            return dialog.Color;
        }

        #endregion
    }
}
