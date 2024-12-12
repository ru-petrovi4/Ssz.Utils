#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Ssz.Operator.Core.Utils.WinApi
{
    /// <summary>
    ///     This class contains utility methods related to keys on the keyboard.
    /// </summary>    
    public class KeyboardKey
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of this class for a given key.
        /// </summary>
        /// <param name="key"></param>
        public KeyboardKey(Keys key)
        {
            _key = key;
            switch (key)
            {
                case Keys.Insert:
                case Keys.Delete:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    _extended = true;
                    break;
                default:
                    _extended = false;
                    break;
            }
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Inject a keyboard event into the event loop, as if the user performed
        ///     it with his keyboard.
        /// </summary>
        public static void InjectKeyboardEvent(Keys key, byte scanCode, uint flags, UIntPtr extraInfo)
        {
            keybd_event((byte) key, scanCode, flags, extraInfo);
        }

        /// <summary>
        ///     Inject a mouse event into the event loop, as if the user performed
        ///     it with his mouse.
        /// </summary>
        public static void InjectMouseEvent(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo)
        {
            mouse_event(flags, dx, dy, data, extraInfo);
        }

        /// <summary>
        ///     The state of this key, as seen by this application.
        /// </summary>
        public short State
        {
            get { return GetKeyState((short) _key); }
        }

        /// <summary>
        ///     The global state of this key.
        /// </summary>
        public short AsyncState
        {
            get { return GetAsyncKeyState((short) _key); }
        }

        /// <summary>
        ///     Determine the name of a key in the current keyboard layout.
        /// </summary>
        /// <returns>The key's name</returns>
        public string KeyName
        {
            get
            {
                var sb = new StringBuilder(512);
                int scancode = MapVirtualKey((int) _key, 0);
                if (_extended)
                    scancode += 0x100;
                GetKeyNameText(scancode << 16, sb, sb.Capacity);
                if (sb.Length == 0)
                {
                    switch (_key)
                    {
                        case Keys.BrowserBack:
                            sb.Append("Back");
                            break;
                        case Keys.BrowserForward:
                            sb.Append("Forward");
                            break;
                        case (Keys) 19:
                            sb.Append("Break");
                            break;
                        case Keys.Apps:
                            sb.Append("ContextMenu");
                            break;
                        case Keys.LWin:
                        case Keys.RWin:
                            sb.Append("Windows");
                            break;
                        case Keys.PrintScreen:
                            sb.Append("PrintScreen");
                            break;
                    }
                }
                return sb.ToString();
            }
        }

        /// <summary>
        ///     Press this key and release it.
        /// </summary>
        public void PressAndRelease()
        {
            Press();
            Release();
        }

        /// <summary>
        ///     Press this key.
        /// </summary>
        public void Press()
        {
            keybd_event((byte) _key, (byte) MapVirtualKey((int) _key, 0), _extended ? (uint) 0x1 : 0x0, UIntPtr.Zero);
        }

        /// <summary>
        ///     Release this key.
        /// </summary>
        public void Release()
        {
            keybd_event((byte) _key, (byte) MapVirtualKey((int) _key, 0), _extended ? (uint) 0x3 : 0x2, UIntPtr.Zero);
        }

        #endregion

        #region private functions

        [DllImport("user32.dll")]
        private static extern short GetKeyState(short nVirtKey);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags,
            UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData,
            UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern int GetKeyNameText(int lParam, [Out] StringBuilder lpString,
            int nSize);

        [DllImport("user32.dll")]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        #endregion

        #region private fields

        private readonly Keys _key;
        private readonly bool _extended;

        #endregion
    }
}