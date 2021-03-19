namespace Ssz.DataGrpc.Common
{
    /// <summary>
    ///     The Limit bits indicates whether a value is liimited or not.
    ///     It is valid regardless of the values of the StatusBits and SubstatusBits.
    /// </summary>
    public enum DataGrpcStatusCodeLimitBits : uint
    {
        /// <summary>
        ///     This value may be used to mask (keep) the bits
        ///     used to convey the limited status of the value.
        /// </summary>
        LimitBitsMask = 0x03000000,

        /// <summary>
        ///     This value provides the number of bits to shift
        ///     the limit bits into the low bits or into the
        ///     Xi Status Code bit position.
        /// </summary>
        LimitBitsShiftCount = 24,

        /// <summary>
        ///     The value is free to move up or down. This value is
        ///     used as the default value when the limit bits do not apply.
        /// </summary>
        NotLimited = 0,

        /// <summary>
        ///     The value is free to move up or down. This value is
        ///     used as the default value when the limit bits do not apply.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        NotLimitedBits = 0x00000000,

        /// <summary>
        ///     The value has ‘pegged’ at some lower limit and
        ///     cannot move any lower.
        /// </summary>
        LowLimited = 1,

        /// <summary>
        ///     The value has ‘pegged’ at some lower limit and
        ///     cannot move any lower.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        LowLimitedBits = 0x10000000,

        /// <summary>
        ///     The value has ‘pegged’ at some high limit and
        ///     cannot move any higher.
        /// </summary>
        HighLimited = 2,

        /// <summary>
        ///     The value has ‘pegged’ at some high limit and
        ///     cannot move any higher.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        HighLimitedBits = 0x20000000,

        /// <summary>
        ///     The value is a constant and cannot move.
        /// </summary>
        Constant = 3,

        /// <summary>
        ///     The value is a constant and cannot move.
        ///     This value is in Xi Status Code bit position.
        /// </summary>
        ConstantBits = 0x30000000,
    }
}