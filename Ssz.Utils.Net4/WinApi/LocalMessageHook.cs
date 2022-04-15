using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ssz.Utils.Net4.WinApi
{
    /// <summary>
    ///     A hook that intercepts local window messages.
    /// </summary>
    public class LocalMessageHook : Hook
    {
        #region construction and destruction

        /// <summary>
        ///     Creates a local message hook and hooks it.
        /// </summary>
        /// <param name="callback"></param>
        public LocalMessageHook(MessageCallback callback)
            : this()
        {
            MessageOccurred = callback;
            StartHook();
        }

        /// <summary>
        ///     Creates a local message hook.
        /// </summary>
        public LocalMessageHook()
            : base(HookType.WH_GETMESSAGE, false, false)
        {
            base.Callback += MessageHookCallback;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Called when a message has been intercepted.
        /// </summary>
        public event MessageCallback MessageOccurred;

        #endregion

        #region private functions

        private int MessageHookCallback(int code, IntPtr lParam, IntPtr wParam, ref bool callNext)
        {
            if (code == HC_ACTION)
            {
                var msg = (Message) Marshal.PtrToStructure(wParam, typeof (Message));
                if (MessageOccurred != null)
                {
                    MessageOccurred(msg);
                }
            }
            return 0;
        }

        #endregion

        /// <summary>
        ///     Represents a method that handles a message from a message hook.
        /// </summary>
        /// <param name="msg"></param>
        public delegate void MessageCallback(Message msg);
    }
}