﻿
namespace Ssz.Utils.CommandLine.Extensions
{
    /// <summary>
    ///     Utility extension methods for System.Char.
    /// </summary>
    internal static class CharExtensions
    {
        #region public functions

        public static bool IsWhiteSpace(this char c)
        {
            switch (c)
            {
                    // Regular
                case '\f':
                case '\v':
                case ' ':
                case '\t':
                    return true;

                    // Unicode
                default:
                    return c > 127 && char.IsWhiteSpace(c);
            }
        }

        public static bool IsLineTerminator(this char c)
        {
            switch (c)
            {
                case '\xD':
                case '\xA':
                case '\x2028':
                case '\x2029':
                    return true;

                default:
                    return false;
            }
        }

        #endregion
    }
}