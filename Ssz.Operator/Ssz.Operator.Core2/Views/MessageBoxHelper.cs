using Avalonia;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core
{
    public static class MessageBoxHelper
    {
        #region public functions

        public static Window? GetRootWindow()
        {
            return PlayDsProjectView.LastActiveRootPlayWindow as Window;
        }

        public async static void ShowInfo(string messageBoxText)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                Resources.InfoMessageBoxCaption,
                messageBoxText,
                ButtonEnum.Ok,
                Icon.Info);
            var result = await box.ShowAsync();
        }

        public async static void ShowWarning(string messageBoxText)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                Resources.WarningMessageBoxCaption,
                messageBoxText,
                ButtonEnum.Ok,
                Icon.Warning);
            var result = await box.ShowAsync();
        }

        public async static void ShowError(string messageBoxText)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                Resources.ErrorMessageBoxCaption,
                messageBoxText,
                ButtonEnum.Ok,
                Icon.Error);
            var result = await box.ShowAsync();
        }

        #endregion
    }
}