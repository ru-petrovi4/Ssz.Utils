using System;
using Avalonia.Controls;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    /// <summary>
    /// Creates the appropriate IKeyboardSender for the current OS.
    /// Call this once at startup and inject the result into GenericKeyboardModel.
    /// </summary>
    public static class KeyboardSenderFactory
    {
        /// <param name="targetTopLevel">
        ///   The Avalonia TopLevel (Window) that will receive synthetic events.
        ///   Required for Linux/macOS. On Windows it is ignored.
        /// </param>
        public static IKeyboardSender Create(TopLevel? targetTopLevel = null)
        {
#if WINDOWS
            if (OperatingSystem.IsWindows())
                return new WindowsKeyboardSender();
#endif
            return new AvaloniaKeyboardSender(targetTopLevel);
        }
    }
}
