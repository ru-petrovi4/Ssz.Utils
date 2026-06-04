using Avalonia.Input;
using System;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;

/// <summary>
/// Abstracts OS-level key press/release injection.
/// Windows: uses Win32 SendInput via WinApi.KeyboardKey.
/// Linux/other: raises Avalonia routed events on the focused TopLevel.
/// </summary>
public interface IKeyboardSender : IDisposable
{
    /// <summary>Simulates pressing a key down.</summary>
    void Press(Key key);

    /// <summary>Simulates releasing a key.</summary>
    void Release(Key key);

    /// <summary>
    /// Returns the current state of the key.
    /// Negative = pressed, 0 = released, 1 = toggled (CapsLock etc.).
    /// </summary>
    short GetState(Key key);
}
