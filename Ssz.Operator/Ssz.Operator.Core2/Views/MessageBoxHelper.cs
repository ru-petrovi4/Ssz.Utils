using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
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

        /// <summary>
        ///     Says what went wrong and waits until it has been read, for where what follows - ending
        ///     the application, say - would otherwise take the message off the screen.
        /// </summary>
        public static async Task ShowErrorAsync(string messageBoxText)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                Resources.ErrorMessageBoxCaption,
                messageBoxText,
                ButtonEnum.Ok,
                Icon.Error);
            await box.ShowAsync();
        }

        /// <summary>
        ///     Asks the author a yes-or-no question and waits for the answer.
        ///     <para>
        ///         The WPF editor used WpfMessageBox.Show, which blocked until the author clicked.
        ///         Avalonia has no blocking dialog, so callers await this one.
        ///     </para>
        /// </summary>
        public static async Task<bool> AskYesNoAsync(string messageBoxText)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                Resources.QuestionMessageBoxCaption,
                messageBoxText,
                ButtonEnum.YesNo,
                Icon.Question);
            ButtonResult result = await box.ShowAsync();
            return result == ButtonResult.Yes;
        }

        /// <returns>
        ///     True for yes, false for no, and null when the author cancelled - which is what the
        ///     editor needs when it asks whether to save before closing.
        /// </returns>
        public static async Task<bool?> AskYesNoCancelAsync(string messageBoxText)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                Resources.QuestionMessageBoxCaption,
                messageBoxText,
                ButtonEnum.YesNoCancel,
                Icon.Question);
            ButtonResult result = await box.ShowAsync();
            switch (result)
            {
                case ButtonResult.Yes:
                    return true;
                case ButtonResult.No:
                    return false;
                default:
                    return null;
            }
        }

        /// <summary>
        ///     Asks whether to save what was changed. When several drawings are being closed the author
        ///     is also offered to answer once for all of them.
        ///     <para>
        ///         The WPF editor had a message box with these very buttons built in; here they are
        ///         spelled out.
        ///     </para>
        /// </summary>
        public static async Task<SaveQuestionResult> AskSaveAsync(string messageBoxText, bool withForAll)
        {
            var buttons = new List<ButtonDefinition>
            {
                new() { Name = OperatorUIResources.MessageBox_Yes },
                new() { Name = OperatorUIResources.MessageBox_No }
            };
            if (withForAll)
            {
                buttons.Add(new ButtonDefinition { Name = OperatorUIResources.MessageBox_YesForAll });
                buttons.Add(new ButtonDefinition { Name = OperatorUIResources.MessageBox_NoForAll });
            }
            buttons.Add(new ButtonDefinition { Name = OperatorUIResources.MessageBox_Cancel, IsCancel = true });

            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ContentTitle = Resources.QuestionMessageBoxCaption,
                ContentMessage = messageBoxText,
                ButtonDefinitions = buttons,
                Icon = Icon.Question
            });

            var result = await box.ShowAsync();

            if (result == OperatorUIResources.MessageBox_Yes) return SaveQuestionResult.Yes;
            if (result == OperatorUIResources.MessageBox_No) return SaveQuestionResult.No;
            if (result == OperatorUIResources.MessageBox_YesForAll) return SaveQuestionResult.YesForAll;
            if (result == OperatorUIResources.MessageBox_NoForAll) return SaveQuestionResult.NoForAll;
            return SaveQuestionResult.Cancel;
        }

        #endregion
    }

    public enum SaveQuestionResult
    {
        Yes,
        No,
        YesForAll,
        NoForAll,
        Cancel
    }
}