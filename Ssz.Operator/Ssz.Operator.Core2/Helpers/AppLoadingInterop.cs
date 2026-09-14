using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Drives the HTML loading overlay (see wwwroot/index.html and wwwroot/main.js) from the
    ///     managed startup code, so its progress bar covers downloading the project files and not
    ///     just the application itself.
    ///     <para>Every call is a no-op outside the browser, or if the JS module is not deployed.
    ///     In that case the overlay hides itself by timeout once the UI is up.</para>
    /// </summary>
    public static partial class AppLoadingInterop
    {
        #region public functions

        /// <summary>
        ///     Imports the JS module once. Safe to call repeatedly.
        /// </summary>
        public static async Task InitializeInteropAsync()
        {
            if (!OperatingSystem.IsBrowser() || _isAvailable)
                return;

            try
            {
                await JSHost.ImportAsync("AppLoadingInterop", "./appLoadingInterop.js"); // /_framework/appLoadingInterop.js
                _isAvailable = true;
            }
            catch (Exception)
            {
                // Not fatal: startup continues without the overlay updates.
            }
        }

        /// <summary>
        ///     Text shown above the progress bar.
        /// </summary>
        public static void SetStatusSafe(string status)
        {
            if (_isAvailable)
                SetStatus(status);
        }

        /// <summary>
        ///     Progress of the project files stage, 0 - 100.
        /// </summary>
        public static void SetProjectProgressSafe(uint progressPercent)
        {
            if (_isAvailable)
                SetProjectProgress((int)progressPercent);
        }

        /// <summary>
        ///     Hides the overlay, revealing the application.
        /// </summary>
        public static void HideSafe()
        {
            if (_isAvailable)
                Hide();
        }

        /// <summary>
        ///     Keeps the overlay on screen and shows the message instead of the progress bar.
        /// </summary>
        public static void ShowErrorSafe(string message)
        {
            if (_isAvailable)
                ShowError(message);
        }

        #endregion

        #region private functions

        [JSImport("setStatus", "AppLoadingInterop")]
        private static partial void SetStatus(string status);

        [JSImport("setProjectProgress", "AppLoadingInterop")]
        private static partial void SetProjectProgress(int progressPercent);

        [JSImport("hide", "AppLoadingInterop")]
        private static partial void Hide();

        [JSImport("showError", "AppLoadingInterop")]
        private static partial void ShowError(string message);

        #endregion

        #region private fields

        private static bool _isAvailable;

        #endregion
    }
}
