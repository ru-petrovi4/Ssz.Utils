using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     The clipboard, reached the way Avalonia offers it.
    ///     <para>
    ///         WPF had a static Clipboard that could be used from anywhere and answered at once. In
    ///         Avalonia the clipboard belongs to a top level and every call is awaited, so these methods
    ///         find the top level of the application and return tasks.
    ///     </para>
    /// </summary>
    public static class ClipboardHelper
    {
        #region public functions

        public static async Task SetTextAsync(string? text)
        {
            IClipboard? clipboard = GetClipboard();
            if (clipboard is null) return;

            await clipboard.SetTextAsync(text ?? @"");
        }

        public static async Task<string?> GetTextAsync()
        {
            IClipboard? clipboard = GetClipboard();
            if (clipboard is null) return null;

            return await clipboard.TryGetTextAsync();
        }

        public static TopLevel? GetTopLevel()
        {
            switch (Application.Current?.ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    return desktop.MainWindow;
                case ISingleViewApplicationLifetime singleViewPlatform:
                    return singleViewPlatform.MainView is null
                        ? null
                        : TopLevel.GetTopLevel(singleViewPlatform.MainView);
                default:
                    return null;
            }
        }

        #endregion

        #region private functions

        private static IClipboard? GetClipboard()
        {
            return GetTopLevel()?.Clipboard;
        }

        #endregion
    }
}
