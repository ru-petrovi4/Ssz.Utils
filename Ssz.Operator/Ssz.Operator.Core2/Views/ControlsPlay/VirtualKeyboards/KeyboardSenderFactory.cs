using Avalonia.Controls;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    /// <summary>
    /// Creates the IKeyboardSender to inject into GenericKeyboardModel at startup.
    /// </summary>
    public static class KeyboardSenderFactory
    {
        /// <param name="targetTopLevel">
        ///   The Avalonia TopLevel (Window) that will receive synthetic events.
        /// </param>
        public static IKeyboardSender Create(TopLevel? targetTopLevel = null)
        {
            return new AvaloniaKeyboardSender(targetTopLevel);
        }
    }
}
