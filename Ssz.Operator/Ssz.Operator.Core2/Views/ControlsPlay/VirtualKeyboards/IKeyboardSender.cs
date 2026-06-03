using System;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    /// <summary>
    /// Abstracts OS-level key press/release injection.
    /// Windows: uses Win32 SendInput via WinApi.KeyboardKey.
    /// Linux/other: raises Avalonia routed events on the focused TopLevel.
    /// </summary>
    public interface IKeyboardSender : IDisposable
    {
        /// <summary>Simulates pressing a key down.</summary>
        void Press(System.Windows.Forms.Keys key);

        /// <summary>Simulates releasing a key.</summary>
        void Release(System.Windows.Forms.Keys key);

        /// <summary>
        /// Returns the current state of the key.
        /// Negative = pressed, 0 = released, 1 = toggled (CapsLock etc.).
        /// </summary>
        short GetState(System.Windows.Forms.Keys key);
    }
}
