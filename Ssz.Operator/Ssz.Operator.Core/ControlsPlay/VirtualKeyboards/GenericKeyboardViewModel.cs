using System;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Ssz.Operator.Core.Utils.WinApi;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    public class GenericKeyboardViewModel : DisposableViewModelBase
    {
        #region private functions

        private void SyncStates()
        {
            /*
            private short _state = 0;
            if (_genericKeyboardModel.LeftCtrl.State != _state)
            {
                _state = _genericKeyboardModel.LeftCtrl.State;
                System.Windows.Forms.MessageBoxHelper.ShowInfo(_state.ToString());                
            }*/

            LeftShiftIsPressed = KeyboardModel.LeftShiftKey.State < 0;
            RightShiftIsPressed = KeyboardModel.RightShiftKey.State < 0;
            LeftCtrlIsPressed = KeyboardModel.LeftCtrlKey.State < 0;
            RightCtrlIsPressed = KeyboardModel.RightCtrlKey.State < 0;
            LeftWinIsPressed = KeyboardModel.LeftWinKey.State < 0;
            RightWinIsPressed = KeyboardModel.RightWinKey.State < 0;
            LeftAltIsPressed = KeyboardModel.LeftAltKey.State < 0;
            RightAltIsPressed = KeyboardModel.RightAltKey.State < 0;
            CapsLockIsActive = KeyboardModel.CapsLockKey.State == -127 ||
                               KeyboardModel.CapsLockKey.State == 1;
        }

        #endregion

        #region construction and destruction

        public GenericKeyboardViewModel() : this(new GenericKeyboardModel())
        {
        }

        public GenericKeyboardViewModel(GenericKeyboardModel keyboard)
        {
            KeyboardModel = keyboard;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(400);
            _timer.Tick += (sender, args) => SyncStates();
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing) _timer.Stop();
            // Release unmanaged resources.
            // Set large fields to null.

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public ICommand ButtonDownCommand
        {
            get
            {
                if (_buttonDownCommand is null)
                    _buttonDownCommand =
                        new RelayCommand(parameter => ButtonCommandHandler(parameter, true));
                return _buttonDownCommand;
            }
        }

        public ICommand ButtonUpCommand
        {
            get
            {
                if (_buttonUpCommand is null)
                    _buttonUpCommand =
                        new RelayCommand(parameter => ButtonCommandHandler(parameter, false));
                return _buttonUpCommand;
            }
        }

        public bool CapsLockIsActive
        {
            get => KeyboardModel.CapsLockIsActive;
            set
            {
                if (KeyboardModel.CapsLockIsActive != value)
                {
                    KeyboardModel.CapsLockIsActive = value;
                    OnPropertyChanged("CapsLockIsActive");
                }
            }
        }

        public bool LeftShiftIsPressed
        {
            get => KeyboardModel.LeftShiftIsPressed;
            set
            {
                if (KeyboardModel.LeftShiftIsPressed != value)
                {
                    KeyboardModel.LeftShiftIsPressed = value;
                    OnPropertyChanged("LeftShiftIsPressed");
                }
            }
        }

        public bool RightShiftIsPressed
        {
            get => KeyboardModel.RightShiftIsPressed;
            set
            {
                if (KeyboardModel.RightShiftIsPressed != value)
                {
                    KeyboardModel.RightShiftIsPressed = value;
                    OnPropertyChanged("RightShiftIsPressed");
                }
            }
        }


        public bool LeftCtrlIsPressed
        {
            get => KeyboardModel.LeftCtrlIsPressed;
            set
            {
                if (KeyboardModel.LeftCtrlIsPressed != value)
                {
                    KeyboardModel.LeftCtrlIsPressed = value;
                    OnPropertyChanged("LeftCtrlIsPressed");
                }
            }
        }

        public bool RightCtrlIsPressed
        {
            get => KeyboardModel.RightCtrlIsPressed;
            set
            {
                if (KeyboardModel.RightCtrlIsPressed != value)
                {
                    KeyboardModel.RightCtrlIsPressed = value;
                    OnPropertyChanged("RightCtrlIsPressed");
                }
            }
        }


        public bool LeftWinIsPressed
        {
            get => KeyboardModel.LeftWinIsPressed;
            set
            {
                if (KeyboardModel.LeftWinIsPressed != value)
                {
                    KeyboardModel.LeftWinIsPressed = value;
                    OnPropertyChanged("LeftWinIsPressed");
                }
            }
        }

        public bool RightWinIsPressed
        {
            get => KeyboardModel.RightWinIsPressed;
            set
            {
                if (KeyboardModel.RightWinIsPressed != value)
                {
                    KeyboardModel.RightWinIsPressed = value;
                    OnPropertyChanged("RightWinIsPressed");
                }
            }
        }


        public bool LeftAltIsPressed
        {
            get => KeyboardModel.LeftAltIsPressed;
            set
            {
                if (KeyboardModel.LeftAltIsPressed != value)
                {
                    KeyboardModel.LeftAltIsPressed = value;
                    OnPropertyChanged("LeftAltIsPressed");
                }
            }
        }

        public bool RightAltIsPressed
        {
            get => KeyboardModel.RightAltIsPressed;
            set
            {
                if (KeyboardModel.RightAltIsPressed != value)
                {
                    KeyboardModel.RightAltIsPressed = value;
                    OnPropertyChanged("RightAltIsPressed");
                }
            }
        }

        #endregion

        #region protected functions

        protected virtual void ButtonCommandHandler(object? parameter, bool isPressed)
        {
            var strParameter = parameter as string;

            if (string.IsNullOrWhiteSpace(strParameter)) return;

            Keys key;
            if (Enum.TryParse(strParameter, out key))
                switch (key)
                {
                    case Keys.LShiftKey:
                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        {
                            if (!isPressed) break;
                            LeftShiftIsPressed = !LeftShiftIsPressed;
                        }
                        else
                        {
                            LeftShiftIsPressed = isPressed;
                        }

                        if (LeftShiftIsPressed) KeyboardModel.LeftShiftKey.Press();
                        else KeyboardModel.LeftShiftKey.Release();
                        break;
                    case Keys.RShiftKey:
                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        {
                            if (!isPressed) break;
                            RightShiftIsPressed = !RightShiftIsPressed;
                        }
                        else
                        {
                            RightShiftIsPressed = isPressed;
                        }

                        if (RightShiftIsPressed) KeyboardModel.RightShiftKey.Press();
                        else KeyboardModel.RightShiftKey.Release();
                        break;
                    case Keys.LControlKey:
                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        {
                            if (!isPressed) break;
                            LeftCtrlIsPressed = !LeftCtrlIsPressed;
                        }
                        else
                        {
                            LeftCtrlIsPressed = isPressed;
                        }

                        if (LeftCtrlIsPressed) KeyboardModel.LeftCtrlKey.Press();
                        else KeyboardModel.LeftCtrlKey.Release();
                        break;
                    case Keys.RControlKey:
                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        {
                            if (!isPressed) break;
                            RightCtrlIsPressed = !RightCtrlIsPressed;
                        }
                        else
                        {
                            RightCtrlIsPressed = isPressed;
                        }

                        if (RightCtrlIsPressed) KeyboardModel.RightCtrlKey.Press();
                        else KeyboardModel.RightCtrlKey.Release();
                        break;
                    case Keys.LWin:
                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        {
                            if (!isPressed) break;
                            LeftWinIsPressed = !LeftWinIsPressed;
                        }
                        else
                        {
                            LeftWinIsPressed = isPressed;
                        }

                        if (LeftWinIsPressed) KeyboardModel.LeftWinKey.Press();
                        else KeyboardModel.LeftWinKey.Release();
                        break;
                    case Keys.RWin:
                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        {
                            if (!isPressed) break;
                            RightWinIsPressed = !RightWinIsPressed;
                        }
                        else
                        {
                            RightWinIsPressed = isPressed;
                        }

                        if (RightWinIsPressed) KeyboardModel.RightWinKey.Press();
                        else KeyboardModel.RightWinKey.Release();
                        break;
                    case Keys.LMenu:
                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        {
                            if (!isPressed) break;
                            LeftAltIsPressed = !LeftAltIsPressed;
                        }
                        else
                        {
                            LeftAltIsPressed = isPressed;
                        }

                        if (LeftAltIsPressed) KeyboardModel.LeftAltKey.Press();
                        else KeyboardModel.LeftAltKey.Release();
                        break;
                    case Keys.RMenu:
                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch)
                        {
                            if (!isPressed) break;
                            RightAltIsPressed = !RightAltIsPressed;
                        }
                        else
                        {
                            RightAltIsPressed = isPressed;
                        }

                        if (RightAltIsPressed) KeyboardModel.RightAltKey.Press();
                        else KeyboardModel.RightAltKey.Release();
                        break;
                    case Keys.CapsLock:
                        if (isPressed)
                        {
                            CapsLockIsActive = !CapsLockIsActive;
                            KeyboardModel.CapsLockKey.Press();
                        }
                        else
                        {
                            KeyboardModel.CapsLockKey.Release();
                        }

                        break;
                    default:
                        KeyboardKey? keyboardKey;
                        if (!KeyboardModel.KeysDictionary.TryGetValue(key, out keyboardKey))
                        {
                            keyboardKey = new KeyboardKey(key);
                            KeyboardModel.KeysDictionary[key] = keyboardKey;
                        }

                        if (isPressed) keyboardKey.Press();
                        else keyboardKey.Release();

                        if (PlayDsProjectView.TouchScreenMode != TouchScreenMode.MultiTouch) ReleaseStickyKeys();
                        break;
                }
        }

        protected virtual void ReleaseStickyKeys()
        {
            if (LeftShiftIsPressed)
            {
                LeftShiftIsPressed = false;
                KeyboardModel.LeftShiftKey.Release();
            }

            if (RightShiftIsPressed)
            {
                RightShiftIsPressed = false;
                KeyboardModel.RightShiftKey.Release();
            }

            if (LeftCtrlIsPressed)
            {
                LeftCtrlIsPressed = false;
                KeyboardModel.LeftCtrlKey.Release();
            }

            if (RightCtrlIsPressed)
            {
                RightCtrlIsPressed = false;
                KeyboardModel.RightCtrlKey.Release();
            }

            if (LeftWinIsPressed)
            {
                LeftWinIsPressed = false;
                KeyboardModel.LeftWinKey.Release();
            }

            if (RightWinIsPressed)
            {
                RightWinIsPressed = false;
                KeyboardModel.RightWinKey.Release();
            }

            if (LeftAltIsPressed)
            {
                LeftAltIsPressed = false;
                KeyboardModel.LeftAltKey.Release();
            }

            if (RightAltIsPressed)
            {
                RightAltIsPressed = false;
                KeyboardModel.RightAltKey.Release();
            }
        }

        protected GenericKeyboardModel KeyboardModel { get; }

        #endregion

        #region private fields

        private RelayCommand? _buttonDownCommand;
        private RelayCommand? _buttonUpCommand;
        private readonly DispatcherTimer _timer;

        #endregion
    }
}