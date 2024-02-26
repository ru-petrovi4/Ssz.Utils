using Ssz.Utils.Serialization; using System; using System.Collections; using System.Collections.Generic;
using System.ComponentModel; using System.Globalization;
using System.IO;
using System.Linq; using System.Runtime.CompilerServices; using System.Runtime.InteropServices;

namespace Ssz.Utils {
    /// <summary>     ///     If func param stringIsLocalized = false, InvariantCulture is used.     ///     If func param stringIsLocalized = true, CultureHelper.SystemCultureInfo is used, which is corresponds operating system culture (see CultureHelper class).     /// </summary>     [StructLayout(LayoutKind.Explicit)]     public struct Any : IOwnedDataSerializable, IComparable<Any>, IComparable     {         #region StorageType enum          public enum TypeCode : byte
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
            ///     A type representing a date and time value.
            /// </summary>
            DateTime = 16,

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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(DBNull value)         {             Unsafe.SkipInit(out _valueTypeCode);             Unsafe.SkipInit(out StorageSByte);             Unsafe.SkipInit(out StorageByte);             Unsafe.SkipInit(out StorageInt16);             Unsafe.SkipInit(out StorageUInt16);
            Unsafe.SkipInit(out StorageInt32);
            Unsafe.SkipInit(out StorageUInt32);
            Unsafe.SkipInit(out StorageBoolean);
            Unsafe.SkipInit(out StorageChar);
            Unsafe.SkipInit(out StorageSingle);
            Unsafe.SkipInit(out StorageDouble);
            Unsafe.SkipInit(out StorageInt64);
            Unsafe.SkipInit(out StorageUInt64);
            Unsafe.SkipInit(out StorageDecimal);
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
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
            Unsafe.SkipInit(out StorageDateTime);
            Unsafe.SkipInit(out StorageObject);

            Set(value);
        }

        #region public functions        
        public static CultureInfo GetCultureInfo(bool localized)         {             if (localized)
                return CultureInfo.CurrentCulture;             return CultureInfo.InvariantCulture;         }

