using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils
{
    public static class AnyHelper
    {
        /// <summary>
        ///     Gets best transport type.
        /// </summary>
        public static TransportType GetTransportType(Any any)
        {
            switch (any.ValueTypeCode)
            {
                case Any.TypeCode.SByte:
                    return TransportType.UInt32;
                case Any.TypeCode.Byte:
                    return TransportType.UInt32;
                case Any.TypeCode.Int16:
                    return TransportType.UInt32;
                case Any.TypeCode.UInt16:
                    return TransportType.UInt32;
                case Any.TypeCode.Int32:
                    return TransportType.UInt32;
                case Any.TypeCode.UInt32:
                    return TransportType.UInt32;
                case Any.TypeCode.Boolean:
                    return TransportType.UInt32;
                case Any.TypeCode.Char:
                    return TransportType.UInt32;
                case Any.TypeCode.Single:
                    return TransportType.Double;
                case Any.TypeCode.Double:
                    return TransportType.Double;
                case Any.TypeCode.Empty:
                    return TransportType.Object;
                case Any.TypeCode.DBNull:
                    return TransportType.Object;
                case Any.TypeCode.Int64:
                    return TransportType.Object;
                case Any.TypeCode.UInt64:
                    return TransportType.Object;
                case Any.TypeCode.Decimal:
                    return TransportType.Object;
                case Any.TypeCode.DateTimeOffset:
                    return TransportType.Object;
                case Any.TypeCode.String:
                    return TransportType.Object;
                case Any.TypeCode.Dictionary:
                    return TransportType.Object;
                case Any.TypeCode.List:
                    return TransportType.Object;
                case Any.TypeCode.Object:
                    return TransportType.Object;
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transportUInt32"></param>
        /// <param name="typeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        public static Any GetAny(UInt32 transportUInt32, Any.TypeCode typeCode, bool stringIsLocalized)
        {
            var any = new Any();
            switch (typeCode)
            {
                case Any.TypeCode.SByte:
                    any.Set((SByte)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.Byte:
                    any.Set((Byte)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.Int16:
                    any.Set((Int16)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.UInt16:
                    any.Set((UInt16)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.Int32:
                    any.Set((Int32)transportUInt32);
                    return any;
                case Any.TypeCode.UInt32:
                    any.Set(transportUInt32);
                    return any;
                case Any.TypeCode.Boolean:
                    any.Set(transportUInt32 != 0u);
                    return any;
                case Any.TypeCode.Char:
                    any.Set((Char)transportUInt32);
                    return any;
                case Any.TypeCode.Single:
                    any.Set((Single)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.Double:
                    any.Set((Double)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.Int64:
                    any.Set((Int64)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.UInt64:
                    any.Set((UInt64)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.Decimal:
                    any.Set((Decimal)(Int32)transportUInt32);
                    return any;
                case Any.TypeCode.String:
                    any.Set(((Int32)transportUInt32).ToString(Any.GetCultureInfo(stringIsLocalized)));
                    return any;
                default:
                    any.Set((Int32)transportUInt32);
                    return any;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transportDouble"></param>
        /// <param name="typeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        public static Any GetAny(Double transportDouble, Any.TypeCode typeCode, bool stringIsLocalized)
        {
            var any = new Any();
            switch (typeCode)
            {
                case Any.TypeCode.Single:
                    any.Set((Single)transportDouble);
                    return any;
                case Any.TypeCode.Double:
                    any.Set(transportDouble);
                    return any;
                default:
                    any.Set(transportDouble);
                    return any;
            }
        }
    }

    /// <summary>        
    /// </summary>
    public enum TransportType : byte
    {
        /// <summary>
        ///     The data value is / was transported as an object.
        /// </summary>
        Object,

        /// <summary>
        ///     The data value is / was transported as a double (64 Bits).
        /// </summary>
        Double,

        /// <summary>
        ///     The data value is / was transported as a uint (32 Bits).
        /// </summary>
        UInt32
    };
}
