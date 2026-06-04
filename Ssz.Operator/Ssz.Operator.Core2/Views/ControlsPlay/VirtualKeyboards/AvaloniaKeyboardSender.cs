using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;

/// <summary>
/// Cross-platform implementation (Linux / macOS / Windows).
/// Injects key events directly into the Avalonia routed event system
/// on the currently focused TopLevel. No OS-level SendInput used.
///
/// Approach:
///   • Printable characters  → TextInputEvent  (correct IME-aware path per Avalonia docs)
///   • Control / modifier / navigation keys → KeyDownEvent / KeyUpEvent on the TopLevel
///
/// The virtual keyboard window itself must have Focusable=false on all buttons
/// (already set in AXAML) so the target window keeps focus.
/// </summary>
public sealed class AvaloniaKeyboardSender : IKeyboardSender
{
    // Tracks which keys are currently "held" so GetState works.
    private readonly HashSet<Key> _pressed = new();
    // CapsLock toggle state
    private bool _capsLockActive;
    private bool _disposed;

    // The TopLevel to send events to. Injected at construction time
    // (typically the main window / PlayDsProjectView window).
    private TopLevel? _targetTopLevel;

    public AvaloniaKeyboardSender(TopLevel? targetTopLevel = null)
    {
        _targetTopLevel = targetTopLevel;
    }

    // ----------------------------------------------------------------
    // IKeyboardSender
    // ----------------------------------------------------------------

    public void Press(Key key)
    {
        if (_disposed) return;
        _pressed.Add(key);

        if (key == Key.CapsLock)
            _capsLockActive = !_capsLockActive;

        Dispatcher.UIThread.Post(() => RaiseKeyDown(key));
    }

    public void Release(Key key)
    {
        if (_disposed) return;
        _pressed.Remove(key);

        Dispatcher.UIThread.Post(() => RaiseKeyUp(key));
    }

    /// <summary>
    /// Mimics WinApi KeyboardKey.State semantics:
    ///   negative  → key is currently pressed
    ///   0         → key is not pressed
    ///   1 / -127  → toggle key (CapsLock) active state
    /// </summary>
    public short GetState(Key key)
    {
        if (key == Key.CapsLock)
            return _capsLockActive ? (short)1 : (short)0;

        return _pressed.Contains(key) ? (short)-128 : (short)0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _pressed.Clear();
        _disposed = true;
    }

    // ----------------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------------


    // ----------------------------------------------------------------
    // Public surface for late-binding the target window
    // ----------------------------------------------------------------

    public TopLevel? TargetTopLevel => _targetTopLevel;

    /// <summary>
    /// Call this from VirtualKeyboardWindow once the owning TopLevel is known.
    /// </summary>
    public void SetTargetTopLevel(TopLevel? topLevel)
    {
        _targetTopLevel = topLevel;
    }

    private TopLevel? GetTopLevel()
    {
        // Prefer the explicitly injected target.
        if (_targetTopLevel is not null) return _targetTopLevel;

        // Fallback: the currently focused window (works for single-window apps).
        return null; // caller must supply TopLevel for multi-window scenarios
    }

    private void RaiseKeyDown(Key key)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return;
        
        var modifiers = BuildModifiers();

