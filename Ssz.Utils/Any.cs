﻿using Ssz.Utils.Serialization; using System; using System.Collections; using System.Collections.Generic;
using System.ComponentModel; using System.Globalization;
using System.IO;
using System.Linq; using System.Runtime.CompilerServices; using System.Runtime.InteropServices; using System.Text.Json; using System.Text.Json.Serialization;

namespace Ssz.Utils {
    /// <summary>     ///     If func param stringIsLocalized = false, CultureInfo.InvariantCulture is used.     ///     If func param stringIsLocalized = true, CultureInfo.CurrentCulture is used.     ///     Double.NaN == Double.NaN, Single.NaN == Single.NaN     /// </summary>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   [StructLayout(LayoutKind.Explicit)]     public struct Any : IOwnedDataSerializable, IComparable<Any>, IComparable     {         #region StorageType enum          public enum TypeCode : byte
        {
            /// <summary>
            ///     A null reference.
            /// </summary>            
            Empty = 0,

            /// <summary>
            ///     A general type representing any reference or value type not explicitly represented
            ///     by another TypeCode.
            /// </summary>
            Object = 1,

            //
            // Summary:
            //     A database null (column) value.
            DBNull = 2,

            /// <summary>
            ///     A simple type representing Boolean values of true or false.
            /// </summary>            
            Boolean = 3,

            /// <summary>
            ///     An integral type representing unsigned 16-bit integers with values between 0
            ///     and 65535. The set of possible values for the System.TypeCode.Char type corresponds
            ///     to the Unicode character set.
            /// </summary>
            Char = 4,

            /// <summary>
            ///     An integral type representing signed 8-bit integers with values between -128
            ///     and 127.
            /// </summary>
            SByte = 5,

            /// <summary>
            ///     An integral type representing unsigned 8-bit integers with values between 0 and
            ///     255.
            /// </summary>
            Byte = 6,

            /// <summary>
            ///     An integral type representing signed 16-bit integers with values between -32768
            ///     and 32767.
            /// </summary>
            Int16 = 7,

            /// <summary>
            ///     An integral type representing unsigned 16-bit integers with values between 0
            ///     and 65535.
            /// </summary>            
            UInt16 = 8,

            /// <summary>
            ///     An integral type representing signed 32-bit integers with values between -2147483648
            ///     and 2147483647.
            /// </summary>
            Int32 = 9,

            /// <summary>
            ///     An integral type representing unsigned 32-bit integers with values between 0
            ///     and 4294967295.
            /// </summary>            
            UInt32 = 10,

            /// <summary>
            ///     An integral type representing signed 64-bit integers with values between -9223372036854775808
            ///     and 9223372036854775807.
            /// </summary>            
            Int64 = 11,

            /// <summary>
            ///     An integral type representing unsigned 64-bit integers with values between 0
            ///     and 18446744073709551615.
            /// </summary>
            UInt64 = 12,

            /// <summary>
            ///     A floating point type representing values ranging from approximately 1.5 x 10
            ///     -45 to 3.4 x 10 38 with a precision of 7 digits.
            /// </summary>
            Single = 13,

            /// <summary>
            ///     A floating point type representing values ranging from approximately 5.0 x 10
            ///     -324 to 1.7 x 10 308 with a precision of 15-16 digits.
            /// </summary>
            Double = 14,

            /// <summary>
            ///     A simple type representing values ranging from 1.0 x 10 -28 to approximately
            ///     7.9 x 10 28 with 28-29 significant digits.
            /// </summary>
            Decimal = 15,

            /// <summary>
            ///     A type representing a date and time value with offset (relative to UTC).
            /// </summary>
            DateTimeOffset = 16,

            /// <summary>
            ///     A sealed class type representing Unicode character strings.
            /// </summary>
            String = 18,

            /// <summary>
            ///     String dictionary of Any.
            /// </summary>
            Dictionary = 19,

            /// <summary>
            ///     List of Any.
            /// </summary>
            List = 20
        }

