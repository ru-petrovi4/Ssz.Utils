#nullable disable

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace Ssz.Operator.Core.Utils.WinApi
{
    /// <summary>
    ///     As long as this object exists all mouse events created from a touch event for legacy support will be disabled.
    /// </summary>
    public class DisableTouchConversionToMouse : IDisposable
    {
        #region construction and destruction

        public DisableTouchConversionToMouse()
        {
            hookId = SetHook(hookCallback);
        }

        public void Dispose()
        {
            if (_disposed) return;

            User32.UnhookWindowsHookEx(hookId);
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~DisableTouchConversionToMouse()
        {
            Dispose();
        }

        #endregion
        
        static readonly User32.LowLevelMouseProc hookCallback = HookCallback;
        static IntPtr hookId = IntPtr.Zero;        

        static IntPtr SetHook(User32.LowLevelMouseProc proc)
        {
            var moduleHandle = User32.GetModuleHandle(null);

            var setHookResult = User32.SetWindowsHookEx(WH_MOUSE_LL, proc, moduleHandle, 0);
            if (setHookResult == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
            return setHookResult;
        }        

        static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var info = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                var extraInfo = unchecked((UInt64)info.dwExtraInfo.ToInt64());
                if ((extraInfo & MOUSEEVENTF_MASK) == MOUSEEVENTF_FROMTOUCH)
                {
                    if ((extraInfo & 0x80) != 0)
                    {
                        //Touch Input
                        return new IntPtr(1);
                    }
                    else
                    {
                        //Pen Input
                        return new IntPtr(1);
                    }

                }
            }

            return User32.CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        bool _disposed;

        const uint MOUSEEVENTF_MASK = 0xFFFFFF00;

        const uint MOUSEEVENTF_FROMTOUCH = 0xFF515700;
        const int WH_MOUSE_LL = 14;

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {

            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }        
    }
}
