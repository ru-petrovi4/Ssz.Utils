using System;
using System.Windows.Forms;

namespace Ssz.Utils.Net4.WinApi
{
    /// <summary>
    ///     A message that has been intercepted by a low-level mouse hook
    /// </summary>
    [CLSCompliant(false)]
    public class LowLevelMouseMessage : LowLevelMessage
    {
        #region construction and destruction

        /// <summary>
        ///     Creates a new low-level mouse message.
        /// </summary>
        public LowLevelMouseMessage(int msg, POINT pt, int mouseData, int flags, int time, IntPtr dwExtraInfo)
            : base(msg, flags, time, dwExtraInfo)
        {
            _pt = pt;
            _mouseData = mouseData;
        }

        #endregion

        #region public functions        

        /// <summary>
        ///     The mouse position where this message occurred.
        /// </summary>
        public POINT Point
        {
            get { return _pt; }
        }

        /// <summary>
        ///     Additional mouse data, depending on the type of event.
        /// </summary>
        public int MouseData
        {
            get { return _mouseData; }
        }

        /// <summary>
        ///     Mouse event flags needed to replay this message.
        /// </summary>
        public uint MouseEventFlags
        {
            get
            {
                switch ((WindowMessage)Message)
                {
                    case WindowMessage.WM_LBUTTONDOWN:
                        return (uint) MouseEventFlagValues.LEFTDOWN;
                    case WindowMessage.WM_LBUTTONUP:
                        return (uint) MouseEventFlagValues.LEFTUP;
                    case WindowMessage.WM_MOUSEMOVE:
                        return (uint) MouseEventFlagValues.MOVE;
                    case WindowMessage.WM_MOUSEWHEEL:
                        return (uint) MouseEventFlagValues.WHEEL;
                    case WindowMessage.WM_MOUSEHWHEEL:
                        return (uint) MouseEventFlagValues.HWHEEL;
                    case WindowMessage.WM_RBUTTONDOWN:
                        return (uint) MouseEventFlagValues.RIGHTDOWN;
                    case WindowMessage.WM_RBUTTONUP:
                        return (uint) MouseEventFlagValues.RIGHTUP;
                    case WindowMessage.WM_MBUTTONDOWN:
                        return (uint) MouseEventFlagValues.MIDDLEDOWN;
                    case WindowMessage.WM_MBUTTONUP:
                        return (uint) MouseEventFlagValues.MIDDLEUP;
                    case WindowMessage.WM_MBUTTONDBLCLK:
                    case WindowMessage.WM_RBUTTONDBLCLK:
                    case WindowMessage.WM_LBUTTONDBLCLK:
                        return 0;
                }
                throw new Exception("Unsupported message");
            }
        }

        /// <summary>
        ///     Replays this event.
        /// </summary>
        public override void ReplayEvent()
        {
            Cursor.Position = Point;
            if (MouseEventFlags != 0)
                KeyboardKey.InjectMouseEvent(MouseEventFlags, 0, 0, (uint) _mouseData >> 16,
                    new UIntPtr((ulong) ExtraInfo.ToInt64()));
        }

        #endregion

        #region private fields

        private readonly POINT _pt;
        private readonly int _mouseData;

        #endregion

        [Flags]
        private enum MouseEventFlagValues
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010,
            WHEEL = 0x00000800,
            HWHEEL = 0x00001000
        }
    }
}