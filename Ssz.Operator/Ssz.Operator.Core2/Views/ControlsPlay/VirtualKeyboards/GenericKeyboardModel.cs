using System;
using System.Collections.Generic;
using System.Windows.Forms; // Keys enum only — no WinForms UI used

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    /// <summary>
    /// Keyboard state model.
    /// All OS-level key injection is now delegated to IKeyboardSender,
    /// which is cross-platform (Avalonia events on Linux, SendInput on Windows).
    ///
    /// The old KeyboardKey helper is replaced by IKeyboardSender throughout.
    /// </summary>
    public class GenericKeyboardModel : IDisposable
    {
        #region construction and destruction

        /// <param name="sender">
        ///   Injected keyboard sender. If null, KeyboardSenderFactory.Create() is used.
        ///   Pass the result of KeyboardSenderFactory.Create(topLevel) from your ViewModel.
        /// </param>
        public GenericKeyboardModel(IKeyboardSender? sender = null)
        {
            Sender = sender ?? KeyboardSenderFactory.Create();
        }

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
                Sender.Dispose();
            }
            Disposed = true;
        }

        ~GenericKeyboardModel() => Dispose(false);

        #endregion

        #region public functions

        /// <summary>Cross-platform key injection backend.</summary>
        public IKeyboardSender Sender { get; }

        // Modifier / toggle states — kept as simple booleans,
        // mirrored from Sender.GetState() by GenericKeyboardViewModel.SyncStates().
        public bool CapsLockIsActive    { get; set; }
        public bool LeftShiftIsPressed  { get; set; }
        public bool RightShiftIsPressed { get; set; }
        public bool LeftCtrlIsPressed   { get; set; }
        public bool RightCtrlIsPressed  { get; set; }
        public bool LeftWinIsPressed    { get; set; }
        public bool RightWinIsPressed   { get; set; }
        public bool LeftAltIsPressed    { get; set; }
        public bool RightAltIsPressed   { get; set; }

        // Convenience wrappers — match the old KeyboardKey.Press/Release API.
        public void PressKey(Keys key)   => Sender.Press(key);
        public void ReleaseKey(Keys key) => Sender.Release(key);
        public short GetKeyState(Keys key) => Sender.GetState(key);

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion
    }
}
