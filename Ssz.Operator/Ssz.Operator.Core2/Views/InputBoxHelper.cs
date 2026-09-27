using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Asks the author for one line of text.
    ///     <para>
    ///         The WPF editor used Microsoft.VisualBasic's InputBox, which is Windows only and blocks.
    ///         This is the message box the rest of the editor already uses, with its input field turned
    ///         on, so it works on the desktop and in the browser alike - and it is awaited rather than
    ///         blocking.
    ///     </para>
    /// </summary>
    public static class InputBoxHelper
    {
        #region public functions

        /// <returns>
        ///     What was typed, or null when the dialog was dismissed - which is how a caller tells
        ///     "cancelled" from "cleared".
        /// </returns>
        public static async Task<string?> ShowAsync(string prompt, string title, string defaultValue = @"")
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = title,
                ContentMessage = prompt,
                ButtonDefinitions = ButtonEnum.OkCancel,
                Icon = Icon.None,
                InputParams = new InputParams
                {
                    DefaultValue = defaultValue
                }
            });

            ButtonResult result = await box.ShowAsync();
            if (result != ButtonResult.Ok)
                return null;

            return box.InputValue ?? @"";
        }

        #endregion
    }
}
