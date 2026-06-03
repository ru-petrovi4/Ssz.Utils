using System;
using System.Windows.Forms;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    /// <summary>
    /// ViewModel for the generic on-screen keyboard.
    /// Fully cross-platform — no WinApi references here.
    ///
    /// KEY CHANGE vs WPF version:
    ///   KeyboardKey.Press() / Release() replaced by GenericKeyboardModel.PressKey() / ReleaseKey(),
    ///   which delegate to the injected IKeyboardSender (AvaloniaKeyboardSender on Linux/macOS,
    ///   WindowsKeyboardSender on Windows).
    ///
    ///   SyncStates() now reads from GenericKeyboardModel.GetKeyState() which in turn
    ///   reads from IKeyboardSender.GetState() — no Win32 GetAsyncKeyState needed.
    /// </summary>
    public class GenericKeyboardViewModel : DisposableViewModelBase
    {
        #region construction and destruction

        public GenericKeyboardViewModel() : this(new GenericKeyboardModel()) { }

        /// <param name="keyboard">Pre-constructed model (with the right IKeyboardSender).</param>
        public GenericKeyboardViewModel(GenericKeyboardModel keyboard)
        {
            KeyboardModel = keyboard;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _timer.Tick += (_, _) => SyncStates();
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            if (disposing)
            {
                _timer.Stop();
                KeyboardModel.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public ICommand ButtonDownCommand =>
            _buttonDownCommand ??= new RelayCommand(p => ButtonCommandHandler(p, true));

        public ICommand ButtonUpCommand =>
            _buttonUpCommand ??= new RelayCommand(p => ButtonCommandHandler(p, false));

        public bool CapsLockIsActive
        {
            get => KeyboardModel.CapsLockIsActive;
            set { if (KeyboardModel.CapsLockIsActive == value) return; KeyboardModel.CapsLockIsActive = value; OnPropertyChanged(nameof(CapsLockIsActive)); }
        }
        public bool LeftShiftIsPressed
        {
            get => KeyboardModel.LeftShiftIsPressed;
            set { if (KeyboardModel.LeftShiftIsPressed == value) return; KeyboardModel.LeftShiftIsPressed = value; OnPropertyChanged(nameof(LeftShiftIsPressed)); }
        }
        public bool RightShiftIsPressed
        {
            get => KeyboardModel.RightShiftIsPressed;
            set { if (KeyboardModel.RightShiftIsPressed == value) return; KeyboardModel.RightShiftIsPressed = value; OnPropertyChanged(nameof(RightShiftIsPressed)); }
        }
        public bool LeftCtrlIsPressed
        {
            get => KeyboardModel.LeftCtrlIsPressed;
            set { if (KeyboardModel.LeftCtrlIsPressed == value) return; KeyboardModel.LeftCtrlIsPressed = value; OnPropertyChanged(nameof(LeftCtrlIsPressed)); }
        }
        public bool RightCtrlIsPressed
        {
            get => KeyboardModel.RightCtrlIsPressed;
            set { if (KeyboardModel.RightCtrlIsPressed == value) return; KeyboardModel.RightCtrlIsPressed = value; OnPropertyChanged(nameof(RightCtrlIsPressed)); }
        }
        public bool LeftWinIsPressed
        {
            get => KeyboardModel.LeftWinIsPressed;
            set { if (KeyboardModel.LeftWinIsPressed == value) return; KeyboardModel.LeftWinIsPressed = value; OnPropertyChanged(nameof(LeftWinIsPressed)); }
        }
        public bool RightWinIsPressed
        {
            get => KeyboardModel.RightWinIsPressed;
            set { if (KeyboardModel.RightWinIsPressed == value) return; KeyboardModel.RightWinIsPressed = value; OnPropertyChanged(nameof(RightWinIsPressed)); }
        }
        public bool LeftAltIsPressed
        {
            get => KeyboardModel.LeftAltIsPressed;
            set { if (KeyboardModel.LeftAltIsPressed == value) return; KeyboardModel.LeftAltIsPressed = value; OnPropertyChanged(nameof(LeftAltIsPressed)); }
        }
        public bool RightAltIsPressed
        {
            get => KeyboardModel.RightAltIsPressed;
            set { if (KeyboardModel.RightAltIsPressed == value) return; KeyboardModel.RightAltIsPressed = value; OnPropertyChanged(nameof(RightAltIsPressed)); }
        }

        #endregion

        #region protected functions

        protected virtual void ButtonCommandHandler(object? parameter, bool isPressed)
        {
            var strParameter = parameter as string;
            if (string.IsNullOrWhiteSpace(strParameter)) return;

            if (!Enum.TryParse<Keys>(strParameter, out var key)) return;

            switch (key)
            {
                case Keys.LShiftKey:
                    ToggleOrSet(ref _leftShiftField, isPressed, k => LeftShiftIsPressed = k,
                        Keys.LShiftKey);
                    break;
                case Keys.RShiftKey:
                    ToggleOrSet(ref _rightShiftField, isPressed, k => RightShiftIsPressed = k,
                        Keys.RShiftKey);
                    break;
                case Keys.LControlKey:
                    ToggleOrSet(ref _leftCtrlField, isPressed, k => LeftCtrlIsPressed = k,
                        Keys.LControlKey);
                    break;
                case Keys.RControlKey:
                    ToggleOrSet(ref _rightCtrlField, isPressed, k => RightCtrlIsPressed = k,
                        Keys.RControlKey);
                    break;
                case Keys.LWin:
                    ToggleOrSet(ref _leftWinField, isPressed, k => LeftWinIsPressed = k,
                        Keys.LWin);
                    break;
                case Keys.RWin:
                    ToggleOrSet(ref _rightWinField, isPressed, k => RightWinIsPressed = k,
                        Keys.RWin);
                    break;
                case Keys.Menu: // Left Alt
                    ToggleOrSet(ref _leftAltField, isPressed, k => LeftAltIsPressed = k,
                        Keys.Menu);
                    break;
                case Keys.RMenu:
                    ToggleOrSet(ref _rightAltField, isPressed, k => RightAltIsPressed = k,
                        Keys.RMenu);
                    break;
                case Keys.CapsLock:
                    if (isPressed)
                    {
                        CapsLockIsActive = !CapsLockIsActive;
                        KeyboardModel.PressKey(Keys.CapsLock);
                    }
                    else
                    {
                        KeyboardModel.ReleaseKey(Keys.CapsLock);
                    }
                    break;

                default:
                    if (isPressed) KeyboardModel.PressKey(key);
                    else KeyboardModel.ReleaseKey(key);

                    if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        ReleaseStickyKeys();
                    break;
            }
        }

        protected virtual void ReleaseStickyKeys()
        {
            if (LeftShiftIsPressed)  { LeftShiftIsPressed  = false; KeyboardModel.ReleaseKey(Keys.LShiftKey);   }
            if (RightShiftIsPressed) { RightShiftIsPressed = false; KeyboardModel.ReleaseKey(Keys.RShiftKey);   }
            if (LeftCtrlIsPressed)   { LeftCtrlIsPressed   = false; KeyboardModel.ReleaseKey(Keys.LControlKey); }
            if (RightCtrlIsPressed)  { RightCtrlIsPressed  = false; KeyboardModel.ReleaseKey(Keys.RControlKey); }
            if (LeftWinIsPressed)    { LeftWinIsPressed    = false; KeyboardModel.ReleaseKey(Keys.LWin);        }
            if (RightWinIsPressed)   { RightWinIsPressed   = false; KeyboardModel.ReleaseKey(Keys.RWin);        }
            if (LeftAltIsPressed)    { LeftAltIsPressed    = false; KeyboardModel.ReleaseKey(Keys.Menu);        }
            if (RightAltIsPressed)   { RightAltIsPressed   = false; KeyboardModel.ReleaseKey(Keys.RMenu);       }
        }

        protected GenericKeyboardModel KeyboardModel { get; }

        #endregion

        #region private functions

        /// <summary>
        /// In MultiTouch mode: honour the actual press/release (isPressed).
        /// In SingleTouch/kiosk mode: toggle on press, ignore release.
        /// </summary>
        private void ToggleOrSet(ref bool field, bool isPressed,
            Action<bool> setter, Keys key)
        {
            if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
            {
                if (!isPressed) return;
                field = !field;
                setter(field);
            }
            else
            {
                field = isPressed;
                setter(field);
            }
            if (field) KeyboardModel.PressKey(key);
            else KeyboardModel.ReleaseKey(key);
        }

        private void SyncStates()
        {
            // AvaloniaKeyboardSender tracks state internally via _pressed HashSet,
            // so GetKeyState returns the correct values on all platforms.
            LeftShiftIsPressed  = KeyboardModel.GetKeyState(Keys.LShiftKey)  < 0;
            RightShiftIsPressed = KeyboardModel.GetKeyState(Keys.RShiftKey)  < 0;
            LeftCtrlIsPressed   = KeyboardModel.GetKeyState(Keys.LControlKey)< 0;
            RightCtrlIsPressed  = KeyboardModel.GetKeyState(Keys.RControlKey)< 0;
            LeftWinIsPressed    = KeyboardModel.GetKeyState(Keys.LWin)       < 0;
            RightWinIsPressed   = KeyboardModel.GetKeyState(Keys.RWin)       < 0;
            LeftAltIsPressed    = KeyboardModel.GetKeyState(Keys.Menu)       < 0;
            RightAltIsPressed   = KeyboardModel.GetKeyState(Keys.RMenu)      < 0;
            var capsState = KeyboardModel.GetKeyState(Keys.CapsLock);
            CapsLockIsActive = capsState == -127 || capsState == 1;
        }

        #endregion

        #region private fields

        private RelayCommand? _buttonDownCommand;
        private RelayCommand? _buttonUpCommand;
        private readonly DispatcherTimer _timer;

        // Local shadow fields for ToggleOrSet
        private bool _leftShiftField, _rightShiftField;
        private bool _leftCtrlField, _rightCtrlField;
        private bool _leftWinField, _rightWinField;
        private bool _leftAltField, _rightAltField;

        #endregion
    }
}
