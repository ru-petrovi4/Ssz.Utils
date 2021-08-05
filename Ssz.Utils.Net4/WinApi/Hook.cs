using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Ssz.Utils;

namespace Ssz.Utils.WinApi
{
    /// <summary>
    ///     A hook is a point in the system message-handling mechanism where an application
    ///     can install a subroutine to monitor the message traffic in the system and process
    ///     certain types of messages before they reach the target window procedure.
    /// </summary>
    public class Hook : Component
    {
        #region construction and destruction

        /// <summary>
        ///     Creates a new hook and hooks it.
        /// </summary>
        public Hook(HookType type, HookCallback callback, bool wrapCallback, bool global)
            : this(type, wrapCallback, global)
        {
            Callback += callback;
            StartHook();
        }

        /// <summary>
        ///     Creates a new hook.
        /// </summary>
        public Hook(HookType type, bool wrapCallback, bool global)
            : this()
        {
            _type = type;
            _wrapCallback = wrapCallback;
            _global = global;
        }

        /*
        /// <summary>
        ///     Creates a new hook.
        /// </summary>
        public Hook(IContainer container)
            : this()
        {
            container.Add(this);
        }*/

        /// <summary>
        ///     Creates a new hook.
        /// </summary>
        public Hook()
        {
            _managedDelegate = InternalCallback;
        }

        /// <summary>
        ///     Unhooks the hook if necessary.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_hooked)
            {
                Unhook();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public static readonly int HC_ACTION = 0,
            HC_GETNEXT = 1,
            HC_SKIP = 2,
            HC_NOREMOVE = 3,
            HC_SYSMODALON = 4,
            HC_SYSMODALOFF = 5;

        /// <summary>
        ///     The type of the hook.
        /// </summary>
        public HookType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        ///     Whether this hook has been started.
        /// </summary>
        public bool Hooked
        {
            get { return _hooked; }
        }

        /// <summary>
        ///     Occurs when the hook's callback is called.
        /// </summary>
        public event HookCallback Callback;

        /// <summary>
        ///     Hooks the hook.
        /// </summary>
        public void StartHook()
        {
            if (_hooked) return;
            IntPtr delegt = Marshal.GetFunctionPointerForDelegate(_managedDelegate);
            if (_wrapCallback)
            {
                _wrappedDelegate = AllocHookWrapper(delegt);
                _hWrapperInstance = LoadLibrary("ManagedWinapiNativeHelper.dll");
                _hHook = SetWindowsHookEx(_type, _wrappedDelegate, _hWrapperInstance, 0);
            }
            else if (_global)
            {
                _hHook = SetWindowsHookEx(_type, delegt, Marshal.GetHINSTANCE(typeof (Hook).Assembly.GetModules()[0]), 0);
            }
            else
            {
                _hHook = SetWindowsHookEx(_type, delegt, IntPtr.Zero, getThreadID());
            }
            if (_hHook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            _hooked = true;
        }

        /// <summary>
        ///     Unhooks the hook.
        /// </summary>
        public virtual void Unhook()
        {
            if (!_hooked) return;
            if (!UnhookWindowsHookEx(_hHook)) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (_wrapCallback)
            {
                if (!FreeHookWrapper(_wrappedDelegate)) throw new Win32Exception();
                if (!FreeLibrary(_hWrapperInstance)) throw new Win32Exception();
            }
            _hooked = false;
        }

        #endregion

        #region internal functions

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        internal static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam,
            IntPtr lParam);

        #endregion

        #region protected functions

        /// <summary>
        ///     Override this method if you want to prevent a call
        ///     to the CallNextHookEx method or if you want to return
        ///     a different return value. For most hooks this is not needed.
        /// </summary>
        protected virtual int InternalCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && Callback != null)
            {
                bool callNext = true;
                int retval = Callback(code, wParam, lParam, ref callNext);
                if (!callNext) return retval;
            }
            return CallNextHookEx(_hHook, code, wParam, lParam);
        }

        #endregion

        #region private functions

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(HookType hook, IntPtr callback,
            IntPtr hMod, uint dwThreadId);

        //[DllImport("ManagedWinapiNativeHelper.dll")]
        private static IntPtr AllocHookWrapper(IntPtr callback)
        {
            Logger.Critical(
                "ManagedWinapiNativeHelper.dll now excluded from bins! Include it again to restore functionality!");
            return new IntPtr();
        }

        //[DllImport("ManagedWinapiNativeHelper.dll")]
        private static bool FreeHookWrapper(IntPtr wrapper)
        {
            Logger.Critical(
                "ManagedWinapiNativeHelper.dll now excluded from bins! Include it again to restore functionality!");
            return true;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        private uint getThreadID()
        {
#pragma warning disable 0618
            return (uint) AppDomain.GetCurrentThreadId();
#pragma warning restore 0618
        }

        #endregion

        #region private fields

        private bool _hooked;

        private HookType _type;
        private IntPtr _hHook;
        private readonly bool _wrapCallback;
        private readonly bool _global;
        private IntPtr _wrappedDelegate;
        private IntPtr _hWrapperInstance;
        private readonly HookProc _managedDelegate;

        #endregion

        /// <summary>
        ///     Represents a method that handles a callback from a hook.
        /// </summary>
        public delegate int HookCallback(int code, IntPtr wParam, IntPtr lParam, ref bool callNext);

        private delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);
    }
}