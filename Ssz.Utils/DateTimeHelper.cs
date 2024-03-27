using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class DateTimeHelper
    {
        #region public functions        

        public static DateTime GetDateTimeUtc(string? dateTimeString)
        {
            if (String.IsNullOrEmpty(dateTimeString))
                return default;
            return new Any(dateTimeString!).ValueAs<DateTime>(false).ToUniversalTime();
        }

        public static string GetString(DateTime dateTime)
        {
            return new Any(dateTime).ValueAsString(false, @"O");
        }

        public static TimeSpan GetTimeSpan(string? timeSpanString, string? defaultUnits = null)
        {
            if (String.IsNullOrWhiteSpace(timeSpanString))
                return TimeSpan.Zero;

            timeSpanString = timeSpanString!.Trim();

            int i = timeSpanString.LastIndexOfAny("0123456789".ToCharArray());
            if (i == -1)
                return TimeSpan.Zero;

            double number = new Any(timeSpanString.Substring(0, i + 1)).ValueAsDouble(false);
            string? units;
            if (i < timeSpanString.Length - 1)
                units = timeSpanString.Substring(i + 1).Trim();
            else
                units = defaultUnits;

            switch (units)
            {
                case @"ms":
                    return TimeSpan.FromMilliseconds(number);
                case @"s":
                    return TimeSpan.FromSeconds(number);
                case @"m":
                    return TimeSpan.FromMinutes(number);
                case @"h":
                    return TimeSpan.FromHours(number);
                case @"d":
                    return TimeSpan.FromDays(number);
                case @"w":
                    return TimeSpan.FromDays(number * 7);
                case @"M":
                    return TimeSpan.FromDays(number * 30);
                case @"Q":
                    return TimeSpan.FromDays(number * 30 * 3);
                case @"y":
                    return TimeSpan.FromDays(number * 365);
                default:
                    return TimeSpan.Zero;
            }
        }

        public static DateTime SafeMinDateTimeUtc = new DateTime(1900, 1, 1).ToUniversalTime();

        public static DateTime SafeMaxDateTimeUtc = new DateTime(3000, 1, 1).ToUniversalTime();

        #endregion
    }
}
