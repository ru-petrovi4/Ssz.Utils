#if WINDOWS
using System;
using System.Windows.Forms;
using Ssz.Operator.Core.Utils.WinApi;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    /// <summary>
    /// Windows implementation: delegates to the existing WinApi KeyboardKey (SendInput).
    /// Only compiled on Windows via the WINDOWS conditional.
    /// </summary>
    public sealed class WindowsKeyboardSender : IKeyboardSender
    {
        private readonly System.Collections.Generic.Dictionary<Keys, KeyboardKey> _keys = new();
        private bool _disposed;

        public void Press(Keys key) => GetOrCreate(key).Press();
        public void Release(Keys key) => GetOrCreate(key).Release();
        public short GetState(Keys key) => GetOrCreate(key).State;

        private KeyboardKey GetOrCreate(Keys key)
        {
            if (!_keys.TryGetValue(key, out var k))
            {
                k = new KeyboardKey(key);
                _keys[key] = k;
            }
            return k;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
#endif
