#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Ssz.Operator.Core.Utils.WinApi
{
    /// <summary>
    ///     A hook that intercepts mouse events
    /// </summary>
    public class LowLevelMouseHook : Hook
    {
        #region construction and destruction

        /// <summary>
        ///     Creates a low-level mouse hook and hooks it.
        /// </summary>
        public LowLevelMouseHook(MouseCallback callback)
            : this()
        {
            MouseIntercepted = callback;
            StartHook();
        }

        /// <summary>
        ///     Creates a low-level mouse hook.
        /// </summary>
        public LowLevelMouseHook()
            : base(HookType.WH_MOUSE_LL, false, true)
        {
            base.Callback += LowLevelMouseHook_Callback;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Called when a mouse action has been intercepted.
        /// </summary>
        public event MouseCallback MouseIntercepted;

        /// <summary>
        ///     Called when a mouse message has been intercepted.
        /// </summary>
        public event LowLevelMessageCallback MessageIntercepted;

        #endregion

        #region private functions

        private int LowLevelMouseHook_Callback(int code, IntPtr wParam, IntPtr lParam, ref bool callNext)
        {
            if (code == HC_ACTION)
            {
                var llh = (MSLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof (MSLLHOOKSTRUCT));
                bool handled = false;
                if (MouseIntercepted != null)
                {
                    MouseIntercepted((int) wParam, llh.pt, llh.mouseData, llh.flags, llh.time, llh.dwExtraInfo,
                        ref handled);
                }
                if (MessageIntercepted != null)
                {
                    MessageIntercepted(
                        new LowLevelMouseMessage((int) wParam, llh.pt, llh.mouseData, llh.flags, llh.time,
                            llh.dwExtraInfo), ref handled);
                }
                if (handled)
                {
                    callNext = false;
                    return 1;
                }
            }
            return 0;
        }

        #endregion

        /// <summary>
        ///     Represents a method that handles an intercepted mouse action.
        /// </summary>
        public delegate void MouseCallback(
            int msg, POINT pt, int mouseData, int flags, int time, IntPtr dwExtraInfo, ref bool handled);

        [StructLayout(LayoutKind.Sequential)]
        public class MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
    }
}