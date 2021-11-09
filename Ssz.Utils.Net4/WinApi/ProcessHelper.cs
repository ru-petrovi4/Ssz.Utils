using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Ssz.Utils.WinApi
{
    public static class ProcessHelper
    {
        #region public functions

        /// <summary>        
        /// </summary>
        /// <param name="process"></param>
        public static void CloseAllWindows(Process process)
        {
            if (process is null || process.Threads.Count == 0) return;
            var uiProcessThread = process.Threads[0];
            if (uiProcessThread is null) return;
            User32.EnumThreadWindows(uiProcessThread.Id,
                                         new User32.CallBack(EnumWindowCallBack),
                                         IntPtr.Zero);            
        }

        #endregion

        #region private functions

        private static bool EnumWindowCallBack(IntPtr hWnd, IntPtr lParam)
        {
            // Seend WM_CLOSE message to close the main window
            User32.SendMessage(hWnd, User32.WM_CLOSE, 0, 0);
            return true;
            /*
            if ((Win32.GetWindow(hWnd, Win32.GW_OWNER) != IntPtr.Zero))
                //Win32.IsWindowVisible(hWnd))  //determine whether it's the main window
            {
                // Seend WM_CLOSE message to close the main window
                Win32.SendMessage(hWnd, Win32.WM_CLOSE, 0, 0);
                return false;
            }
            return true;*/
        }

        #endregion
    }
}
