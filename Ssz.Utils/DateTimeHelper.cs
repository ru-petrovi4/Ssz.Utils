using System;
using System.Collections.Generic;
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
                return DateTime.MinValue;
            return new Any(dateTimeString).ValueAs<DateTime>(false).ToUniversalTime();
        }

        public static string GetString(DateTime dateTime)
        {
            return new Any(dateTime).ValueAsString(false, @"O");
        }

        public static DateTime SafeMinDateTimeUtc = new DateTime(1900, 1, 1).ToUniversalTime();

        public static DateTime SafeMaxDateTimeUtc = new DateTime(3000, 1, 1).ToUniversalTime();

        #endregion
    }
}
