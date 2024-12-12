using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Ssz.Operator.Core.Utils.WinApi;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    public class GenericKeyboardModel
    {
        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region construction and destruction

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.			
            }

            // Release unmanaged resources.
            // Set large fields to null.            
            Disposed = true;
        }


        ~GenericKeyboardModel()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public bool CapsLockIsActive { get; set; }

        public bool LeftShiftIsPressed { get; set; }
        public bool RightShiftIsPressed { get; set; }
        public bool LeftCtrlIsPressed { get; set; }
        public bool RightCtrlIsPressed { get; set; }
        public bool LeftWinIsPressed { get; set; }
        public bool RightWinIsPressed { get; set; }
        public bool LeftAltIsPressed { get; set; }
        public bool RightAltIsPressed { get; set; }

        public KeyboardKey LeftShiftKey { get; } = new(Keys.LShiftKey);

        public KeyboardKey RightShiftKey { get; } = new(Keys.RShiftKey);

        public KeyboardKey LeftCtrlKey { get; } = new(Keys.LControlKey);

        public KeyboardKey RightCtrlKey { get; } = new(Keys.RControlKey);

        public KeyboardKey LeftWinKey { get; } = new(Keys.LWin);

        public KeyboardKey RightWinKey { get; } = new(Keys.RWin);

        public KeyboardKey LeftAltKey { get; } = new(Keys.Menu);

        public KeyboardKey RightAltKey { get; } = new(Keys.RMenu);

        public KeyboardKey CapsLockKey { get; } = new(Keys.CapsLock);

        public Dictionary<Keys, KeyboardKey> KeysDictionary { get; } = new(200);

        #endregion

        #region private fields

        #endregion
    }
}