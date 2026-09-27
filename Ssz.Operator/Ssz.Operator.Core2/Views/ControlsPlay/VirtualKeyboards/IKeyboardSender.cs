using Avalonia.Input;
using System;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;

/// <summary>
/// Abstracts key press/release injection: the implementation raises Avalonia routed events on the
/// focused TopLevel, which works on every platform the editor and the player run on.
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