        #endregion 
        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(object? value)         {                         Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);            
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);         }                  /// <summary>         ///          /// </summary>         /// <param name="that"></param>         public Any(Any that)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(that);                     }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(SByte value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Byte value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Int16 value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(UInt16 value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Int32 value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(UInt32 value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Boolean value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Char value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Single value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Double value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Decimal value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Int64 value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(UInt64 value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(DateTime value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(DateTimeOffset value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>                                                                                                            public Any(DBNull value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(String? value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(Dictionary<string, Any>? value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(List<Any>? value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTimeOffset);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        #region public functions        
        public static CultureInfo GetCultureInfo(bool localized)         {             if (localized)
                return CultureInfo.CurrentCulture;             return CultureInfo.InvariantCulture;         }

        /// <summary>         ///          /// </summary>         public TypeCode ValueTypeCode         {             get { return (TypeCode)_valueTypeCode; }         }          /// <summary>                 /// </summary>         public Type ValueType         {             get             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return typeof (SByte);                     case TypeCode.Byte:                         return typeof (Byte);                     case TypeCode.Int16:                         return typeof (Int16);                     case TypeCode.UInt16:                         return typeof (UInt16);                     case TypeCode.Int32:                         return typeof (Int32);                     case TypeCode.UInt32:                         return typeof (UInt32);                     case TypeCode.Boolean:                         return typeof (Boolean);                     case TypeCode.Single:                         return typeof (Single);                     case TypeCode.Double:                         return typeof (Double);                     case TypeCode.Empty:                         return typeof (Object);                     case TypeCode.DBNull:                         return typeof (DBNull);                     case TypeCode.Int64:                         return typeof (Int64);                     case TypeCode.UInt64:                         return typeof (UInt64);                     case TypeCode.Decimal:                         return typeof (Decimal);                     case TypeCode.DateTimeOffset:                         return typeof (DateTimeOffset);                     case TypeCode.String:                         return typeof (String);                     case TypeCode.Char:                         return typeof (Char);                     case TypeCode.Dictionary:                         return typeof(Dictionary<string, Any>);                     case TypeCode.List:                         return typeof(List<Any>);                     case TypeCode.Object:                                                 return StorageObject!.GetType();                 }                 throw new InvalidOperationException();             }         }

        /// <summary>                 /// </summary>         /// <param name="sValue"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public static Any ConvertToBestType(string? sValue, bool stringIsLocalized)         {             if (sValue is null) return                  new Any();             if (String.IsNullOrWhiteSpace(sValue)) return new Any(sValue);             switch (sValue.ToUpperInvariant())             {                 case "TRUE":                     return new Any(true);                 case "FALSE":                     return new Any(false);             }             Double dValue;             bool parsedAsDouble = Double.TryParse(sValue, NumberStyles.Any,                 GetCultureInfo(stringIsLocalized), out dValue);             if (!parsedAsDouble) return new Any(sValue);             bool isInt32 = Math.Abs(dValue % 1) <= Double.Epsilon * 100;             if (isInt32)             {                 isInt32 = Int32.MinValue <= dValue && dValue <= Int32.MaxValue;                 if (isInt32) return new Any((int)dValue);             }             return new Any(dValue);         }          /// <summary>                 /// </summary>         /// <param name="left"></param>         /// <param name="right"></param>         /// <returns></returns>         public static bool operator ==(Any left, Any right)         {             if (left._valueTypeCode != right._valueTypeCode)                 return false;              switch ((TypeCode)left._valueTypeCode)
            {
                case TypeCode.SByte:
                    return left.StorageSByte == right.StorageSByte;                    
                case TypeCode.Byte:
                    return left.StorageByte == right.StorageByte;
                case TypeCode.Int16:
                    return left.StorageInt16 == right.StorageInt16;
                case TypeCode.UInt16:
                    return left.StorageUInt16 == right.StorageUInt16;
                case TypeCode.Int32:
                    return left.StorageInt32 == right.StorageInt32;
                case TypeCode.UInt32:
                    return left.StorageUInt32 == right.StorageUInt32;
                case TypeCode.Boolean:
                    return left.StorageBoolean == right.StorageBoolean;
                case TypeCode.Char:
                    return left.StorageChar == right.StorageChar;
                case TypeCode.Single:
                    if (Single.IsNaN(left.StorageSingle) && Single.IsNaN(right.StorageSingle))
                        return true;
                    return left.StorageSingle == right.StorageSingle;                    
                case TypeCode.Double:
                    if (Double.IsNaN(left.StorageDouble) && Double.IsNaN(right.StorageDouble))
                        return true;
                    return left.StorageDouble == right.StorageDouble;                    
                case TypeCode.Empty:
                    return true;
                case TypeCode.DBNull:
                    return true;
                case TypeCode.Int64:
                    return left.StorageInt64 == right.StorageInt64;
                case TypeCode.UInt64:
                    return left.StorageUInt64 == right.StorageUInt64;
                case TypeCode.Decimal:
                    return left.StorageDecimal == right.StorageDecimal;
                case TypeCode.DateTimeOffset:
                    return left.StorageDateTimeOffset == right.StorageDateTimeOffset;
                case TypeCode.String:
                    return (string)left.StorageObject! == (string)right.StorageObject!;
                case TypeCode.Dictionary:
                    return ((Dictionary<string, Any>)left.StorageObject!).SequenceEqual((Dictionary<string, Any>)right.StorageObject!);
                case TypeCode.List:
                    return ((List<Any>)left.StorageObject!).SequenceEqual((List<Any>)right.StorageObject!);
                case TypeCode.Object:
                    try
                    {                        
                        if (left.StorageObject is IStructuralEquatable leftStructuralEquatable)
                            return leftStructuralEquatable.Equals(right.StorageObject, StructuralComparisons.StructuralEqualityComparer);                        
                        return left.StorageObject == right.StorageObject;
                    }
                    catch
                    {
                        return false;
                    }
                default:
                    throw new InvalidOperationException();
            }         }          /// <summary>                 /// </summary>         /// <param name="left"></param>         /// <param name="right"></param>         /// <returns></returns>         public static bool operator !=(Any left, Any right)         {             return !(left == right);         }

        /// <summary>         ///     Compares the current instance with another object of the same type and returns an integer that indicates          ///     whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.         ///     Uses ValueAsDouble(false), ValueAsUInt32(false), ValueAsString(false) depending of ValueTransportType.          ///     Warning! NaN equals NaN!         /// </summary>         /// <param name="that"></param>         /// <param name="deadband"></param>         /// <returns></returns>         public int CompareTo(Any that, double deadband)         {
            switch ((TypeCode)_valueTypeCode)
            {
                case TypeCode.SByte:
                    return ValueAs<SByte>(false).CompareTo(that.ValueAs<SByte>(false));
                case TypeCode.Byte:
                    return ValueAs<Byte>(false).CompareTo(that.ValueAs<Byte>(false));
                case TypeCode.Int16:
                    return ValueAs<Int16>(false).CompareTo(that.ValueAs<Int16>(false));
                case TypeCode.UInt16:
                    return ValueAs<UInt16>(false).CompareTo(that.ValueAs<UInt16>(false));
                case TypeCode.Int32:
                    return ValueAsInt32(false).CompareTo(that.ValueAsInt32(false));
                case TypeCode.UInt32:
                    return ValueAsUInt32(false).CompareTo(that.ValueAsUInt32(false));
                case TypeCode.Boolean:
                    return ValueAsBoolean(false).CompareTo(that.ValueAsBoolean(false));
                case TypeCode.Char:
                    return ValueAs<Char>(false).CompareTo(that.ValueAs<Char>(false));
                case TypeCode.Single:
                    {                         float d = ValueAsSingle(false);
                        float thatD = that.ValueAsSingle(false);
                        if (Single.IsNaN(d) && Single.IsNaN(thatD))
                            return 0;
                        float diff = d - thatD;
                        float deadbandF = (float)deadband + Single.Epsilon * 100;
                        if (diff > -deadbandF &&
                                diff < deadbandF)
                            return 0;
                        if (diff <= -deadbandF)
                            return -1;
                        if (diff >= deadbandF)
                            return 1;
                        return -1;
                    }
                case TypeCode.Double:
                    {                         double d = ValueAsDouble(false);
                        double thatD = that.ValueAsDouble(false);
                        if (Double.IsNaN(d) && Double.IsNaN(thatD))
                            return 0;
                        double diff = d - thatD;
                        deadband = deadband + Single.Epsilon * 100;
                        if (diff >= -deadband &&
                                diff <= deadband)
                            return 0;
                        if (diff < -deadband)
                            return -1;
                        if (diff > deadband)
                            return 1;
                        return -1;
                    }
                case TypeCode.Empty:
                    return that.ValueTypeCode == TypeCode.Empty ? 0 : 1;
                case TypeCode.DBNull:
                    return that.ValueTypeCode == TypeCode.DBNull ? 0 : 1;
                case TypeCode.Int64:
                    return ValueAsInt64(false).CompareTo(that.ValueAsInt64(false));
                case TypeCode.UInt64:
                    return ValueAsUInt64(false).CompareTo(that.ValueAsUInt64(false));
                case TypeCode.Decimal:
                    return ValueAs<Decimal>(false).CompareTo(that.ValueAs<Decimal>(false));
                case TypeCode.DateTimeOffset:
                    return ValueAs<DateTimeOffset>(false).CompareTo(that.ValueAs<DateTimeOffset>(false));
                case TypeCode.String:
                    return ValueAsString(false).CompareTo(that.ValueAsString(false));
                case TypeCode.Dictionary:
                    return ValueAsDictionary().SequenceEqual(that.ValueAsDictionary()) ? 0 : 1;
                case TypeCode.List:
                    return ValueAsList().SequenceEqual(that.ValueAsList()) ? 0 : 1;
                case TypeCode.Object:
                    try
                    {
                        var thatObject = that.ValueAsObject();
                        if (thatObject is null)
                            return -1;
                        if (StorageObject is IStructuralComparable thisStructuralComparable)
                            return thisStructuralComparable.CompareTo(thatObject, StructuralComparisons.StructuralComparer);
                        if (StorageObject is IComparable thisComparable)
                            return thisComparable.CompareTo(thatObject);
                        return ValueAsString(false).CompareTo(that.ValueAsString(false));
                    }      
                    catch
                    {
                        return -1;
                    }
                default:
                    throw new InvalidOperationException();
            }         }        

        /// <summary>         ///     Compares the current instance with another object of the same type and returns an integer that indicates          ///     whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.         ///     Uses ValueAsDouble(false), ValueAsUInt32(false), ValueAsString(false) depending of ValueTransportType.                 /// </summary>         /// <param name="that"></param>                 /// <returns></returns>         public int CompareTo(Any that)         {
            return CompareTo(that, 0.0);         }

        public int CompareTo(object? obj)
        {
            if (obj is Any any)
                return CompareTo(any, 0.0);
            throw new ArgumentException(nameof(obj));
        }

        /// <summary>         ///     Strictly copare, no conversions         /// </summary>         /// <param name="obj"></param>         /// <returns></returns>         public override bool Equals(object? obj)         {             if (obj is null || !(obj is Any)) return false;              return this == (Any) obj;         }          /// <summary>         ///     Strictly copare, no conversions         /// </summary>         /// <param name="that"></param>         /// <returns></returns>         public bool Equals(Any that)         {             return this == that;         }          /// <summary>                 /// </summary>         /// <returns></returns>         public override int GetHashCode()         {             return 0;             //unchecked
            //{
            //    int hashCode = _valueTypeCode;
            //    hashCode = (hashCode * 397) ^ (int)_storageUInt32;
            //    hashCode = (hashCode * 397) ^ _storageDouble.GetHashCode();
            //    hashCode = (hashCode * 397) ^ _storageObject.GetHashCode();
            //    return hashCode;
            //}         }          /// <summary>                 /// </summary>         /// <returns></returns>         public override string ToString()         {                         return ValueAsString(false);         }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(object? value)         {             if (value is null)
            {
                _valueTypeCode = (byte)TypeCode.Empty;
                StorageObject = null;
                return;
            }             if (value is Dictionary<string, Any>)             {                 _valueTypeCode = (byte)TypeCode.Dictionary;
                StorageObject = value;                 return;             }             if (value is List<Any>)             {                 _valueTypeCode = (byte)TypeCode.List;
                StorageObject = value;                 return;             }              Type valueType = value.GetType();              if (valueType.IsEnum)             {                 _valueTypeCode = (byte)TypeCode.Object;
                StorageObject = value;                 return;             }

            switch (valueType.FullName)             {                 case @"Ssz.Utils.Any":                     Set((Any)value);                     return;                 case @"System.SByte":                     Set((SByte)value);                     return;                 case @"System.Byte":                     Set((Byte)value);                     return;                 case @"System.Int16":                     Set((Int16)value);                     return;                 case @"System.UInt16":                     Set((UInt16)value);                     return;                 case @"System.Int32":                     Set((Int32)value);                     return;                 case @"System.UInt32":                     Set((UInt32)value);                     return;                 case @"System.Boolean":                     Set((Boolean)value);                     return;                 case @"System.Single":                     Set((Single)value);                     return;                 case @"System.Double":                     Set((Double)value);                     return;
                case @"System.DBNull":                     Set((DBNull)value);                     return;                 case @"System.Int64":                     Set((Int64)value);                     return;                 case @"System.UInt64":                     Set((UInt64)value);                     return;                 case @"System.Decimal":                     Set((Decimal)value);                     return;                 case @"System.DateTime":                     Set((DateTime)value);                     return;                 case @"System.Char":                     Set((Char)value);                     return;
                case @"System.String":                     _valueTypeCode = (byte)TypeCode.String;
                    StorageObject = value;                     return;
                default:                     _valueTypeCode = (byte)TypeCode.Object;
                    StorageObject = value;                     return;             }
        }

        /// <summary>         /// </summary>         public void Set(Any value)         {             _valueTypeCode = value._valueTypeCode;             switch ((TypeCode)_valueTypeCode)
            {
                case TypeCode.SByte:
                    StorageSByte = value.StorageSByte;
                    StorageObject = null;
                    break;
                case TypeCode.Byte:
                    StorageByte = value.StorageByte;
                    StorageObject = null;
                    break;
                case TypeCode.Int16:
                    StorageInt16 = value.StorageInt16;
                    StorageObject = null;
                    break;
                case TypeCode.UInt16:
                    StorageUInt16 = value.StorageUInt16;
                    StorageObject = null;
                    break;
                case TypeCode.Int32:
                    StorageInt32 = value.StorageInt32;
                    StorageObject = null;
                    break;
                case TypeCode.UInt32:
                    StorageUInt32 = value.StorageUInt32;
                    StorageObject = null;
                    break;
                case TypeCode.Boolean:
                    StorageBoolean = value.StorageBoolean;
                    StorageObject = null;
                    break;
                case TypeCode.Char:
                    StorageChar = value.StorageChar;
                    StorageObject = null;
                    break;
                case TypeCode.Single:                    
                    StorageSingle = value.StorageSingle;
                    StorageObject = null;
                    break;
                case TypeCode.Double:                    
                    StorageDouble = value.StorageDouble;
                    StorageObject = null;
                    break;
                case TypeCode.Empty:
                    StorageObject = null;
                    break;
                case TypeCode.DBNull:
                    StorageObject = DBNull.Value;
                    break;
                case TypeCode.Int64:
                    StorageInt64 = value.StorageInt64;
                    StorageObject = null;
                    break;
                case TypeCode.UInt64:
                    StorageUInt64 = value.StorageUInt64;
                    StorageObject = null;
                    break;
                case TypeCode.Decimal:
                    StorageDecimal = value.StorageDecimal;
                    StorageObject = null;
                    break;
                case TypeCode.DateTimeOffset:
                    StorageDateTimeOffset = value.StorageDateTimeOffset;
                    StorageObject = null;
                    break;
                case TypeCode.String:
                    StorageObject = value.StorageObject;
                    break;
                case TypeCode.Dictionary:
                    StorageObject = value.StorageObject;
                    break;
                case TypeCode.List:
                    StorageObject = value.StorageObject;
                    break;
                case TypeCode.Object:
                    StorageObject = value.StorageObject;
                    break;
                default:
                    throw new InvalidOperationException();
            }                     }                  /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(SByte value)         {             _valueTypeCode = (byte)TypeCode.SByte;             StorageSByte = value;                         StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Byte value)         {             _valueTypeCode = (byte)TypeCode.Byte;             StorageByte = value;                         StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Int16 value)         {             _valueTypeCode = (byte)TypeCode.Int16;             StorageInt16 = value;                         StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(UInt16 value)         {             _valueTypeCode = (byte)TypeCode.UInt16;             StorageUInt16 = value;                         StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Int32 value)         {             _valueTypeCode = (byte)TypeCode.Int32;             StorageInt32 = value;                         StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(UInt32 value)         {             _valueTypeCode = (byte)TypeCode.UInt32;             StorageUInt32 = value;                         StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Boolean value)         {             _valueTypeCode = (byte)TypeCode.Boolean;             StorageBoolean = value;                         StorageObject = null;         }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Char value)         {             _valueTypeCode = (byte)TypeCode.Char;             StorageChar = value;
            StorageObject = null;         }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Single value)         {             _valueTypeCode = (byte)TypeCode.Single;
            StorageSingle = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Double value)         {             _valueTypeCode = (byte)TypeCode.Double;                         StorageDouble = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Decimal value)         {             _valueTypeCode = (byte)TypeCode.Decimal;
            StorageDecimal = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Int64 value)         {             _valueTypeCode = (byte)TypeCode.Int64;
            StorageInt64 = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(UInt64 value)         {             _valueTypeCode = (byte)TypeCode.UInt64;
            StorageUInt64 = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(DateTime value)         {             _valueTypeCode = (byte)TypeCode.DateTimeOffset;
            StorageDateTimeOffset = value;             StorageObject = null;         }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(DateTimeOffset value)         {             _valueTypeCode = (byte)TypeCode.DateTimeOffset;
            StorageDateTimeOffset = value;             StorageObject = null;         }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(DBNull value)         {             _valueTypeCode = (byte)TypeCode.DBNull;                         StorageObject = value;         }                  /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(String? value)         {
            if (value is null)
            {
                _valueTypeCode = (byte)TypeCode.Empty;
                StorageObject = null;
                return;
            }             _valueTypeCode = (byte)TypeCode.String;                         StorageObject = value;         }          public void Set(Dictionary<string, Any>? value)         {
            if (value is null)
            {
                _valueTypeCode = (byte)TypeCode.Empty;
                StorageObject = null;
                return;
            }
            _valueTypeCode = (byte)TypeCode.Dictionary;
            StorageObject = value;         }

        public void Set(List<Any>? value)         {
            if (value is null)
            {
                _valueTypeCode = (byte)TypeCode.Empty;
                StorageObject = null;
                return;
            }
            _valueTypeCode = (byte)TypeCode.List;
            StorageObject = value;         }

        /// <summary>         ///          /// </summary>         /// <returns></returns>         public object? ValueAsObject()         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return StorageSingle;                     case TypeCode.Double:                         return StorageDouble;                     case TypeCode.Empty:                         return null;                     case TypeCode.DBNull:                         return StorageObject;                     case TypeCode.Int64:                         return StorageInt64;                     case TypeCode.UInt64:                         return StorageUInt64;                     case TypeCode.Decimal:                         return StorageDecimal;                     case TypeCode.DateTimeOffset:                         return StorageDateTimeOffset;                                         case TypeCode.String:                         return StorageObject;                     case TypeCode.Dictionary:                         return StorageObject;                     case TypeCode.List:                         return StorageObject;                     case TypeCode.Object:                         return StorageObject;                 }             }             catch (Exception)             {                 return null;             }              throw new InvalidOperationException();         }          /// <summary>                 /// </summary>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public String ValueAsString(bool stringIsLocalized, string? stringFormat = null)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Byte:                         return StorageByte.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Int16:                         return StorageInt16.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt16:                         return StorageUInt16.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Int32:                         return StorageInt32.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt32:                         return StorageUInt32.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Boolean:                         return StorageBoolean.ToString(GetCultureInfo(stringIsLocalized));                     case TypeCode.Char:                         return StorageChar.ToString();                     case TypeCode.Single:                         return StorageSingle.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Double:                         return StorageDouble.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Empty:                         return String.Empty;                     case TypeCode.DBNull:                                                 return String.Empty; // ((DBNull)StorageObject!).ToString(GetCultureInfo(stringIsLocalized));
                    case TypeCode.Int64:                                                 return StorageInt64.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt64:                                                 return StorageUInt64.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Decimal:                                                 return StorageDecimal.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.DateTimeOffset:                                                 return StorageDateTimeOffset.ToString(stringFormat ?? @"O", GetCultureInfo(stringIsLocalized));                                             case TypeCode.String:                                                 return (String)StorageObject!;
                    case TypeCode.Dictionary:                         {
                            var options = new JsonSerializerOptions
                            {
                                Converters = { new ToStringJsonConverter() },
                                WriteIndented = false,
                                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                            };
                            return JsonSerializer.Serialize(StorageObject!, options);
                        }                                             case TypeCode.List:                         {
                            var options = new JsonSerializerOptions
                            {
                                Converters = { new ToStringJsonConverter() },
                                WriteIndented = false,
                                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                            };
                            return JsonSerializer.Serialize(StorageObject!, options);
                        }
                    case TypeCode.Object:                         return TypeCodeObject_ValueAsString(StorageObject, stringIsLocalized, stringFormat);                 }             }             catch (Exception)             {                 return String.Empty;             }              throw new InvalidOperationException();         }          /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public Int32 ValueAsInt32(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return (Int32) StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1 : 0;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return (Int32)StorageSingle;                     case TypeCode.Double:                         return (Int32)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (Int32)StorageInt64;                     case TypeCode.UInt64:                                                 return (Int32)StorageUInt64;                     case TypeCode.Decimal:                                                 return (Int32)StorageDecimal;                     case TypeCode.DateTimeOffset:                         return 0;                     case TypeCode.String:                                                 Int32 result;                         if (!Int32.TryParse((string)StorageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(int), stringIsLocalized);                         if (obj is null)                              return 0;                         else return (int)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public UInt32 ValueAsUInt32(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return (UInt32)StorageSByte;                     case TypeCode.Byte:                         return (UInt32)StorageByte;                     case TypeCode.Int16:                         return (UInt32)StorageInt16;                     case TypeCode.UInt16:                         return (UInt32)StorageUInt16;                     case TypeCode.Int32:                         return (UInt32)StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1u : 0u;                     case TypeCode.Char:                         return (UInt32)StorageChar;                     case TypeCode.Single:                         return (UInt32)StorageSingle;                     case TypeCode.Double:                         return (UInt32)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (UInt32)StorageInt64;                     case TypeCode.UInt64:                                                 return (UInt32)StorageUInt64;                     case TypeCode.Decimal:                                                 return (UInt32)StorageDecimal;                     case TypeCode.DateTimeOffset:                         return 0;                     case TypeCode.String:                                                 UInt32 result;                         if (!UInt32.TryParse((string)StorageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(UInt32), stringIsLocalized);                         if (obj is null) return 0;                         else return (UInt32)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          public Int64 ValueAsInt64(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return (Int64)StorageSByte;                     case TypeCode.Byte:                         return (Int64)StorageByte;                     case TypeCode.Int16:                         return (Int64)StorageInt16;                     case TypeCode.UInt16:                         return (Int64)StorageUInt16;                     case TypeCode.Int32:                         return (Int64)StorageInt32;                     case TypeCode.UInt32:                         return (Int64)StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1L : 0L;                     case TypeCode.Char:                         return (Int64)StorageChar;                     case TypeCode.Single:                         return (Int64)StorageSingle;                     case TypeCode.Double:                         return (Int64)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:
                        return (Int64)StorageInt64;                     case TypeCode.UInt64:
                        return (Int64)StorageUInt64;                     case TypeCode.Decimal:
                        return (Int64)StorageDecimal;                     case TypeCode.DateTimeOffset:                         return 0;                     case TypeCode.String:
                        Int64 result;                         if (!Int64.TryParse((string)StorageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:
                        object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(Int64), stringIsLocalized);                         if (obj is null)                             return 0;                         else return (Int64)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          public UInt64 ValueAsUInt64(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return (UInt64)StorageSByte;                     case TypeCode.Byte:                         return (UInt64)StorageByte;                     case TypeCode.Int16:                         return (UInt64)StorageInt16;                     case TypeCode.UInt16:                         return (UInt64)StorageUInt16;                     case TypeCode.Int32:                         return (UInt64)StorageInt32;                     case TypeCode.UInt32:                         return (UInt64)StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1ul : 0ul;                     case TypeCode.Char:                         return (UInt64)StorageChar;                     case TypeCode.Single:                         return (UInt64)StorageSingle;                     case TypeCode.Double:                         return (UInt64)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (UInt64)StorageInt64;                     case TypeCode.UInt64:                                                 return (UInt64)StorageUInt64;                     case TypeCode.Decimal:                                                 return (UInt64)StorageDecimal;                     case TypeCode.DateTimeOffset:                         return 0;                     case TypeCode.String:                                                 UInt64 result;                         if (!UInt64.TryParse((string)StorageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(UInt64), stringIsLocalized);                         if (obj is null)                              return 0;                         else return (UInt64)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }

        /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public float ValueAsSingle(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1f : 0f;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return StorageSingle;                     case TypeCode.Double:                         return (float)StorageDouble;                     case TypeCode.Empty:                         return 0.0f;                     case TypeCode.DBNull:                         return 0.0f;                     case TypeCode.Int64:
                        return StorageInt64;                     case TypeCode.UInt64:
                        return StorageUInt64;                     case TypeCode.Decimal:
                        return (float)StorageDecimal;                     case TypeCode.DateTimeOffset:                         return 0.0f;                     case TypeCode.String:
                        return ConvertToSingle((string)StorageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return 0.0f;                     case TypeCode.List:                         return 0.0f;                     case TypeCode.Object:
                        object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(float), stringIsLocalized);                         if (obj is null)                             return 0.0f;                         else return (float)obj;                 }             }             catch (Exception)             {                 return 0.0f;             }              throw new InvalidOperationException();         }

        /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public Double ValueAsDouble(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1d : 0d;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return StorageSingle;                     case TypeCode.Double:                         return StorageDouble;                     case TypeCode.Empty:                         return 0.0d;                     case TypeCode.DBNull:                         return 0.0d;                     case TypeCode.Int64:                                                 return StorageInt64;                     case TypeCode.UInt64:                                                 return StorageUInt64;                     case TypeCode.Decimal:                                                 return (Double)StorageDecimal;                     case TypeCode.DateTimeOffset:                         return 0.0d;                     case TypeCode.String:                                                 return ConvertToDouble((string)StorageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return 0.0d;                     case TypeCode.List:                         return 0.0d;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(double), stringIsLocalized);                         if (obj is null)                             return 0.0d;                         else return (double)obj;                 }             }             catch (Exception)             {                 return 0.0d;             }              throw new InvalidOperationException();         }

        /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public decimal ValueAsDecimal(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1 : 0;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return (decimal)StorageSingle;                     case TypeCode.Double:                         return (decimal)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:
                        return StorageInt64;                     case TypeCode.UInt64:
                        return StorageUInt64;                     case TypeCode.Decimal:
                        return StorageDecimal;                     case TypeCode.DateTimeOffset:                         return 0;                     case TypeCode.String:
                        return ConvertToDecimal((string)StorageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:
                        object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(decimal), stringIsLocalized);                         if (obj is null)                             return 0;                         else return (decimal)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }

        /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public bool ValueAsBoolean(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte != 0;                     case TypeCode.Byte:                         return StorageByte != 0;                     case TypeCode.Int16:                         return StorageInt16 != 0;                     case TypeCode.UInt16:                         return StorageUInt16 != 0;                     case TypeCode.Int32:                         return StorageInt32 != 0;                     case TypeCode.UInt32:                         return StorageUInt32 != 0;                     case TypeCode.Boolean:                         return StorageBoolean;                     case TypeCode.Char:                         return StorageChar != 0;                     case TypeCode.Single:                         return StorageSingle != 0.0 && !Single.IsNaN(StorageSingle);                     case TypeCode.Double:                         return StorageDouble != 0.0 && !Double.IsNaN(StorageDouble);                     case TypeCode.Empty:                         return false;                     case TypeCode.DBNull:                         return false;                     case TypeCode.Int64:                                                 return StorageInt64 != 0;                     case TypeCode.UInt64:                                                 return StorageUInt64 != 0;                     case TypeCode.Decimal:                                                 return StorageDecimal != (decimal) 0.0;                     case TypeCode.DateTimeOffset:                         return false;                     case TypeCode.String:                                                 return ConvertToBoolean((string)StorageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return false;                     case TypeCode.List:                         return false;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(bool), stringIsLocalized);                         if (obj is null)                             return false;                         else return (bool)obj;                 }             }             catch (Exception)             {                 return false;             }              throw new InvalidOperationException();         }          public Dictionary<string, Any> ValueAsDictionary()         {             if ((TypeCode)_valueTypeCode == TypeCode.Dictionary)                 return (Dictionary<string, Any>)StorageObject!;             else                 return new();         }

        public List<Any> ValueAsList()         {             if ((TypeCode)_valueTypeCode == TypeCode.List)                 return (List<Any>)StorageObject!;             else                 return new();         }

        /// <summary>         ///          /// </summary>         /// <typeparam name="T"></typeparam>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public T? ValueAs<T>(bool stringIsLocalized, string? stringFormat = null)             where T : notnull         {             return (T?)ValueAs(typeof(T), stringIsLocalized, stringFormat);         }          /// <summary>         ///     Returns requested type or null.          /// </summary>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public object? ValueAs(Type? asType, bool stringIsLocalized, string? stringFormat = null)         {             if (asType is null || asType == typeof(object) || asType == ValueType)             {                 return ValueAsObject();             }              if (asType.IsEnum)             {                 if (ValueTypeCode == TypeCode.String)
                {
                    string stringValue = ValueAsString(stringIsLocalized);
                    if (stringValue != @"")
                    {
#if NETSTANDARD
                    try
                    {
                        return Enum.Parse(asType, stringValue, true);
                    }
                    catch
                    {
                    }
#else
                        if (Enum.TryParse(asType, stringValue, true, out object? result))
                            return result;
#endif
                    }
                }
                try
                {
                    return Enum.ToObject(asType, ValueAsInt32(stringIsLocalized));
                }
                catch
                {
                }
                return Activator.CreateInstance(asType);
            }                          if (_valueTypeCode == (byte)TypeCode.Object)             {                                 return TypeCodeObject_ValueAs(StorageObject!, asType, stringIsLocalized, stringFormat);
            }
            
            if (asType.IsGenericType)             {                 return ValueAsObject(asType, stringIsLocalized);
            }

            TypeCode asTypeCode;
            switch (asType.FullName)
            {
                case @"System.SByte":
                    asTypeCode = TypeCode.SByte;
                    break;
                case @"System.Byte":
                    asTypeCode = TypeCode.Byte;
                    break;
                case @"System.Int16":
                    asTypeCode = TypeCode.Int16;
                    break;
                case @"System.UInt16":
                    asTypeCode = TypeCode.UInt16;
                    break;
                case @"System.Int32":
                    return ValueAsInt32(stringIsLocalized);
                case @"System.UInt32":
                    return ValueAsUInt32(stringIsLocalized);
                case @"System.Boolean":
                    return ValueAsBoolean(stringIsLocalized);
                case @"System.Char":
                    asTypeCode = TypeCode.Char;
                    break;
                case @"System.Single":
                    return ValueAsSingle(stringIsLocalized);
                case @"System.Double":
                    return ValueAsDouble(stringIsLocalized);
                case @"System.DBNull":
                    return DBNull.Value;
                case @"System.Int64":
                    return ValueAsInt64(stringIsLocalized);
                case @"System.UInt64":
                    return ValueAsUInt64(stringIsLocalized);
                case @"System.Decimal":
                    return ValueAsDecimal(stringIsLocalized);
                case @"System.DateTimeOffset":
                    return ValueAsDateTimeOffset(stringIsLocalized, stringFormat);
                case @"System.DateTime":
                    return ValueAsDateTimeOffset(stringIsLocalized, stringFormat).UtcDateTime;                    
                case @"System.TimeSpan":
                    return ValueAsTimeSpan(stringIsLocalized, stringFormat);
                case @"System.String":
                    return ValueAsString(stringIsLocalized, stringFormat);
                default:
                    return ValueAsObject(asType, stringIsLocalized);
            }              var destination = new Any();             if (Convert(ref destination, this, asTypeCode, stringIsLocalized, stringFormat))             {                 return destination.ValueAsObject();             }             else             {                 if (!asType.IsAbstract)
                    try                     {                         return Activator.CreateInstance(asType);                     }                     catch (Exception)                     {                     }                 return null;             }         }        

        public void SerializeOwnedData(SerializationWriter writer, object? context)         {             writer.Write(_valueTypeCode);             switch ((TypeCode)_valueTypeCode)
            {
                case TypeCode.SByte:
                    writer.Write(StorageSByte);
                    break;
                case TypeCode.Byte:
                    writer.Write(StorageByte);
                    break;
                case TypeCode.Int16:
                    writer.Write(StorageInt16);
                    break;
                case TypeCode.UInt16:
                    writer.Write(StorageUInt16);
                    break;
                case TypeCode.Int32:
                    writer.Write(StorageInt32);
                    break;
                case TypeCode.UInt32:
                    writer.Write(StorageUInt32);
                    break;
                case TypeCode.Boolean:
                    writer.Write(StorageBoolean);
                    break;
                case TypeCode.Char:
                    writer.Write(StorageChar);
                    break;
                case TypeCode.Single:
                    writer.Write(StorageSingle);
                    break;
                case TypeCode.Double:
                    writer.Write(StorageDouble);
                    break;
                case TypeCode.Empty:
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.Int64:
                    writer.Write(StorageInt64);
                    break;
                case TypeCode.UInt64:
                    writer.Write(StorageUInt64);
                    break;
                case TypeCode.Decimal:
                    writer.Write(StorageDecimal);
                    break;
                case TypeCode.DateTimeOffset:
                    writer.Write(StorageDateTimeOffset.DateTime);
                    writer.Write(StorageDateTimeOffset.Offset);
                    break;
                case TypeCode.String:
                    writer.Write((String)StorageObject!);
                    break;
                case TypeCode.Dictionary:
                    writer.WriteDictionaryOfOwnedDataSerializable((Dictionary<string, Any>)StorageObject!, context);
                    break;
                case TypeCode.List:
                    writer.WriteListOfOwnedDataSerializable((List<Any>)StorageObject!, context);
                    break;
                case TypeCode.Object:
                    writer.WriteObject(StorageObject);
                    break;
                default:
                    throw new InvalidOperationException();
            }         }          public void DeserializeOwnedData(SerializationReader reader, object? context)         {             _valueTypeCode = reader.ReadByte();                         switch ((TypeCode)_valueTypeCode)
            {
                case TypeCode.SByte:
                    StorageSByte = reader.ReadSByte();                     StorageObject = null;
                    break;
                case TypeCode.Byte:
                    StorageByte = reader.ReadByte();                     StorageObject = null;
                    break;
                case TypeCode.Int16:
                    StorageInt16 = reader.ReadInt16();                     StorageObject = null;
                    break;
                case TypeCode.UInt16:
                    StorageUInt16 = reader.ReadUInt16();                     StorageObject = null;
                    break;
                case TypeCode.Int32:
                    StorageInt32 = reader.ReadInt32();                     StorageObject = null;
                    break;
                case TypeCode.UInt32:
                    StorageUInt32 = reader.ReadUInt32();                     StorageObject = null;
                    break;
                case TypeCode.Boolean:
                    StorageBoolean = reader.ReadBoolean();                     StorageObject = null;
                    break;
                case TypeCode.Char:
                    StorageChar = reader.ReadChar();                     StorageObject = null;
                    break;
                case TypeCode.Single:
                    StorageSingle = reader.ReadSingle();                     StorageObject = null;
                    break;
                case TypeCode.Double:
                    StorageDouble = reader.ReadDouble();                     StorageObject = null;
                    break;
                case TypeCode.Empty:
                    StorageObject = null;
                    break;
                case TypeCode.DBNull:
                    StorageObject = DBNull.Value;
                    break;
                case TypeCode.Int64:
                    StorageInt64 = reader.ReadInt64();
                    StorageObject = null;
                    break;
                case TypeCode.UInt64:
                    StorageUInt64 = reader.ReadUInt64();
                    StorageObject = null;
                    break;
                case TypeCode.Decimal:
                    StorageDecimal = reader.ReadDecimal();
                    StorageObject = null;
                    break;
                case TypeCode.DateTimeOffset:
                    var dateTime = reader.ReadDateTime();
                    var offset = reader.ReadTimeSpan();
                    StorageDateTimeOffset = new DateTimeOffset(dateTime, offset);
                    StorageObject = null;
                    break;
                case TypeCode.String:
                    StorageObject = reader.ReadString();                    
                    break;
                case TypeCode.Dictionary:                   
                    StorageObject = reader.ReadDictionaryOfOwnedDataSerializable(() => new Any(), context);
                    break;
                case TypeCode.List:
                    StorageObject = reader.ReadListOfOwnedDataSerializable(() => new Any(), context);
                    break;
                case TypeCode.Object:
                    StorageObject = reader.ReadObject()!;                    
                    break;
                default:
                    throw new InvalidOperationException();
            }                     }

        #endregion 
        #region private functions 
        private DateTimeOffset ValueAsDateTimeOffset(bool stringIsLocalized, string? stringFormat)
        {
            switch ((TypeCode)_valueTypeCode)
            {
                case TypeCode.String:
                    {
                        string dateTimeOffsetString = ((string)StorageObject!).Trim();

                        if (String.IsNullOrWhiteSpace(dateTimeOffsetString))
                            return default;

                        DateTimeOffset result;

                        if (String.IsNullOrEmpty(stringFormat))
                        {
                            if (dateTimeOffsetString.StartsWith("now", StringComparison.InvariantCultureIgnoreCase))
                            {
                                result = DateTimeOffset.UtcNow;
                                dateTimeOffsetString = dateTimeOffsetString.Substring("now".Length).Trim();
                                bool? sign = null;
                                if (dateTimeOffsetString.StartsWith("-"))
                                    sign = false;
                                if (dateTimeOffsetString.StartsWith("+"))
                                    sign = true;
                                if (sign is not null)
                                {
                                    var timeSpan = new Any(dateTimeOffsetString.Substring(1).Trim()).ValueAs<TimeSpan>(false);
                                    if (sign.Value)
                                        result += timeSpan;
                                    else
                                        result -= timeSpan;
                                }
                            }
                            else
                            {
                                DateTimeOffset value;
                                if (!DateTimeOffset.TryParse(dateTimeOffsetString, GetCultureInfo(stringIsLocalized), DateTimeStyles.None, out value))
                                    result = default;
                                else
                                    result = value;
                            }                            
                        }                         else
                        {
                            DateTimeOffset value;
                            if (!DateTimeOffset.TryParseExact(dateTimeOffsetString, stringFormat, GetCultureInfo(stringIsLocalized), DateTimeStyles.None, out value))
                                result = default;
                            else
                                result = value;
                        }

                        return result;
                    }
                case TypeCode.DateTimeOffset:
                    return StorageDateTimeOffset;
                default:
                    return default;
            }
        } 
        private TimeSpan ValueAsTimeSpan(bool stringIsLocalized, string? stringFormat)
        {
            switch ((TypeCode)_valueTypeCode)
            {                
                case TypeCode.String:
                    {
                        string timeSpanString = (string)StorageObject!;

                        if (String.IsNullOrWhiteSpace(timeSpanString))
                            return TimeSpan.Zero;

                        TimeSpan result;

                        if (timeSpanString.Count(f => f == ':') >= 2)
                        {
                            if (String.IsNullOrEmpty(stringFormat))
                            {
                                TimeSpan value;
                                if (String.IsNullOrWhiteSpace(timeSpanString) ||
                                        !TimeSpan.TryParse(timeSpanString, GetCultureInfo(stringIsLocalized), out value))
                                    result = default;
                                else
                                    result = value;
                            }
                            else
                            {
                                TimeSpan value;
                                if (String.IsNullOrWhiteSpace(timeSpanString) ||
                                        !TimeSpan.TryParseExact(timeSpanString, stringFormat, GetCultureInfo(stringIsLocalized), TimeSpanStyles.None, out value))
                                    result = default;
                                else
                                    result = value;
                            }

                            return result;
                        }

                        timeSpanString = timeSpanString!.Trim();

                        result = TimeSpan.Zero;

                        string? numberString = null;
                        double? number = null;
                        string? units = null;
                        foreach (Char ch in timeSpanString)
                        {
                            if (Char.IsDigit(ch) || ch == '.')
                            {
                                if (numberString is null && number is not null)
                                {
                                    result += GetTimeSpan(number.Value, units, "s");
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

                        if (numberString is not null)
                        {
                            Double.TryParse(numberString, out double number2);
                            number = number2;
                        }

                        if (number is not null)
                            result += GetTimeSpan(number.Value, units, "s");

                        return result;
                    }
                default:
                    return default;
            }
        }

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

        /// <summary>         ///     storageObject.ValueTypeCode == (byte)TypeCode.Object         /// </summary>         /// <param name="storageObject"></param>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static object? TypeCodeObject_ValueAs(object storageObject, Type asType, bool stringIsLocalized, string? stringFormat = null)         {             if (asType == typeof(string))             {                 return TypeCodeObject_ValueAsString(storageObject, stringIsLocalized, stringFormat);             }              Type storageObjectType = storageObject.GetType();             if (storageObjectType.IsSubclassOf(asType))                 return storageObject;

            TypeConverter converter = TypeDescriptor.GetConverter(storageObjectType);             if (converter.CanConvertTo(asType))             {                 try                 {                     return converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), storageObject, asType);                 }                 catch (Exception)                 {                 }             }              if (storageObjectType.IsEnum)             {                 switch (asType.FullName)
                {
                    case @"System.SByte":
                        return (SByte)(int)storageObject;
                    case @"System.Byte":
                        return (Byte)(int)storageObject;
                    case @"System.Int16":
                        return (Int16)(int)storageObject;
                    case @"System.UInt16":
                        return (UInt16)(int)storageObject;
                    case @"System.Int32":
                        return (Int32)(int)storageObject;
                    case @"System.UInt32":
                        return (UInt32)(int)storageObject;
                    case @"System.Single":
                        return (Single)(int)storageObject;
                    case @"System.Double":
                        return (Double)(int)storageObject;                    
                    case @"System.Int64":
                        return (Int64)(int)storageObject;
                    case @"System.UInt64":
                        return (UInt64)(int)storageObject;
                    case @"System.Decimal":
                        return (Decimal)(int)storageObject;
                }                             }              if (!asType.IsAbstract)
                try                 {                     return Activator.CreateInstance(asType);                 }                 catch (Exception)                 {                 }             return null;         }          /// <summary>                 /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static string TypeCodeObject_ValueAsString(object? value, bool stringIsLocalized, string? stringFormat)         {             if (value is null) return String.Empty;              Type type = value.GetType();             switch (type.Name)
            {
                case @"System.Object":
                    return String.Empty;
                case @"System.TimeSpan":
                    return ((TimeSpan)value).ToString(stringFormat ?? @"c", GetCultureInfo(stringIsLocalized));
            }

            if (value is byte[] byteArray)
            {
                return $"[Byte array ({byteArray.Length} bytes)]";
            }

            if (type.IsArray ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>)))
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new ToStringJsonConverter() },
                    WriteIndented = false,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                };
                return JsonSerializer.Serialize(value, options);
            }             // TODO:             //System.Windows.Markup.ValueSerializer valueSerializer =             //                ValueSerializer.GetSerializerFor(type);             //if (valueSerializer is not null && valueSerializer.CanConvertToString(value, null))             //{             //    try             //    {             //        string result = valueSerializer.ConvertToString(value, null);             //        if (result is not null) return result;             //    }             //    catch (Exception)             //    {             //    }             //}              TypeConverter converter = TypeDescriptor.GetConverter(type);             if (converter.CanConvertTo(typeof(string)))             {                 try                 {                     string? result = converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), value, typeof(string)) as string;                     if (result is not null) return result;                 }                 catch (Exception)                 {                 }             }              return value.ToString() ?? @"";         }          /// <summary>         ///     Returns false, if String.IsNullOrWhiteSpace(value) || value.ToUpperInvariant() == "FALSE" || value == "0",         ///     otherwise true.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static bool ConvertToBoolean(string? value, bool stringIsLocalized)         {             Any any = ConvertToBestType(value, stringIsLocalized);             if (any._valueTypeCode == (byte)TypeCode.String)                  return false;             return any.ValueAsBoolean(stringIsLocalized);         }

        /// <summary>         ///     Returns Single 0.0 if String.IsNullOrWhiteSpace(value) or value is not correct number.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static float ConvertToSingle(string value, bool stringIsLocalized)         {             float result;             if (String.IsNullOrWhiteSpace(value) || !Single.TryParse(value, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out result))
                result = 0.0f;             return result;         }

        /// <summary>         ///     Returns Double 0.0 if String.IsNullOrWhiteSpace(value) or value is not correct number.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static double ConvertToDouble(string value, bool stringIsLocalized)         {             double result;             if (String.IsNullOrWhiteSpace(value) || !Double.TryParse(value, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out result))
                result = 0.0d;             return result;         }

        /// <summary>         ///     Returns Decimal 0 if String.IsNullOrWhiteSpace(value) or value is not correct number.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static decimal ConvertToDecimal(string value, bool stringIsLocalized)         {             decimal result;             if (String.IsNullOrWhiteSpace(value) || !Decimal.TryParse(value, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out result))
                result = 0;             return result;         }

        /// <summary>         ///     Returns true, if succeeded.         ///     if conversion fails, destination doesn't change.         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object, source.ValueTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"> </param>         /// <param name="source"> </param>         /// <param name="toTypeCode"> </param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns> true if succeded, false otherwise </returns>         private static bool Convert(ref Any destination, Any source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat = null)         {             switch (source.ValueTypeCode)             {                 case TypeCode.SByte:                     return Convert(ref destination, source.StorageSByte, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Byte:                     return Convert(ref destination, source.StorageByte, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Int16:                     return Convert(ref destination, source.StorageInt16, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt16:                     return Convert(ref destination, source.StorageUInt16, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Int32:                     return Convert(ref destination, source.StorageInt32, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt32:                     return Convert(ref destination, source.StorageUInt32, toTypeCode, stringIsLocalized,                          stringFormat);                 case TypeCode.Boolean:                     return Convert(ref destination, source.StorageBoolean, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Char:                     return Convert(ref destination, source.StorageChar, toTypeCode, stringIsLocalized,                          stringFormat);                 case TypeCode.Single:                     return Convert(ref destination, source.StorageSingle, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Double:                     return Convert(ref destination, source.StorageDouble, toTypeCode, stringIsLocalized, stringFormat);                 case TypeCode.Empty:                     return ConvertFromNullOrDBNull(ref destination, toTypeCode);                 case TypeCode.DBNull:                     return ConvertFromNullOrDBNull(ref destination, toTypeCode);                 case TypeCode.Int64:                                         return Convert(ref destination, source.StorageInt64, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt64:                                         return Convert(ref destination, source.StorageUInt64, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Decimal:                                         return Convert(ref destination, source.StorageDecimal, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.String:                                         return Convert(ref destination, (String)source.StorageObject!, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.DateTimeOffset:                                         return Convert(ref destination, source.StorageDateTimeOffset, toTypeCode, stringIsLocalized,                         stringFormat);                             }              throw new InvalidOperationException();         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, SByte source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set(source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Byte source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set(source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                        default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int16 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set(source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt16 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set(source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int32 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set(source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt32 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set(source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Char source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32 ) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set(source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int64 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set(source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt64 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set(source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Boolean source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) (source ? 1 : 0));                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) (source ? 1 : 0));                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) (source ? 1 : 0));                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) (source ? 1 : 0));                         return true;                     case TypeCode.Int32:                         destination.Set((source ? 1 : 0));                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) (source ? 1 : 0));                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) (source ? 1 : 0));                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) (source ? 1 : 0));                         return true;                     case TypeCode.Boolean:                         destination.Set(source);                         return true;                     case TypeCode.Char:                         destination.Set(source ? 'Y' : 'N');                         return true;                     case TypeCode.DateTimeOffset:                         return false;                     case TypeCode.Single:                         destination.Set((Single) (source ? 1 : 0));                         return true;                     case TypeCode.Double:                         destination.Set((Double) (source ? 1 : 0));                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) (source ? 1 : 0));                         return true;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, DateTimeOffset source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.DateTimeOffset:                         destination.Set(source);                         return true;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Single source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0.0 && !Single.IsNaN(source));                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                     case TypeCode.Single:                         destination.Set(source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Double source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0.0 && !Double.IsNaN(source));                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set(source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Decimal source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != (decimal) 0.0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTimeOffset:                         return false;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Decimal:                         destination.Set(source);                         return true;                                         default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="toTypeCode"></param>         /// <returns></returns>         private static bool ConvertFromNullOrDBNull(ref Any destination, TypeCode toTypeCode)         {             switch (toTypeCode)             {                 case TypeCode.SByte:                     destination.Set((SByte)0);                     return true;                 case TypeCode.Byte:                     destination.Set((Byte)0);                     return true;                 case TypeCode.Int16:                     destination.Set((Int16)0);                     return true;                 case TypeCode.UInt16:                     destination.Set((UInt16)0);                     return true;                 case TypeCode.Int32:                     destination.Set(0);                     return true;                 case TypeCode.UInt32:                     destination.Set((UInt32)0);                     return true;                 case TypeCode.Boolean:                     destination.Set(false);                     return true;                 case TypeCode.Char:                     destination.Set((Char)0);                     return true;                 case TypeCode.Single:                     destination.Set(0.0f);                     return true;                 case TypeCode.Double:                     destination.Set(0.0d);                     return true;                 case TypeCode.Int64:                     destination.Set((Int64)0);                     return true;                 case TypeCode.UInt64:                     destination.Set((UInt64)0);                     return true;                 case TypeCode.Decimal:                     destination.Set((Decimal)0);                     return true;                 case TypeCode.DateTimeOffset:                     destination.Set(default(DateTimeOffset));                     return true;                                 default:                     return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, String source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                     {                         SByte value;                         if (String.IsNullOrWhiteSpace(source) ||                             !SByte.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Byte:                     {                         Byte value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Byte.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Int16:                     {                         Int16 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int16.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt16:                     {                         UInt16 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt16.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Int32:                     {                         Int32 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int32.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt32:                     {                         UInt32 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt32.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Boolean:                         destination.Set(ConvertToBoolean(source, stringIsLocalized));                         return true;                     case TypeCode.Single:
                        destination.Set(ConvertToSingle(source, stringIsLocalized));                         return true;                     case TypeCode.Double:                         destination.Set(ConvertToDouble(source, stringIsLocalized));                         return true;                     case TypeCode.Int64:                     {                         Int64 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int64.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt64:                     {                         UInt64 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt64.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Decimal:
                        destination.Set(ConvertToDecimal(source, stringIsLocalized));                         return true;                                                          default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     asType has System.TypeCode.Object, _valueTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private object? ValueAsObject(Type asType, bool stringIsLocalized)         {             if (_valueTypeCode == (byte)TypeCode.Empty)             {                 if (asType.IsClass)                 {                     return null;                 }                 else                 {                     if (!asType.IsAbstract)
                        try                         {                             return Activator.CreateInstance(asType);                         }                         catch (Exception)                         {                         }                     return null;                 }                             }              if (_valueTypeCode == (byte)TypeCode.String)             {                                 if ((string)StorageObject! == @"")                 {                     if (!asType.IsAbstract)                         try                         {                             return Activator.CreateInstance(asType);                         }                         catch (Exception)                         {                                                 }                     return null;                 }             }              TypeConverter converter = TypeDescriptor.GetConverter(asType);             if (converter.CanConvertFrom(ValueType))             {                 try                 {                     return converter.ConvertFrom(null, GetCultureInfo(stringIsLocalized), ValueAsObject()!); // _valueTypeCode == (byte)TypeCode.Empty handled earlier.
                }                 catch (Exception)                 {                 }                             }              if (!asType.IsAbstract)
                try                 {                     return Activator.CreateInstance(asType);                 }                 catch (Exception)                 {                 }             return null;         }

        private void Write(Utf8JsonWriter writer, JsonSerializerOptions options)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         JsonSerializer.Serialize(writer, StorageSByte, options);                         break;                     case TypeCode.Byte:                                                 JsonSerializer.Serialize(writer, StorageByte, options);                         break;                     case TypeCode.Int16:                                                 JsonSerializer.Serialize(writer, StorageInt16, options);                         break;                     case TypeCode.UInt16:                                                 JsonSerializer.Serialize(writer, StorageUInt16, options);                         break;                     case TypeCode.Int32:                                                 JsonSerializer.Serialize(writer, StorageInt32, options);                         break;                     case TypeCode.UInt32:                                                 JsonSerializer.Serialize(writer, StorageUInt32, options);                         break;                     case TypeCode.Boolean:                                                 JsonSerializer.Serialize(writer, StorageBoolean, options);                         break;                     case TypeCode.Char:                                                 JsonSerializer.Serialize(writer, StorageChar, options);                         break;                     case TypeCode.Single:                                                 JsonSerializer.Serialize(writer, StorageSingle, options);                         break;                     case TypeCode.Double:                                                 JsonSerializer.Serialize(writer, StorageDouble, options);                         break;                     case TypeCode.Empty:                                                                         break;                     case TypeCode.DBNull:
                        break;
                    case TypeCode.Int64:                                                 JsonSerializer.Serialize(writer, StorageInt64, options);                         break;                     case TypeCode.UInt64:                                                 JsonSerializer.Serialize(writer, StorageUInt64, options);                         break;                     case TypeCode.Decimal:                                                 JsonSerializer.Serialize(writer, StorageDecimal, options);                         break;                     case TypeCode.DateTimeOffset:                        
                        JsonSerializer.Serialize(writer, StorageDateTimeOffset, options);                         break;
                    case TypeCode.String:                        
                        JsonSerializer.Serialize(writer, (String)StorageObject!, options);                         break;
                    case TypeCode.Dictionary:                        
                        JsonSerializer.Serialize(writer, StorageObject!, options);                         break;
                    case TypeCode.List:                        
                        JsonSerializer.Serialize(writer, StorageObject!, options);                         break;
                    case TypeCode.Object:                                                 JsonSerializer.Serialize(writer, StorageObject, options);                         break;                 }             }             catch (Exception)             {                             }         }

