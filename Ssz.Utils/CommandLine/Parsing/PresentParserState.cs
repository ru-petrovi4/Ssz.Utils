using System;


namespace Ssz.Utils.CommandLine.Parsing
{
    [Flags]
    internal enum PresentParserState : ushort
    {
        Undefined = 0x00,

        Success = 0x01,

        Failure = 0x02,

        MoveOnNextElement = 0x04
    }
}