        // For printable characters send TextInput so TextBox gets the char correctly.
        var text = KeyToText(key, modifiers);
        if (text is not null)
        {
            topLevel.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Source = topLevel,
                Text = text
            });
            return; // TextInput covers the character; no need to also send KeyDown.
        }

        // Non-printable / control keys: send KeyDown.
        topLevel.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = topLevel,
            Key = key,
            KeyModifiers = modifiers
        });
    }

    private void RaiseKeyUp(Key key)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        topLevel.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Source = topLevel,
            Key = key,
            KeyModifiers = BuildModifiers()
        });
    }

    private KeyModifiers BuildModifiers()
    {
        var m = KeyModifiers.None;
        if (_pressed.Contains(Key.LeftShift) || _pressed.Contains(Key.RightShift))
            m |= KeyModifiers.Shift;
        if (_pressed.Contains(Key.LeftCtrl) || _pressed.Contains(Key.RightCtrl))
            m |= KeyModifiers.Control;
        if (_pressed.Contains(Key.LeftAlt) || _pressed.Contains(Key.RightAlt))
            m |= KeyModifiers.Alt;
        if (_pressed.Contains(Key.LWin) || _pressed.Contains(Key.RWin))
            m |= KeyModifiers.Meta;
        return m;
    }

    /// <summary>
    /// Returns the printable string for the key+modifiers combo, or null for
    /// non-printable keys (Enter, Delete, arrows, modifiers, F-keys, etc.).
    /// </summary>
    private string? KeyToText(Key key, KeyModifiers modifiers)
    {
        bool shift = modifiers.HasFlag(KeyModifiers.Shift);
        bool caps = _capsLockActive;
        // Effective upper-case state for letters
        bool upper = shift ^ caps;

        return key switch
        {
            // Digits row
            Key.D0 => shift ? ")" : "0",
            Key.D1 => shift ? "!" : "1",
            Key.D2 => shift ? "@" : "2",
            Key.D3 => shift ? "#" : "3",
            Key.D4 => shift ? "$" : "4",
            Key.D5 => shift ? "%" : "5",
            Key.D6 => shift ? "^" : "6",
            Key.D7 => shift ? "&" : "7",
            Key.D8 => shift ? "*" : "8",
            Key.D9 => shift ? "(" : "9",
            // Numpad
            Key.NumPad0 => "0",
            Key.NumPad1 => "1",
            Key.NumPad2 => "2",
            Key.NumPad3 => "3",
            Key.NumPad4 => "4",
            Key.NumPad5 => "5",
            Key.NumPad6 => "6",
            Key.NumPad7 => "7",
            Key.NumPad8 => "8",
            Key.NumPad9 => "9",
            Key.Decimal => ".",
            Key.Add => "+",
            Key.Subtract => "-",
            Key.Multiply => "*",
            Key.Divide => "/",
            // Punctuation (US layout)
            Key.OemMinus => shift ? "_" : "-",
            Key.OemPlus => shift ? "+" : "=",
            Key.OemOpenBrackets => shift ? "{" : "[",
            Key.OemCloseBrackets => shift ? "}" : "]",
            Key.OemPipe => shift ? "|" : "\\",
            Key.OemSemicolon => shift ? ":" : ";",
            Key.OemQuotes => shift ? "\"" : "'",
            Key.OemComma => shift ? "<" : ",",
            Key.OemPeriod => shift ? ">" : ".",
            Key.OemQuestion => shift ? "?" : "/",
            Key.OemTilde => shift ? "~" : "`",
            Key.Space => " ",
            // Letters A-Z
            >= Key.A and <= Key.Z =>
                (upper ? key.ToString() : key.ToString().ToLowerInvariant()),
            // Everything else (Enter, Delete, arrows, F-keys, modifiers) → null
            _ => null
        };
    }
}


