using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client
{
    internal static class DateTimeHelper
    {
        public static Timestamp ConvertToTimestamp(DateTime dateTimeUtc)
        {
            if (dateTimeUtc < DateTime.FromFileTimeUtc(0))
                dateTimeUtc = DateTime.FromFileTimeUtc(0);
            else if (dateTimeUtc > DateTime.FromFileTimeUtc(0) + TimeSpan.FromDays(365 * 1000))
                dateTimeUtc = DateTime.FromFileTimeUtc(0) + TimeSpan.FromDays(365 * 1000);
            try
            {
                return Timestamp.FromDateTime(dateTimeUtc);
            }
            catch
            {
                return new Timestamp();
            }
        }
    }
}
