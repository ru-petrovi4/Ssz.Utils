/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    internal class FocusChangeEventArgs : EventArgs
    {
        #region Constructors

        public FocusChangeEventArgs(IntPtr gotFocusWinHandle, IntPtr lostFocusWinHandle)
        {
            GotFocusWinHandle = gotFocusWinHandle;
            LostFocusWinHandle = lostFocusWinHandle;
        }

        #endregion

        #region Properties

        public IntPtr GotFocusWinHandle { get; }

        public IntPtr LostFocusWinHandle { get; }

        #endregion
    }

    internal class WindowHookHandler
    {
        #region Constructors

        #endregion

        #region Events

        public event EventHandler<FocusChangeEventArgs> FocusChanged;
        //public event EventHandler<WindowActivateEventArgs> Activate;

        #endregion

        #region Members

        private IntPtr _windowHook;
        private Win32Helper.HookProc _hookProc;
        private readonly ReentrantFlag _insideActivateEvent = new();

        #endregion

        #region Public Methods

        public void Attach()
        {
            _hookProc = HookProc;
            _windowHook = Win32Helper.SetWindowsHookEx(
                Win32Helper.HookType.WH_CBT,
                _hookProc,
                IntPtr.Zero,
                (int) Win32Helper.GetCurrentThreadId());
        }

        public void Detach()
        {
            Win32Helper.UnhookWindowsHookEx(_windowHook);
        }

        public int HookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code == Win32Helper.HCBT_SETFOCUS)
            {
                if (FocusChanged != null)
                    FocusChanged(this, new FocusChangeEventArgs(wParam, lParam));
            }
            else if (code == Win32Helper.HCBT_ACTIVATE)
            {
                if (_insideActivateEvent.CanEnter)
                    using (_insideActivateEvent.Enter())
                    {
                        //if (Activate != null)
                        //    Activate(this, new WindowActivateEventArgs(wParam));
                    }
            }


            return Win32Helper.CallNextHookEx(_windowHook, code, wParam, lParam);
        }

        #endregion
    }
}