#endregion          #region public fields       
        /// <summary>
        ///     Unsafe.
        /// </summary>
        [FieldOffset(0)]         public object? StorageObject;

        /// <summary>
        ///     Unsafe.
        /// </summary>         [FieldOffset(8)]         public sbyte StorageSByte;

        /// <summary>
        ///     Unsafe.
        /// </summary>         [FieldOffset(8)]         public byte StorageByte;

        /// <summary>
        ///     Unsafe.
        /// </summary>         [FieldOffset(8)]         public short StorageInt16;

        /// <summary>
        ///     Unsafe.
        /// </summary>         [FieldOffset(8)]         public ushort StorageUInt16;

        /// <summary>
        ///     Unsafe.
        /// </summary>         [FieldOffset(8)]         public int StorageInt32;

        /// <summary>
        ///     Unsafe.
        /// </summary>         [FieldOffset(8)]         public uint StorageUInt32;

        /// <summary>
        ///     Unsafe.
        /// </summary>          [FieldOffset(8)]         public bool StorageBoolean;

        /// <summary>
        ///     Unsafe.
        /// </summary>         [FieldOffset(8)]         public char StorageChar;

        /// <summary>
        ///     Unsafe.
        ///     4 bytes.
        /// </summary>         [FieldOffset(8)]         public float StorageSingle;

        /// <summary>
        ///     Unsafe.
        ///     8 bytes
        /// </summary>         [FieldOffset(8)]         public double StorageDouble;

        /// <summary>
        ///     Unsafe.
        /// </summary>
        [FieldOffset(8)]         public long StorageInt64;

        /// <summary>
        ///     Unsafe.
        /// </summary>
        [FieldOffset(8)]         public ulong StorageUInt64;

        /// <summary>
        ///     Unsafe.
        ///     16 bytes
        /// </summary>
        [FieldOffset(8)]         public decimal StorageDecimal;

        /// <summary>
        ///     Unsafe.
        /// </summary>
        [FieldOffset(8)]         public DateTimeOffset StorageDateTimeOffset;   
        
        public static readonly Any[] EmptyArray = new Any[0];

        #endregion 
        #region private fields

        [FieldOffset(24)]         private byte _valueTypeCode;

        #endregion 
        private class ToStringJsonConverter : JsonConverter<Any>
        {
            public override Any Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotSupportedException("Deserialization is not supported.");
            }            

            public override void Write(Utf8JsonWriter writer, Any value, JsonSerializerOptions options)
            {
                value.Write(writer, options);
                //writer.WriteStringValue(value.ValueAsString(false));                
            }
        } 
        //public class HashEqualityComparer : IEqualityComparer<Any>
        //{
        //    public static readonly HashEqualityComparer Instance = new HashEqualityComparer();

        //    public bool Equals(Any x, Any y)
        //    {
        //        return x.GetHashCode() == y.GetHashCode();
        //    }

        //    public int GetHashCode(Any obj)
        //    {
        //        return 0;
        //    }
        //}
    }     }