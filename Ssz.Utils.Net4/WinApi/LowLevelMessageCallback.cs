namespace Ssz.Utils.Net4.WinApi
{
    /// <summary>
    ///     Represents a method that handles an intercepted low-level message.
    /// </summary>
    public delegate void LowLevelMessageCallback(LowLevelMessage evt, ref bool handled);
}