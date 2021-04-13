using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class ValueStatusTimestamp
    { 
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
            sb.Append(StatusCode.ToString("X4"));
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
        public static bool operator ==(ValueStatusTimestamp? left, ValueStatusTimestamp? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(null, left) || ReferenceEquals(null, right)) return false;            
            return left.Equals(right);
        }

        /// <summary>        
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(ValueStatusTimestamp? left, ValueStatusTimestamp? right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            ValueStatusTimestamp? other = obj as ValueStatusTimestamp;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public bool Equals(ValueStatusTimestamp that)
        {            
            return StatusCode == that.StatusCode && TimestampUtc == that.TimestampUtc && Value == that.Value;
        }

        /// <summary>
        ///     Uses ValueAsDouble(false), ValueAsInt32(false), ValueAsString(false) depending of ValueStorageType.
        ///     Returns true if diff is less than or equal deadband.
        /// </summary>
        /// <param name="that"></param>
        /// <returns></returns>
        public bool Compare(ValueStatusTimestamp that, double deadband = 0.0)
        {
            return StatusCode == that.StatusCode && TimestampUtc == that.TimestampUtc && Value.Compare(that.Value, deadband);
        }

        public Any Value;
        
        public uint StatusCode;
        
        public DateTime TimestampUtc;

        #endregion
    }

    public static class StatusCodes
    {
        public const uint Unknown = 0;

        public const uint ItemDoesNotExist = 1;

        public const uint Good = 2;
    }
}
