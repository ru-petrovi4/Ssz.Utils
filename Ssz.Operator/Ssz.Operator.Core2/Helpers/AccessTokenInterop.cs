using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     The access token of the user, supplied by the page the application is embedded into
    ///     (see wwwroot/_framework/accessTokenInterop.js).
    ///     <para>The token arrives shortly after start up and again every time the host renews the
    ///     session, so it cannot be read once and kept: subscribe to <see cref="AccessTokenChanged"/>,
    ///     or read <see cref="AccessToken"/> right before each call that needs it.</para>
    ///     <para>Outside the browser, or when the application is not embedded, there is no token:
    ///     <see cref="AccessToken"/> stays null and the event is never raised.</para>
    /// </summary>
    public static partial class AccessTokenInterop
    {
        #region public functions

        /// <summary>
        ///     The current access token, or null while none has arrived yet.
        /// </summary>
        public static string? AccessToken { get; private set; }

        /// <summary>
        ///     Raised when the token arrives and on every renewal, on the main (UI) thread.
        /// </summary>
        public static event Action<string>? AccessTokenChanged;

        /// <summary>
        ///     Imports the JS module once and starts listening for the token. Safe to call repeatedly.
        /// </summary>
        public static async Task InitializeInteropAsync()
        {
            if (!OperatingSystem.IsBrowser() || _isAvailable)
                return;

            try
            {
                await JSHost.ImportAsync("AccessTokenInterop", "./accessTokenInterop.js"); // /_framework/accessTokenInterop.js
                Subscribe(OnAccessTokenChanged);
                _isAvailable = true;
            }
            catch (Exception)
            {
                // Not fatal: the application also runs outside a host that supplies tokens.
            }
        }

        /// <summary>
        ///     Completes with the first access token received. For code that cannot start before
        ///     the token is there - a call that would otherwise be made with no credentials.
        /// </summary>
        public static Task<string> WaitForAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            return _firstAccessTokenTaskCompletionSource.Task.WaitAsync(cancellationToken);
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Called from JS for the current token and for every new one.
        /// </summary>
        private static void OnAccessTokenChanged(string accessToken)
        {
            if (String.IsNullOrEmpty(accessToken))
                return;

            AccessToken = accessToken;
            _firstAccessTokenTaskCompletionSource.TrySetResult(accessToken);

            try
            {
                AccessTokenChanged?.Invoke(accessToken);
            }
            catch (Exception ex)
            {
                // The call comes from JS: an exception escaping here would surface as an unhandled
                // rejection in the browser instead of being reported by the subscriber.
                Console.Error.WriteLine(@"AccessTokenChanged subscriber failed." + Environment.NewLine + ex);
            }
        }

        [JSImport("subscribe", "AccessTokenInterop")]
        private static partial void Subscribe(
            [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> onAccessTokenChanged);

        #endregion

        #region private fields

        private static bool _isAvailable;

        private static readonly TaskCompletionSource<string> _firstAccessTokenTaskCompletionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        #endregion
    }
}
