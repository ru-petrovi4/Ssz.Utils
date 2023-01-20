using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public struct ValueStatusTimestamp
    {
        #region construction and destruction

        public ValueStatusTimestamp(Any value, uint valueStatusCode, DateTime timestampUtc)
        {
            Value = value;
            ValueStatusCode = valueStatusCode;
            TimestampUtc = timestampUtc;
        }

        /// <summary>
        ///     ValueStatusCode.Good
        /// </summary>
        /// <param name="value"></param>
        /// <param name="timestampUtc"></param>
        public ValueStatusTimestamp(Any value, DateTime timestampUtc)
        {
            Value = value;
            ValueStatusCode = DataAccess.ValueStatusCodes.Good;
            TimestampUtc = timestampUtc;
        }

        /// <summary>
        ///     ValueStatusCode.Good, DateTime.UtcNow
        /// </summary>
        /// <param name="value"></param>
        public ValueStatusTimestamp(Any value)
        {
            Value = value;
            ValueStatusCode = DataAccess.ValueStatusCodes.Good;
            TimestampUtc = DateTime.UtcNow;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Provides an simple overload for the ToString() method that combines the
        ///     data type, timestamp, status code, and value into a single string.
        /// </summary>
        /// <returns> </returns>
        public override string ToString()
        {
            var sb = new StringBuilder(Value.ValueType.ToString());
            sb.Append(", V = ");
            sb.Append(Value);
            sb.Append(", TS = ");
            sb.Append(TimestampUtc.ToString("u"));
            sb.Append(", SC = 0x");
            sb.Append(ValueStatusCode.ToString("X4"));
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return 0;
        }

        /// <summary>        
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(ValueStatusTimestamp left, ValueStatusTimestamp right)
        {
            return left.Equals(right);
        }

        /// <summary>        
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(ValueStatusTimestamp left, ValueStatusTimestamp right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {            
            if (!(obj is ValueStatusTimestamp)) return false;            
            return Equals((ValueStatusTimestamp)obj);
        }

        public bool Equals(ValueStatusTimestamp that)
        {            
            return ValueStatusCode == that.ValueStatusCode && TimestampUtc == that.TimestampUtc && Value == that.Value;
        }

        /// <summary>
        ///     Uses ValueAsDouble(false), ValueAsInt32(false), ValueAsString(false) depending of ValueStorageType.
        ///     Returns true if diff is less than or equal deadband.
        ///     TimestampUtc is NOT compared.
        /// </summary>
        /// <param name="that"></param>
        /// <param name="deadband"></param>
        /// <returns></returns>
        public bool Compare(ValueStatusTimestamp that, double deadband = 0.0)
        {
            return ValueStatusCode == that.ValueStatusCode && Value.Compare(that.Value, deadband);
        }

        public Any Value;

        /// <summary>
        ///     See consts in <see cref="ValueStatusCodes"/>
        /// </summary>
        public uint ValueStatusCode;
        
        public DateTime TimestampUtc;        

        #endregion
    }

    public static class ValueStatusCodes
    {
        public const uint Unknown = 0;

        public const uint ItemDoesNotExist = 1;

        public const uint Good = 2;

        public static bool IsGood(uint valueStatusCode) => valueStatusCode >= Good;
    }
}