// ----------------------------------------------------------------
// Key mapping: System.Windows.Forms.Keys → Avalonia.Input.Key
// ----------------------------------------------------------------
//private static readonly Dictionary<Keys, Key> _keyMap = new()
//{
//    // Letters
//    [Keys.A] = Key.A, [Keys.B] = Key.B, [Keys.C] = Key.C, [Keys.D] = Key.D,
//    [Keys.E] = Key.E, [Keys.F] = Key.F, [Keys.G] = Key.G, [Keys.H] = Key.H,
//    [Keys.I] = Key.I, [Keys.J] = Key.J, [Keys.K] = Key.K, [Keys.L] = Key.L,
//    [Keys.M] = Key.M, [Keys.N] = Key.N, [Keys.O] = Key.O, [Keys.P] = Key.P,
//    [Keys.Q] = Key.Q, [Keys.R] = Key.R, [Keys.S] = Key.S, [Keys.T] = Key.T,
//    [Keys.U] = Key.U, [Keys.V] = Key.V, [Keys.W] = Key.W, [Keys.X] = Key.X,
//    [Keys.Y] = Key.Y, [Keys.Z] = Key.Z,
//    // Digits
//    [Keys.D0] = Key.D0, [Keys.D1] = Key.D1, [Keys.D2] = Key.D2,
//    [Keys.D3] = Key.D3, [Keys.D4] = Key.D4, [Keys.D5] = Key.D5,
//    [Keys.D6] = Key.D6, [Keys.D7] = Key.D7, [Keys.D8] = Key.D8, [Keys.D9] = Key.D9,
//    // Numpad
//    [Keys.NumPad0] = Key.NumPad0, [Keys.NumPad1] = Key.NumPad1, [Keys.NumPad2] = Key.NumPad2,
//    [Keys.NumPad3] = Key.NumPad3, [Keys.NumPad4] = Key.NumPad4, [Keys.NumPad5] = Key.NumPad5,
//    [Keys.NumPad6] = Key.NumPad6, [Keys.NumPad7] = Key.NumPad7, [Keys.NumPad8] = Key.NumPad8,
//    [Keys.NumPad9] = Key.NumPad9,
//    [Keys.Decimal] = Key.OemPeriod, [Keys.Add] = Key.Add, [Keys.Subtract] = Key.Subtract,
//    [Keys.Multiply] = Key.Multiply, [Keys.Divide] = Key.Divide,
//    // Navigation / editing
//    [Keys.Enter] = Key.Enter, [Keys.Return] = Key.Return,
//    [Keys.Back] = Key.Back,
//    [Keys.Delete] = Key.Delete,
//    [Keys.Escape] = Key.Escape,
//    [Keys.Tab] = Key.Tab,
//    [Keys.Space] = Key.Space,
//    [Keys.Left] = Key.Left, [Keys.Right] = Key.Right,
//    [Keys.Up] = Key.Up, [Keys.Down] = Key.Down,
//    [Keys.Home] = Key.Home, [Keys.End] = Key.End,
//    [Keys.PageUp] = Key.PageUp, [Keys.PageDown] = Key.PageDown,
//    [Keys.Insert] = Key.Insert,
//    // Modifiers
//    [Keys.LShiftKey] = Key.LeftShift, [Keys.RShiftKey] = Key.RightShift,
//    [Keys.ShiftKey] = Key.LeftShift,
//    [Keys.LControlKey] = Key.LeftCtrl, [Keys.RControlKey] = Key.RightCtrl,
//    [Keys.ControlKey] = Key.LeftCtrl,
//    [Keys.Menu] = Key.LeftAlt, [Keys.RMenu] = Key.RightAlt,
//    [Keys.LWin] = Key.LWin, [Keys.RWin] = Key.RWin,
//    [Keys.CapsLock] = Key.CapsLock,
//    // F-keys
//    [Keys.F1]  = Key.F1,  [Keys.F2]  = Key.F2,  [Keys.F3]  = Key.F3,
//    [Keys.F4]  = Key.F4,  [Keys.F5]  = Key.F5,  [Keys.F6]  = Key.F6,
//    [Keys.F7]  = Key.F7,  [Keys.F8]  = Key.F8,  [Keys.F9]  = Key.F9,
//    [Keys.F10] = Key.F10, [Keys.F11] = Key.F11, [Keys.F12] = Key.F12,
//    // Punctuation (US layout)
//    [Keys.OemMinus]        = Key.OemMinus,
//    [Keys.Oemplus]         = Key.OemPlus,
//    [Keys.OemOpenBrackets] = Key.OemOpenBrackets,
//    [Keys.OemCloseBrackets]= Key.OemCloseBrackets,
//    [Keys.OemPipe]         = Key.OemPipe,
//    [Keys.OemSemicolon]    = Key.OemSemicolon,
//    [Keys.OemQuotes]       = Key.OemQuotes,
//    [Keys.Oemcomma]        = Key.OemComma,
//    [Keys.OemPeriod]       = Key.OemPeriod,
//    [Keys.OemQuestion]     = Key.OemQuestion,
//    [Keys.Oemtilde]        = Key.OemTilde,
//};

//private static Key WinFormsKeyToAvaloniaKey(Keys key)
//    {
//        return _keyMap.TryGetValue(key, out var avKey) ? avKey : Key.None;
//    }