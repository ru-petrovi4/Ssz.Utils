using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.Addons.ExperionEventsJournalFilesImporter
{
    public static class ExperionHelper
    {
        public static (DateTime, bool) GetDateTime(string? dateTimeString, string dateTimeFormatOption)
        {
            if (String.IsNullOrEmpty(dateTimeString))
                return (DateTime.MinValue, false);
            DateTime timeUtc;
            bool succeeded = DateTime.TryParseExact(dateTimeString, @"dd.MM.yyyy H:mm:ss.FFFFF", CultureInfo.InvariantCulture,
                               DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out timeUtc);
            if (succeeded)
                return (timeUtc, succeeded);
            succeeded = DateTime.TryParseExact(dateTimeString, @"dd.MM.yyyy HH:mm:ss.FFFFF", CultureInfo.InvariantCulture,
                               DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out timeUtc);
            if (succeeded)
                return (timeUtc, succeeded);
            succeeded = DateTime.TryParseExact(dateTimeString, @"yyyy.MM.dd H:mm:ss.FFFFF", CultureInfo.InvariantCulture,
                               DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out timeUtc);
            if (succeeded)
                return (timeUtc, succeeded);
            succeeded = DateTime.TryParseExact(dateTimeString, @"yyyy.MM.dd HH:mm:ss.FFFFF", CultureInfo.InvariantCulture,
                               DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out timeUtc);
            if (succeeded)
                return (timeUtc, succeeded);


            succeeded = DateTime.TryParseExact(dateTimeString, dateTimeFormatOption, CultureInfo.InvariantCulture,
                               DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out timeUtc);
            if (succeeded)
                return (timeUtc, succeeded);
            //var index = dateTimeFormatOption.IndexOf(".F");
            //if (index != -1)
            //{
            //    dateTimeFormatOption = dateTimeFormatOption.Substring(0, index + 1);
            //    foreach (int i in Enumerable.Range(0, 5))
            //    {
            //        dateTimeFormatOption += "F";
            //        succeeded = DateTime.TryParseExact(dateTimeString, dateTimeFormatOption, CultureInfo.InvariantCulture,
            //                        DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out timeUtc);
            //        if (succeeded)
            //            return (timeUtc, succeeded);
            //    }
            //}
            succeeded = DateTime.TryParse(dateTimeString, CultureInfo.InvariantCulture,
                                    DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out timeUtc);
            if (succeeded)
                return (timeUtc, succeeded);
            return (DateTime.MinValue, false);
        }
    }
}
