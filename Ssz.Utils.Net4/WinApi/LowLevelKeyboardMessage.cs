using System;
using System.Windows.Forms;

namespace Ssz.Utils.WinApi
{
    /// <summary>
    ///     A message that has been intercepted by a low-level mouse hook
    /// </summary>
    [CLSCompliant(false)]
    public class LowLevelKeyboardMessage : LowLevelMessage
    {
        #region construction and destruction

        /// <summary>
        ///     Creates a new low-level keyboard message.
        /// </summary>
        public LowLevelKeyboardMessage(int msg, int vkCode, int scanCode, int flags, int time, IntPtr dwExtraInfo)
            : base(msg, flags, time, dwExtraInfo)
        {
            _vkCode = vkCode;
            _scanCode = scanCode;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The virtual key code that caused this message.
        /// </summary>
        public int VirtualKeyCode
        {
            get { return _vkCode; }
        }

        /// <summary>
        ///     The scan code that caused this message.
        /// </summary>
        public int ScanCode
        {
            get { return _scanCode; }
        }

        /// <summary>
        ///     Flags needed to replay this event.
        /// </summary>
        public uint KeyboardEventFlags
        {
            get
            {
                switch ((WindowMessage)Message)
                {
                    case WindowMessage.WM_KEYDOWN:
                    case WindowMessage.WM_SYSKEYDOWN:
                        return 0;
                    case WindowMessage.WM_KEYUP:
                    case WindowMessage.WM_SYSKEYUP:
                        return KEYEVENTF_KEYUP;
                }
                throw new Exception("Unsupported message");
            }
        }

        /// <summary>
        ///     Replays this event.
        /// </summary>
        public override void ReplayEvent()
        {
            KeyboardKey.InjectKeyboardEvent((Keys) _vkCode, (byte) _scanCode, KeyboardEventFlags,
                new UIntPtr((ulong) ExtraInfo.ToInt64()));
        }

        #endregion

        #region private fields

        private readonly int _vkCode;
        private readonly int _scanCode;

        private const int KEYEVENTF_KEYUP = 0x2;        

        #endregion
    }
}