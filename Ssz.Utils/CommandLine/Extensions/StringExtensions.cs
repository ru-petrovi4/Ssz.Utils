using System.Globalization;

namespace Ssz.Utils.CommandLine.Extensions
{
    /// <summary>
    ///     Utility extension methods for System.String.
    /// </summary>
    internal static class StringExtensions
    {
        #region public functions

        public static string Spaces(this int value)
        {
            return new string(' ', value);
        }

        public static bool IsNumeric(this string value)
        {
            decimal temporary;
            return decimal.TryParse(value, out temporary);
        }

        public static string FormatInvariant(this string value, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, value, arguments);
        }

        public static string FormatLocal(this string value, params object[] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture, value, arguments);
        }

        #endregion
    }
}