        /// <summary>         ///          /// </summary>         public TypeCode ValueTypeCode         {             get { return (TypeCode)_valueTypeCode; }         }          /// <summary>                 /// </summary>         public Type ValueType         {             get             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return typeof (SByte);                     case TypeCode.Byte:                         return typeof (Byte);                     case TypeCode.Int16:                         return typeof (Int16);                     case TypeCode.UInt16:                         return typeof (UInt16);                     case TypeCode.Int32:                         return typeof (Int32);                     case TypeCode.UInt32:                         return typeof (UInt32);                     case TypeCode.Boolean:                         return typeof (Boolean);                     case TypeCode.Single:                         return typeof (Single);                     case TypeCode.Double:                         return typeof (Double);                     case TypeCode.Empty:                         return typeof (Object);                     case TypeCode.DBNull:                         return typeof (DBNull);                     case TypeCode.Int64:                         return typeof (Int64);                     case TypeCode.UInt64:                         return typeof (UInt64);                     case TypeCode.Decimal:                         return typeof (Decimal);                     case TypeCode.DateTime:                         return typeof (DateTime);                     case TypeCode.String:                         return typeof (String);                     case TypeCode.Char:                         return typeof (Char);                     case TypeCode.Dictionary:                         return typeof(Dictionary<string, Any>);                     case TypeCode.List:                         return typeof(List<Any>);                     case TypeCode.Object:                                                 return StorageObject!.GetType();                 }                 throw new InvalidOperationException();             }         }

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
                    return left.StorageSingle == right.StorageSingle;                    
                case TypeCode.Double:
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
                case TypeCode.DateTime:
                    return left.StorageDateTime == right.StorageDateTime;
                case TypeCode.String:
                    return (string)left.StorageObject! == (string)right.StorageObject!;
                case TypeCode.Dictionary:
                    return ((Dictionary<string, Any>)left.StorageObject!).SequenceEqual((Dictionary<string, Any>)right.StorageObject!);
                case TypeCode.List:
                    return ((List<Any>)left.StorageObject!).SequenceEqual((List<Any>)right.StorageObject!);
                case TypeCode.Object:
                    {                        
                        if (left.StorageObject is IStructuralEquatable leftStructuralEquatable)
                            return leftStructuralEquatable.Equals(right.StorageObject, StructuralComparisons.StructuralEqualityComparer);                        
                        return left.StorageObject == right.StorageObject;
                    }
                default:
                    throw new InvalidOperationException();
            }         }          /// <summary>                 /// </summary>         /// <param name="left"></param>         /// <param name="right"></param>         /// <returns></returns>         public static bool operator !=(Any left, Any right)         {             return !(left == right);         }

        /// <summary>         ///     Compares the current instance with another object of the same type and returns an integer that indicates          ///     whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.         ///     Uses ValueAsDouble(false), ValueAsUInt32(false), ValueAsString(false) depending of ValueTransportType.                 /// </summary>         /// <param name="that"></param>         /// <param name="deadband"></param>         /// <returns></returns>         public int CompareTo(Any that, double deadband)         {
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
                        float diff = d - thatD;
                        if (diff > -deadband - Single.Epsilon * 100 &&
                                diff < deadband + Single.Epsilon * 100)
                            return 0;
                        if (diff <= -deadband - Single.Epsilon * 100)
                            return -1;
                        if (diff >= deadband + Single.Epsilon * 100)
                            return 1;
                        return -1;
                    }
                case TypeCode.Double:
                    {                         double d = ValueAsDouble(false);
                        double thatD = that.ValueAsDouble(false);                        
                        double diff = d - thatD;
                        if (diff >= -deadband - Double.Epsilon * 100 &&
                                diff <= deadband + Double.Epsilon * 100)
                            return 0;
                        if (diff < -deadband - Double.Epsilon * 100)
                            return -1;
                        if (diff > deadband + Double.Epsilon * 100)
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
                case TypeCode.DateTime:
                    return ValueAs<DateTime>(false).CompareTo(that.ValueAs<DateTime>(false));
                case TypeCode.String:
                    return ValueAsString(false).CompareTo(that.ValueAsString(false));
                case TypeCode.Dictionary:
                    return ValueAsDictionary().SequenceEqual(that.ValueAsDictionary()) ? 0 : 1;
                case TypeCode.List:
                    return ValueAsList().SequenceEqual(that.ValueAsList()) ? 0 : 1;
                case TypeCode.Object:
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

            switch (valueType.Name)             {                 case nameof(Any):                     Set((Any)value);                     return;                 case nameof(SByte):                     Set((SByte)value);                     return;                 case nameof(Byte):                     Set((Byte)value);                     return;                 case nameof(Int16):                     Set((Int16)value);                     return;                 case nameof(UInt16):                     Set((UInt16)value);                     return;                 case nameof(Int32):                     Set((Int32)value);                     return;                 case nameof(UInt32):                     Set((UInt32)value);                     return;                 case nameof(Boolean):                     Set((Boolean)value);                     return;                 case nameof(Single):                     Set((Single)value);                     return;                 case nameof(Double):                     Set((Double)value);                     return;
                case nameof(DBNull):                     Set((DBNull)value);                     return;                 case nameof(Int64):                     Set((Int64)value);                     return;                 case nameof(UInt64):                     Set((UInt64)value);                     return;                 case nameof(Decimal):                     Set((Decimal)value);                     return;                 case nameof(DateTime):                     Set((DateTime)value);                     return;                 case nameof(Char):                     Set((Char)value);                     return;
                case nameof(String):                     _valueTypeCode = (byte)TypeCode.String;
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
                case TypeCode.DateTime:
                    StorageDateTime = value.StorageDateTime;
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

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Char value)         {             _valueTypeCode = (byte)TypeCode.UInt32;             StorageChar = value;
            StorageObject = null;         }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Single value)         {             _valueTypeCode = (byte)TypeCode.Single;
            StorageSingle = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Double value)         {             _valueTypeCode = (byte)TypeCode.Double;                         StorageDouble = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Decimal value)         {             _valueTypeCode = (byte)TypeCode.Decimal;
            StorageDecimal = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Int64 value)         {             _valueTypeCode = (byte)TypeCode.Int64;
            StorageInt64 = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(UInt64 value)         {             _valueTypeCode = (byte)TypeCode.UInt64;
            StorageUInt64 = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(DateTime value)         {             _valueTypeCode = (byte)TypeCode.DateTime;
            StorageDateTime = value;             StorageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(DBNull value)         {             _valueTypeCode = (byte)TypeCode.DBNull;                         StorageObject = value;         }                  /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(String? value)         {
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

        /// <summary>         ///          /// </summary>         /// <returns></returns>         public object? ValueAsObject()         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return StorageSingle;                     case TypeCode.Double:                         return StorageDouble;                     case TypeCode.Empty:                         return null;                     case TypeCode.DBNull:                         return StorageObject;                     case TypeCode.Int64:                         return StorageInt64;                     case TypeCode.UInt64:                         return StorageUInt64;                     case TypeCode.Decimal:                         return StorageDecimal;                     case TypeCode.DateTime:                         return StorageDateTime;                                         case TypeCode.String:                         return StorageObject;                     case TypeCode.Dictionary:                         return StorageObject;                     case TypeCode.List:                         return StorageObject;                     case TypeCode.Object:                         return StorageObject;                 }             }             catch (Exception)             {                 return null;             }              throw new InvalidOperationException();         }          /// <summary>                 /// </summary>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public String ValueAsString(bool stringIsLocalized, string? stringFormat = null)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Byte:                         return StorageByte.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Int16:                         return StorageInt16.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt16:                         return StorageUInt16.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Int32:                         return StorageInt32.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt32:                         return StorageUInt32.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Boolean:                         return StorageBoolean.ToString(GetCultureInfo(stringIsLocalized));                     case TypeCode.Char:                         return StorageChar.ToString();                     case TypeCode.Single:                         return StorageSingle.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Double:                         return StorageDouble.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Empty:                         return String.Empty;                     case TypeCode.DBNull:                                                 return ((DBNull)StorageObject!).ToString(GetCultureInfo(stringIsLocalized));                     case TypeCode.Int64:                                                 return StorageInt64.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt64:                                                 return StorageUInt64.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Decimal:                                                 return StorageDecimal.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.DateTime:                                                 return StorageDateTime.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                                             case TypeCode.String:                                                 return (String)StorageObject!;
                    case TypeCode.Dictionary:                         return StorageObject!.ToString() ?? @"";                     case TypeCode.List:                         return StorageObject!.ToString() ?? @"";                     case TypeCode.Object:                         return ConvertToString(StorageObject, stringIsLocalized);                 }             }             catch (Exception)             {                 return String.Empty;             }              throw new InvalidOperationException();         }          /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public Int32 ValueAsInt32(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return (Int32) StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1 : 0;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return (Int32)StorageSingle;                     case TypeCode.Double:                         return (Int32)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (Int32)StorageInt64;                     case TypeCode.UInt64:                                                 return (Int32)StorageUInt64;                     case TypeCode.Decimal:                                                 return (Int32)StorageDecimal;                     case TypeCode.DateTime:                         return 0;                     case TypeCode.String:                                                 Int32 result;                         if (!Int32.TryParse((string)StorageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(int), stringIsLocalized);                         if (obj is null)                              return 0;                         else return (int)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public UInt32 ValueAsUInt32(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return (UInt32)StorageSByte;                     case TypeCode.Byte:                         return (UInt32)StorageByte;                     case TypeCode.Int16:                         return (UInt32)StorageInt16;                     case TypeCode.UInt16:                         return (UInt32)StorageUInt16;                     case TypeCode.Int32:                         return (UInt32)StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1u : 0u;                     case TypeCode.Char:                         return (UInt32)StorageChar;                     case TypeCode.Single:                         return (UInt32)StorageSingle;                     case TypeCode.Double:                         return (UInt32)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (UInt32)StorageInt64;                     case TypeCode.UInt64:                                                 return (UInt32)StorageUInt64;                     case TypeCode.Decimal:                                                 return (UInt32)StorageDecimal;                     case TypeCode.DateTime:                         return 0;                     case TypeCode.String:                                                 UInt32 result;                         if (!UInt32.TryParse((string)StorageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(UInt32), stringIsLocalized);                         if (obj is null) return 0;                         else return (UInt32)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          public Int64 ValueAsInt64(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return (Int64)StorageSByte;                     case TypeCode.Byte:                         return (Int64)StorageByte;                     case TypeCode.Int16:                         return (Int64)StorageInt16;                     case TypeCode.UInt16:                         return (Int64)StorageUInt16;                     case TypeCode.Int32:                         return (Int64)StorageInt32;                     case TypeCode.UInt32:                         return (Int64)StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1L : 0L;                     case TypeCode.Char:                         return (Int64)StorageChar;                     case TypeCode.Single:                         return (Int64)StorageSingle;                     case TypeCode.Double:                         return (Int64)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:
                        return (Int64)StorageInt64;                     case TypeCode.UInt64:
                        return (Int64)StorageUInt64;                     case TypeCode.Decimal:
                        return (Int64)StorageDecimal;                     case TypeCode.DateTime:                         return 0;                     case TypeCode.String:
                        Int64 result;                         if (!Int64.TryParse((string)StorageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:
                        object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(Int64), stringIsLocalized);                         if (obj is null)                             return 0;                         else return (Int64)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          public UInt64 ValueAsUInt64(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return (UInt64)StorageSByte;                     case TypeCode.Byte:                         return (UInt64)StorageByte;                     case TypeCode.Int16:                         return (UInt64)StorageInt16;                     case TypeCode.UInt16:                         return (UInt64)StorageUInt16;                     case TypeCode.Int32:                         return (UInt64)StorageInt32;                     case TypeCode.UInt32:                         return (UInt64)StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1ul : 0ul;                     case TypeCode.Char:                         return (UInt64)StorageChar;                     case TypeCode.Single:                         return (UInt64)StorageSingle;                     case TypeCode.Double:                         return (UInt64)StorageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (UInt64)StorageInt64;                     case TypeCode.UInt64:                                                 return (UInt64)StorageUInt64;                     case TypeCode.Decimal:                                                 return (UInt64)StorageDecimal;                     case TypeCode.DateTime:                         return 0;                     case TypeCode.String:                                                 UInt64 result;                         if (!UInt64.TryParse((string)StorageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(UInt64), stringIsLocalized);                         if (obj is null)                              return 0;                         else return (UInt64)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }

        /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public float ValueAsSingle(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1f : 0f;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return StorageSingle;                     case TypeCode.Double:                         return (float)StorageDouble;                     case TypeCode.Empty:                         return 0.0f;                     case TypeCode.DBNull:                         return 0.0f;                     case TypeCode.Int64:
                        return StorageInt64;                     case TypeCode.UInt64:
                        return StorageUInt64;                     case TypeCode.Decimal:
                        return (float)StorageDecimal;                     case TypeCode.DateTime:                         return 0.0f;                     case TypeCode.String:
                        return ConvertToSingle((string)StorageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return 0.0f;                     case TypeCode.List:                         return 0.0f;                     case TypeCode.Object:
                        object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(float), stringIsLocalized);                         if (obj is null)                             return 0.0f;                         else return (float)obj;                 }             }             catch (Exception)             {                 return 0.0f;             }              throw new InvalidOperationException();         }

        /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public Double ValueAsDouble(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte;                     case TypeCode.Byte:                         return StorageByte;                     case TypeCode.Int16:                         return StorageInt16;                     case TypeCode.UInt16:                         return StorageUInt16;                     case TypeCode.Int32:                         return StorageInt32;                     case TypeCode.UInt32:                         return StorageUInt32;                     case TypeCode.Boolean:                         return StorageBoolean ? 1d : 0d;                     case TypeCode.Char:                         return StorageChar;                     case TypeCode.Single:                         return StorageSingle;                     case TypeCode.Double:                         return StorageDouble;                     case TypeCode.Empty:                         return 0.0d;                     case TypeCode.DBNull:                         return 0.0d;                     case TypeCode.Int64:                                                 return StorageInt64;                     case TypeCode.UInt64:                                                 return StorageUInt64;                     case TypeCode.Decimal:                                                 return (Double)StorageDecimal;                     case TypeCode.DateTime:                         return 0.0d;                     case TypeCode.String:                                                 return ConvertToDouble((string)StorageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return 0.0d;                     case TypeCode.List:                         return 0.0d;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(double), stringIsLocalized);                         if (obj is null)                             return 0.0d;                         else return (double)obj;                 }             }             catch (Exception)             {                 return 0.0d;             }              throw new InvalidOperationException();         }          /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public bool ValueAsBoolean(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_valueTypeCode)                 {                     case TypeCode.SByte:                         return StorageSByte != 0;                     case TypeCode.Byte:                         return StorageByte != 0;                     case TypeCode.Int16:                         return StorageInt16 != 0;                     case TypeCode.UInt16:                         return StorageUInt16 != 0;                     case TypeCode.Int32:                         return StorageInt32 != 0;                     case TypeCode.UInt32:                         return StorageUInt32 != 0;                     case TypeCode.Boolean:                         return StorageBoolean;                     case TypeCode.Char:                         return StorageChar != 0;                     case TypeCode.Single:                         return StorageSingle != 0.0 && !Single.IsNaN(StorageSingle);                     case TypeCode.Double:                         return StorageDouble != 0.0 && !Double.IsNaN(StorageDouble);                     case TypeCode.Empty:                         return false;                     case TypeCode.DBNull:                         return false;                     case TypeCode.Int64:                                                 return StorageInt64 != 0;                     case TypeCode.UInt64:                                                 return StorageUInt64 != 0;                     case TypeCode.Decimal:                                                 return StorageDecimal != (decimal) 0.0;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                                                 return ConvertToBoolean((string)StorageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return false;                     case TypeCode.List:                         return false;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(StorageObject!, typeof(bool), stringIsLocalized);                         if (obj is null)                             return false;                         else return (bool)obj;                 }             }             catch (Exception)             {                 return false;             }              throw new InvalidOperationException();         }          public Dictionary<string, Any> ValueAsDictionary()         {             if ((TypeCode)_valueTypeCode == TypeCode.Dictionary)                 return (Dictionary<string, Any>)StorageObject!;             else                 return new();         }

        public List<Any> ValueAsList()         {             if ((TypeCode)_valueTypeCode == TypeCode.List)                 return (List<Any>)StorageObject!;             else                 return new();         }

        /// <summary>         ///          /// </summary>         /// <typeparam name="T"></typeparam>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public T? ValueAs<T>(bool stringIsLocalized, string? stringFormat = null)             where T : notnull         {             return (T?)ValueAs(typeof(T), stringIsLocalized, stringFormat);         }          /// <summary>         ///     Returns requested type or null.          /// </summary>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public object? ValueAs(Type? asType, bool stringIsLocalized, string? stringFormat = null)         {             if (asType is null || asType == typeof(object) || asType == ValueType)             {                 return ValueAsObject();             }              if (asType.IsEnum)             {                 return ValueAsIsEnum(asType, stringIsLocalized);                             }                          if (_valueTypeCode == (byte)TypeCode.Object)             {                                 return TypeCodeObject_ValueAs(StorageObject!, asType, stringIsLocalized);
            }

            TypeCode asTypeCode;
            if (asType.IsGenericType)             {                 return ValueAsObject(asType, stringIsLocalized);
            }
            
            switch (asType.Name)
            {
                case nameof(SByte):
                    asTypeCode = TypeCode.SByte;
                    break;
                case nameof(Byte):
                    asTypeCode = TypeCode.Byte;
                    break;
                case nameof(Int16):
                    asTypeCode = TypeCode.Int16;
                    break;
                case nameof(UInt16):
                    asTypeCode = TypeCode.UInt16;
                    break;
                case nameof(Int32):
                    asTypeCode = TypeCode.Int32;
                    break;
                case nameof(UInt32):
                    asTypeCode = TypeCode.UInt32;
                    break;
                case nameof(Boolean):
                    asTypeCode = TypeCode.Boolean;
                    break;
                case nameof(Char):
                    asTypeCode = TypeCode.Char;
                    break;
                case nameof(Single):
                    asTypeCode = TypeCode.Single;
                    break;
                case nameof(Double):
                    asTypeCode = TypeCode.Double;
                    break;
                case nameof(DBNull):
                    return DBNull.Value;
                case nameof(Int64):
                    asTypeCode = TypeCode.Int64;
                    break;
                case nameof(UInt64):
                    asTypeCode = TypeCode.UInt64;
                    break;
                case nameof(Decimal):
                    asTypeCode = TypeCode.Decimal;
                    break;
                case nameof(DateTime):
                    asTypeCode = TypeCode.DateTime;
                    break;
                case nameof(String):
                    asTypeCode = TypeCode.String;
                    break;
                default:
                    return ValueAsObject(asType, stringIsLocalized);
            }              var destination = new Any();             if (Convert(ref destination, this, asTypeCode, stringIsLocalized, stringFormat))             {                 return destination.ValueAsObject();             }             else             {                 try                 {                     return Activator.CreateInstance(asType);                 }                 catch (Exception)                 {                 }                 return null;             }         }        

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
                case TypeCode.DateTime:
                    writer.Write(StorageDateTime);
                    break;
                case TypeCode.String:
                    writer.Write((String)StorageObject!);
                    break;
                case TypeCode.Dictionary:
                    writer.WriteDictionaryOfOwnedDataSerializable((Dictionary<string, Any>)StorageObject!, null);
                    break;
                case TypeCode.List:
                    writer.WriteListOfOwnedDataSerializable((List<Any>)StorageObject!, null);
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
                case TypeCode.DateTime:
                    StorageDateTime = reader.ReadDateTime();
                    StorageObject = null;
                    break;
                case TypeCode.String:
                    StorageObject = reader.ReadString();                    
                    break;
                case TypeCode.Dictionary:                   
                    StorageObject = reader.ReadDictionaryOfOwnedDataSerializable(() => new Any(), null);
                    break;
                case TypeCode.List:
                    StorageObject = reader.ReadListOfOwnedDataSerializable(() => new Any(), null);
                    break;
                case TypeCode.Object:
                    StorageObject = reader.ReadObject()!;                    
                    break;
                default:
                    throw new InvalidOperationException();
            }                     }          #endregion          #region private functions          /// <summary>         ///     storageObject.ValueTypeCode == (byte)TypeCode.Object         /// </summary>         /// <param name="storageObject"></param>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static object? TypeCodeObject_ValueAs(object storageObject, Type asType, bool stringIsLocalized)         {             if (asType == typeof(string))             {                 return ConvertToString(storageObject, stringIsLocalized);             }              Type storageObjectType = storageObject.GetType();             if (storageObjectType.IsSubclassOf(asType))                 return storageObject;

            TypeConverter converter = TypeDescriptor.GetConverter(storageObjectType);             if (converter.CanConvertTo(asType))             {                 try                 {                     return converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), storageObject, asType);                 }                 catch (Exception)                 {                 }             }              if (storageObjectType.IsEnum)             {                 switch (asType.Name)
                {
                    case nameof(SByte):
                        return (SByte)(int)storageObject;
                    case nameof(Byte):
                        return (Byte)(int)storageObject;
                    case nameof(Int16):
                        return (Int16)(int)storageObject;
                    case nameof(UInt16):
                        return (UInt16)(int)storageObject;
                    case nameof(Int32):
                        return (Int32)(int)storageObject;
                    case nameof(UInt32):
                        return (UInt32)(int)storageObject;
                    case nameof(Single):
                        return (Single)(int)storageObject;
                    case nameof(Double):
                        return (Double)(int)storageObject;                    
                    case nameof(Int64):
                        return (Int64)(int)storageObject;
                    case nameof(UInt64):
                        return (UInt64)(int)storageObject;
                    case nameof(Decimal):
                        return (Decimal)(int)storageObject;
                }                             }              try             {                 return Activator.CreateInstance(asType);             }             catch (Exception)             {             }             return null;         }          /// <summary>                 /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static string ConvertToString(object? value, bool stringIsLocalized)         {             if (value is null) return String.Empty;              Type type = value.GetType();             if (type == typeof(object))                  return String.Empty;              // TODO:             //System.Windows.Markup.ValueSerializer valueSerializer =             //                ValueSerializer.GetSerializerFor(type);             //if (valueSerializer is not null && valueSerializer.CanConvertToString(value, null))             //{             //    try             //    {             //        string result = valueSerializer.ConvertToString(value, null);             //        if (result is not null) return result;             //    }             //    catch (Exception)             //    {             //    }             //}              TypeConverter converter = TypeDescriptor.GetConverter(type);             if (converter.CanConvertTo(typeof(string)))             {                 try                 {                     string? result = converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), value, typeof(string)) as string;                     if (result is not null) return result;                 }                 catch (Exception)                 {                 }             }              return value.ToString() ?? @"";         }          /// <summary>         ///     Returns false, if String.IsNullOrWhiteSpace(value) || value.ToUpperInvariant() == "FALSE" || value == "0",         ///     otherwise true.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static bool ConvertToBoolean(string? value, bool stringIsLocalized)         {             Any any = ConvertToBestType(value, stringIsLocalized);             if (any._valueTypeCode == (byte)TypeCode.String)                  return false;             return any.ValueAsBoolean(stringIsLocalized);         }

        /// <summary>         ///     Returns Single 0.0 if String.IsNullOrWhiteSpace(value) or value is not correct number.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static float ConvertToSingle(string value, bool stringIsLocalized)         {             float result;             if (String.IsNullOrWhiteSpace(value) || !Single.TryParse(value, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out result))
                result = 0.0f;             return result;         }

        /// <summary>         ///     Returns Double 0.0 if String.IsNullOrWhiteSpace(value) or value is not correct number.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static double ConvertToDouble(string value, bool stringIsLocalized)         {             double result;             if (String.IsNullOrWhiteSpace(value) || !Double.TryParse(value, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out result))
                result = 0.0d;             return result;         }        

        /// <summary>         ///     Returns true, if succeeded.         ///     if conversion fails, destination doesn't change.         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object, source.ValueTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"> </param>         /// <param name="source"> </param>         /// <param name="toTypeCode"> </param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns> true if succeded, false otherwise </returns>         private static bool Convert(ref Any destination, Any source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat = null)         {                         switch (source.ValueTypeCode)             {                 case TypeCode.SByte:                     return Convert(ref destination, source.StorageSByte, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Byte:                     return Convert(ref destination, source.StorageByte, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Int16:                     return Convert(ref destination, source.StorageInt16, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt16:                     return Convert(ref destination, source.StorageUInt16, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Int32:                     return Convert(ref destination, source.StorageInt32, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt32:                     return Convert(ref destination, source.StorageUInt32, toTypeCode, stringIsLocalized,                          stringFormat);                 case TypeCode.Boolean:                     return Convert(ref destination, source.StorageBoolean, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Char:                     return Convert(ref destination, source.StorageChar, toTypeCode, stringIsLocalized,                          stringFormat);                 case TypeCode.Single:                     return Convert(ref destination, source.StorageSingle, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Double:                     return Convert(ref destination, source.StorageDouble, toTypeCode, stringIsLocalized, stringFormat);                 case TypeCode.Empty:                     return ConvertFromNullOrDBNull(ref destination, toTypeCode);                 case TypeCode.DBNull:                     return ConvertFromNullOrDBNull(ref destination, toTypeCode);                 case TypeCode.Int64:                                         return Convert(ref destination, source.StorageInt64, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt64:                                         return Convert(ref destination, source.StorageUInt64, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Decimal:                                         return Convert(ref destination, source.StorageDecimal, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.String:                                         return Convert(ref destination, (String)source.StorageObject!, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.DateTime:                                         return Convert(ref destination, source.StorageDateTime, toTypeCode, stringIsLocalized,                         stringFormat);                             }              throw new InvalidOperationException();         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, SByte source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set(source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Byte source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set(source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int16 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set(source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt16 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set(source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int32 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set(source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt32 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set(source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Char source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32 ) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set(source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString());                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int64 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set(source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt64 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set(source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Boolean source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) (source ? 1 : 0));                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) (source ? 1 : 0));                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) (source ? 1 : 0));                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) (source ? 1 : 0));                         return true;                     case TypeCode.Int32:                         destination.Set((source ? 1 : 0));                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) (source ? 1 : 0));                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) (source ? 1 : 0));                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) (source ? 1 : 0));                         return true;                     case TypeCode.Boolean:                         destination.Set(source);                         return true;                     case TypeCode.Char:                         destination.Set(source ? 'Y' : 'N');                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.Single:                         destination.Set((Single) (source ? 1 : 0));                         return true;                     case TypeCode.Double:                         destination.Set((Double) (source ? 1 : 0));                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) (source ? 1 : 0));                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, DateTime source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.DateTime:                         destination.Set(source);                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Single source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0.0 && !Single.IsNaN(source));                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.Single:                         destination.Set(source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Double source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0.0 && !Double.IsNaN(source));                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set(source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Decimal source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != (decimal) 0.0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Decimal:                         destination.Set(source);                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="toTypeCode"></param>         /// <returns></returns>         private static bool ConvertFromNullOrDBNull(ref Any destination, TypeCode toTypeCode)         {             switch (toTypeCode)             {                 case TypeCode.SByte:                     destination.Set((SByte)0);                     return true;                 case TypeCode.Byte:                     destination.Set((Byte)0);                     return true;                 case TypeCode.Int16:                     destination.Set((Int16)0);                     return true;                 case TypeCode.UInt16:                     destination.Set((UInt16)0);                     return true;                 case TypeCode.Int32:                     destination.Set(0);                     return true;                 case TypeCode.UInt32:                     destination.Set((UInt32)0);                     return true;                 case TypeCode.Boolean:                     destination.Set(false);                     return true;                 case TypeCode.Char:                     destination.Set((Char)0);                     return true;                 case TypeCode.Single:                     destination.Set(0.0f);                     return true;                 case TypeCode.Double:                     destination.Set(0.0d);                     return true;                 case TypeCode.Int64:                     destination.Set((Int64)0);                     return true;                 case TypeCode.UInt64:                     destination.Set((UInt64)0);                     return true;                 case TypeCode.Decimal:                     destination.Set((Decimal)0);                     return true;                 case TypeCode.DateTime:                     destination.Set(DateTime.MinValue);                     return true;                 case TypeCode.String:                     destination.Set(String.Empty);                     return true;                 default:                     return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, String source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                     {                         SByte value;                         if (String.IsNullOrWhiteSpace(source) ||                             !SByte.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Byte:                     {                         Byte value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Byte.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Int16:                     {                         Int16 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int16.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt16:                     {                         UInt16 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt16.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Int32:                     {                         Int32 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int32.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt32:                     {                         UInt32 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt32.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Boolean:                         destination.Set(ConvertToBoolean(source, stringIsLocalized));                         return true;                     case TypeCode.Single:
                        destination.Set(ConvertToSingle(source, stringIsLocalized));                         return true;                     case TypeCode.Double:                         destination.Set(ConvertToDouble(source, stringIsLocalized));                         return true;                     case TypeCode.Int64:                     {                         Int64 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int64.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt64:                     {                         UInt64 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt64.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Decimal:                     {                         Decimal value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Decimal.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.DateTime:                     {                         DateTime value;                         if (String.IsNullOrWhiteSpace(source) ||                             !DateTime.TryParse(source, GetCultureInfo(stringIsLocalized), DateTimeStyles.None, out value))                         {                             value = DateTime.MinValue;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.String:                         destination.Set(source);                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }                                        /// <summary>            ///     asType is Enum         /// </summary>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private object? ValueAsIsEnum(Type asType, bool stringIsLocalized)         {             if (ValueTypeCode == TypeCode.String)             {                 string stringValue = ValueAsString(stringIsLocalized);                 if (stringValue != @"")
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
                }                                           }             try
            {
                return Enum.ToObject(asType, ValueAsInt32(stringIsLocalized));
            }
            catch
            {
            }             return Activator.CreateInstance(asType);         }          /// <summary>         ///     asType has System.TypeCode.Object, _valueTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private object? ValueAsObject(Type asType, bool stringIsLocalized)         {             if (_valueTypeCode == (byte)TypeCode.Empty)             {                 if (asType.IsClass)                 {                     return null;                 }                 else                 {                     try                     {                         return Activator.CreateInstance(asType);                     }                     catch (Exception)                     {                     }                     return null;                 }                             }              if (_valueTypeCode == (byte)TypeCode.String)             {                                 if ((string)StorageObject! == @"")                 {                     try                     {                         return Activator.CreateInstance(asType);                     }                     catch (Exception)                     {                                             }                     return null;                 }             }              TypeConverter converter = TypeDescriptor.GetConverter(asType);             if (converter.CanConvertFrom(ValueType))             {                 try                 {                     return converter.ConvertFrom(null, GetCultureInfo(stringIsLocalized), ValueAsObject()!); // _valueTypeCode == (byte)TypeCode.Empty handled earlier.
                }                 catch (Exception)                 {                 }                             }              try             {                 return Activator.CreateInstance(asType);             }             catch (Exception)             {             }             return null;         }

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
        [FieldOffset(8)]         public DateTime StorageDateTime;        

        #endregion 
        #region private fields

        [FieldOffset(24)]         private byte _valueTypeCode;

        #endregion 
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