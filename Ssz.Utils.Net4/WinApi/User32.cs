using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Net4.WinApi
{
    [CLSCompliant(false)]
    [SuppressUnmanagedCodeSecurity]
    public static class User32
    {
        #region public functions

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod,
                uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(int dwThreadId, CallBack lpfn, IntPtr lParam);

        public delegate bool CallBack(IntPtr hWnd, IntPtr lParam);

        public const int WM_CLOSE = 0x0010;
        public const int GW_OWNER = 4;

        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hWnd, int msg, int hParam, int lParam);

        [DllImport("user32.Dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, int wCmd);

        #endregion
    }
}
