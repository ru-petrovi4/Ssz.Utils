using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public struct ValueStatusTimestamp: IOwnedDataSerializable
    {
        #region construction and destruction

        public ValueStatusTimestamp(Any value, uint statusCode, DateTime timestampUtc)
        {
            Value = value;
            StatusCode = statusCode;
            TimestampUtc = timestampUtc;
        }

        /// <summary>
        ///     StatusCode.Good
        /// </summary>
        /// <param name="value"></param>
        /// <param name="timestampUtc"></param>
        public ValueStatusTimestamp(Any value, DateTime timestampUtc)
        {
            Value = value;
            StatusCode = StatusCodes.Good;
            TimestampUtc = timestampUtc;
        }

        /// <summary>
        ///     StatusCode.Good, DateTime.UtcNow
        /// </summary>
        /// <param name="value"></param>
        public ValueStatusTimestamp(Any value)
        {
            Value = value;
            StatusCode = StatusCodes.Good;
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
            if (obj is ValueStatusTimestamp valueStatusTimestamp)
                return Equals(valueStatusTimestamp, 0.0);
            return false;
        }        

        /// <summary>
        ///     Uses ValueAsDouble(false), ValueAsUInt32(false), ValueAsString(false) depending of ValueTransportType.
        ///     Returns true if diff is less than or equal deadband.
        ///     TimestampUtc is NOT compared.
        /// </summary>
        /// <param name="that"></param>
        /// <param name="deadband"></param>
        /// <returns></returns>
        public bool Equals(ValueStatusTimestamp that, double deadband)
        {
            return StatusCode == that.StatusCode && Value.CompareTo(that.Value, deadband) == 0;
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.WriteOwnedDataSerializable(Value, context);
            writer.Write(StatusCode);
            writer.Write(TimestampUtc);
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            reader.ReadOwnedDataSerializable(Value, context);
            StatusCode = reader.ReadUInt32();
            TimestampUtc = reader.ReadDateTime();
        }

        public Any Value;

        /// <summary>
        ///     See consts in <see cref="StatusCodes"/>
        /// </summary>
        public uint StatusCode;
        
        public DateTime TimestampUtc;        

        #endregion
    }
}
