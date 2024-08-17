using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
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

        /// <summary>
        ///     2009-06-15T13:45:30 (DateTimeKind.Local) --> 2009-06-15T13:45:30.0000000-07:00
        ///     2009-06-15T13:45:30 (DateTimeKind.Utc) --> 2009-06-15T13:45:30.0000000Z
        ///     2009-06-15T13:45:30 (DateTimeKind.Unspecified) --> 2009-06-15T13:45:30.0000000
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string GetString(DateTime dateTime)
        {
            return new Any(dateTime).ValueAsString(false, @"O");
        }

        /// <summary>
        ///     [-][d':']h':'mm':'ss[.FFFFFFF].
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static string GetString(TimeSpan timeSpan)
        {
            return new Any(timeSpan).ValueAsString(false, @"g");
        }        

        public static DateTime GetDateTimeUtc(string? dateTimeString)
        {
            if (String.IsNullOrEmpty(dateTimeString))
                return default;
            return new Any(dateTimeString!).ValueAs<DateTime>(false).ToUniversalTime();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSpanString"></param>
        /// <param name="defaultUnits"></param>
        /// <returns></returns>
        public static TimeSpan GetTimeSpan(string? timeSpanString, string? defaultUnits = null)
        {
            if (String.IsNullOrWhiteSpace(timeSpanString))
                return TimeSpan.Zero;

            if (timeSpanString.Count(f => f == ':') >= 2)
                return new Any(timeSpanString).ValueAs<TimeSpan>(false);

            timeSpanString = timeSpanString!.Trim();

            TimeSpan result = TimeSpan.Zero;

            string? numberString = null;
            double? number = null;
            string? units = null;
            foreach (Char ch in timeSpanString)
            {
                if (Char.IsDigit(ch) || ch == '.')
                {
                    if (numberString is null && number is not null)
                    {
                        result += GetTimeSpan(number.Value, units, defaultUnits);
                        number = null;
                        units = null;
                    }

                    numberString += ch;                    
                }
                else
                {
                    if (numberString is not null)
                    {
                        Double.TryParse(numberString, out double number2);
                        number = number2;
                        numberString = null;                        
                    }
                    
                    units += ch;
                }
                
            }

            if (number is not null)
                result += GetTimeSpan(number.Value, units, defaultUnits);

            return result;
        }

        public static DateTime SafeMinDateTimeUtc = DateTimeOffset.FromUnixTimeSeconds(0).UtcDateTime;

        #endregion

        #region private functions

        private static TimeSpan GetTimeSpan(double number, string? units, string? defaultUnits)
        {
            units = units?.Trim();
            if (String.IsNullOrEmpty(units))
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

        #endregion
    }
}


//public static char[] NumberCharArray = "0123456789.".ToCharArray();