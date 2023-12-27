using Ssz.Utils.Serialization; using System; using System.Collections.Generic;
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
        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public Any(object? value)         {                         Unsafe.SkipInit(out _typeCode);             Unsafe.SkipInit(out _storageSByte);             Unsafe.SkipInit(out _storageByte);             Unsafe.SkipInit(out _storageInt16);             Unsafe.SkipInit(out _storageUInt16);            
            Unsafe.SkipInit(out _storageInt32);
            Unsafe.SkipInit(out _storageUInt32);
            Unsafe.SkipInit(out _storageBoolean);
            Unsafe.SkipInit(out _storageChar);
            Unsafe.SkipInit(out _storageSingle);
            Unsafe.SkipInit(out _storageDouble);
            Unsafe.SkipInit(out _storageInt64);
            Unsafe.SkipInit(out _storageUInt64);
            Unsafe.SkipInit(out _storageDecimal);
            Unsafe.SkipInit(out _storageDateTime);
            Unsafe.SkipInit(out _storageObject);

            Set(value);         }                  /// <summary>         ///          /// </summary>         /// <param name="that"></param>         public Any(Any that)         {             Unsafe.SkipInit(out _typeCode);             Unsafe.SkipInit(out _storageSByte);             Unsafe.SkipInit(out _storageByte);             Unsafe.SkipInit(out _storageInt16);             Unsafe.SkipInit(out _storageUInt16);
            Unsafe.SkipInit(out _storageInt32);
            Unsafe.SkipInit(out _storageUInt32);
            Unsafe.SkipInit(out _storageBoolean);
            Unsafe.SkipInit(out _storageChar);
            Unsafe.SkipInit(out _storageSingle);
            Unsafe.SkipInit(out _storageDouble);
            Unsafe.SkipInit(out _storageInt64);
            Unsafe.SkipInit(out _storageUInt64);
            Unsafe.SkipInit(out _storageDecimal);
            Unsafe.SkipInit(out _storageDateTime);
            Unsafe.SkipInit(out _storageObject);

            Set(that);                     }

        #region public functions        
        public static CultureInfo GetCultureInfo(bool localized)         {             if (localized)
                return CultureInfo.CurrentCulture;             return CultureInfo.InvariantCulture;         }

        /// <summary>         ///          /// </summary>         public TypeCode ValueTypeCode         {             get { return (TypeCode)_typeCode; }         }          /// <summary>                 /// </summary>         public Type ValueType         {             get             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return typeof (SByte);                     case TypeCode.Byte:                         return typeof (Byte);                     case TypeCode.Int16:                         return typeof (Int16);                     case TypeCode.UInt16:                         return typeof (UInt16);                     case TypeCode.Int32:                         return typeof (Int32);                     case TypeCode.UInt32:                         return typeof (UInt32);                     case TypeCode.Boolean:                         return typeof (Boolean);                     case TypeCode.Single:                         return typeof (Single);                     case TypeCode.Double:                         return typeof (Double);                     case TypeCode.Empty:                         return typeof (Object);                     case TypeCode.DBNull:                         return typeof (DBNull);                     case TypeCode.Int64:                         return typeof (Int64);                     case TypeCode.UInt64:                         return typeof (UInt64);                     case TypeCode.Decimal:                         return typeof (Decimal);                     case TypeCode.DateTime:                         return typeof (DateTime);                     case TypeCode.String:                         return typeof (String);                     case TypeCode.Char:                         return typeof (Char);                     case TypeCode.Dictionary:                         return typeof(Dictionary<string, Any>);                     case TypeCode.List:                         return typeof(List<Any>);                     case TypeCode.Object:                                                 return _storageObject!.GetType();                 }                 throw new InvalidOperationException();             }         }

        /// <summary>                 /// </summary>         /// <param name="sValue"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public static Any ConvertToBestType(string? sValue, bool stringIsLocalized)         {             if (sValue is null) return new Any(null);             if (String.IsNullOrWhiteSpace(sValue)) return new Any(sValue);             switch (sValue.ToUpperInvariant())             {                 case "TRUE":                     return new Any(true);                 case "FALSE":                     return new Any(false);             }             Double dValue;             bool parsedAsDouble = Double.TryParse(sValue, NumberStyles.Any,                 GetCultureInfo(stringIsLocalized), out dValue);             if (!parsedAsDouble) return new Any(sValue);             bool isInt32 = Math.Abs(dValue % 1) <= Double.Epsilon * 100;             if (isInt32)             {                 isInt32 = Int32.MinValue <= dValue && dValue <= Int32.MaxValue;                 if (isInt32) return new Any((int)dValue);             }             return new Any(dValue);         }          /// <summary>                 /// </summary>         /// <param name="left"></param>         /// <param name="right"></param>         /// <returns></returns>         public static bool operator ==(Any left, Any right)         {             if (left._typeCode != right._typeCode)                 return false;              switch ((TypeCode)left._typeCode)
            {
                case TypeCode.SByte:
                    return left._storageSByte == right._storageSByte;                    
                case TypeCode.Byte:
                    return left._storageByte == right._storageByte;
                case TypeCode.Int16:
                    return left._storageInt16 == right._storageInt16;
                case TypeCode.UInt16:
                    return left._storageUInt16 == right._storageUInt16;
                case TypeCode.Int32:
                    return left._storageInt32 == right._storageInt32;
                case TypeCode.UInt32:
                    return left._storageUInt32 == right._storageUInt32;
                case TypeCode.Boolean:
                    return left._storageBoolean == right._storageBoolean;
                case TypeCode.Char:
                    return left._storageChar == right._storageChar;
                case TypeCode.Single:
                    {                         float leftF = left._storageSingle;                         float rightF = right._storageSingle;                         if (Single.IsNaN(rightF)) return Single.IsNaN(leftF);                         if (Single.IsPositiveInfinity(rightF)) return Single.IsPositiveInfinity(leftF);                         if (Single.IsNegativeInfinity(rightF)) return Single.IsNegativeInfinity(leftF);                         return Math.Abs(leftF - rightF) <= Single.Epsilon * 100;
                    }
                case TypeCode.Double:
                    {                         double leftD = left._storageDouble;                         double rightD = right._storageDouble;                         if (Double.IsNaN(rightD)) return Double.IsNaN(leftD);                         if (Double.IsPositiveInfinity(rightD)) return Double.IsPositiveInfinity(leftD);                         if (Double.IsNegativeInfinity(rightD)) return Double.IsNegativeInfinity(leftD);                         return Math.Abs(leftD - rightD) <= Double.Epsilon * 100;
                    }
                case TypeCode.Empty:
                    return true;
                case TypeCode.DBNull:
                    return true;
                case TypeCode.Int64:
                    return left._storageInt64 == right._storageInt64;
                case TypeCode.UInt64:
                    return left._storageUInt64 == right._storageUInt64;
                case TypeCode.Decimal:
                    return left._storageDecimal == right._storageDecimal;
                case TypeCode.DateTime:
                    return left._storageDateTime == right._storageDateTime;
                case TypeCode.String:
                    return (string)left._storageObject! == (string)right._storageObject!;
                case TypeCode.Dictionary:
                    return ((Dictionary<string, Any>)left._storageObject!).SequenceEqual((Dictionary<string, Any>)right._storageObject!);
                case TypeCode.List:
                    return ((List<Any>)left._storageObject!).SequenceEqual((List<Any>)right._storageObject!);
                case TypeCode.Object:
                    return left._storageObject == right._storageObject;
                default:
                    throw new InvalidOperationException();
            }         }          /// <summary>                 /// </summary>         /// <param name="left"></param>         /// <param name="right"></param>         /// <returns></returns>         public static bool operator !=(Any left, Any right)         {             return !(left == right);         }

        /// <summary>         ///     Compares the current instance with another object of the same type and returns an integer that indicates          ///     whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.         ///     Uses ValueAsDouble(false), ValueAsUInt32(false), ValueAsString(false) depending of ValueTransportType.                 /// </summary>         /// <param name="that"></param>         /// <param name="deadband"></param>         /// <returns></returns>         public int CompareTo(Any that, double deadband)         {
            switch ((TypeCode)_typeCode)
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
                    {                         double d = ValueAsDouble(false);
                        double thatD = that.ValueAsDouble(false);
                        if (Double.IsNaN(thatD))
                            return Double.IsNaN(d) ? 0 : 1;
                        if (Double.IsPositiveInfinity(thatD))
                            return Double.IsPositiveInfinity(d) ? 0 : -1;
                        if (Double.IsNegativeInfinity(thatD))
                            return Double.IsNegativeInfinity(d) ? 0 : 1;
                        double diff = d - thatD;
                        if (diff < -deadband - Double.Epsilon * 100)
                            return -1;
                        if (diff > deadband + Double.Epsilon * 100)
                            return 1;
                        return 0;
                    }
                case TypeCode.Double:
                    {                         double d = ValueAsDouble(false);
                        double thatD = that.ValueAsDouble(false);
                        if (Double.IsNaN(thatD))
                            return Double.IsNaN(d) ? 0 : 1;
                        if (Double.IsPositiveInfinity(thatD))
                            return Double.IsPositiveInfinity(d) ? 0 : -1;
                        if (Double.IsNegativeInfinity(thatD))
                            return Double.IsNegativeInfinity(d) ? 0 : 1;
                        double diff = d - thatD;
                        if (diff < -deadband - Double.Epsilon * 100)
                            return -1;
                        if (diff > deadband + Double.Epsilon * 100)
                            return 1;
                        return 0;
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
                        if (_storageObject is IComparable comparable)
                            return comparable.CompareTo(that.ValueAsObject());
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
            //    int hashCode = _typeCode;
            //    hashCode = (hashCode * 397) ^ (int)_storageUInt32;
            //    hashCode = (hashCode * 397) ^ _storageDouble.GetHashCode();
            //    hashCode = (hashCode * 397) ^ _storageObject.GetHashCode();
            //    return hashCode;
            //}         }          /// <summary>                 /// </summary>         /// <returns></returns>         public override string ToString()         {                         return ValueAsString(false);         }                  /// <summary>         /// </summary>         public void Set(Any value)         {             _typeCode = value._typeCode;             switch ((TypeCode)_typeCode)
            {
                case TypeCode.SByte:
                    _storageSByte = value._storageSByte;
                    _storageObject = null;
                    break;
                case TypeCode.Byte:
                    _storageByte = value._storageByte;
                    _storageObject = null;
                    break;
                case TypeCode.Int16:
                    _storageInt16 = value._storageInt16;
                    _storageObject = null;
                    break;
                case TypeCode.UInt16:
                    _storageUInt16 = value._storageUInt16;
                    _storageObject = null;
                    break;
                case TypeCode.Int32:
                    _storageInt32 = value._storageInt32;
                    _storageObject = null;
                    break;
                case TypeCode.UInt32:
                    _storageUInt32 = value._storageUInt32;
                    _storageObject = null;
                    break;
                case TypeCode.Boolean:
                    _storageBoolean = value._storageBoolean;
                    _storageObject = null;
                    break;
                case TypeCode.Char:
                    _storageChar = value._storageChar;
                    _storageObject = null;
                    break;
                case TypeCode.Single:                    
                    _storageSingle = value._storageSingle;
                    _storageObject = null;
                    break;
                case TypeCode.Double:                    
                    _storageDouble = value._storageDouble;
                    _storageObject = null;
                    break;
                case TypeCode.Empty:
                    _storageObject = null;
                    break;
                case TypeCode.DBNull:
                    _storageObject = DBNull.Value;
                    break;
                case TypeCode.Int64:
                    _storageInt64 = value._storageInt64;
                    _storageObject = null;
                    break;
                case TypeCode.UInt64:
                    _storageUInt64 = value._storageUInt64;
                    _storageObject = null;
                    break;
                case TypeCode.Decimal:
                    _storageDecimal = value._storageDecimal;
                    _storageObject = null;
                    break;
                case TypeCode.DateTime:
                    _storageDateTime = value._storageDateTime;
                    _storageObject = null;
                    break;
                case TypeCode.String:
                    _storageObject = value._storageObject;
                    break;
                case TypeCode.Dictionary:
                    _storageObject = value._storageObject;
                    break;
                case TypeCode.List:
                    _storageObject = value._storageObject;
                    break;
                case TypeCode.Object:
                    _storageObject = value._storageObject;
                    break;
                default:
                    throw new InvalidOperationException();
            }                     }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(object? value)         {             if (value is null)
            {
                _typeCode = (byte)TypeCode.Empty;
                _storageObject = null;
                return;
            }
            if (value is Any any)             {                 Set(any);                 return;             }             if (value is Dictionary<string, Any> dictionary)             {                 Set(dictionary);                 return;             }             if (value is List<Any> list)             {                 Set(list);                 return;             }              Type valueType = value.GetType();              if (valueType.IsEnum)             {                 _typeCode = (byte)TypeCode.Object;
                _storageObject = value;                 return;             }

            switch (valueType.Name)             {                 case nameof(SByte):                     Set((SByte)value);                     return;                 case nameof(Byte):                     Set((Byte)value);                     return;                 case nameof(Int16):                     Set((Int16)value);                     return;                 case nameof(UInt16):                     Set((UInt16)value);                     return;                 case nameof(Int32):                     Set((Int32)value);                     return;                 case nameof(UInt32):                     Set((UInt32)value);                     return;                 case nameof(Boolean):                     Set((Boolean)value);                     return;                 case nameof(Single):                     Set((Single)value);                     return;                 case nameof(Double):                     Set((Double)value);                     return;                                 case nameof(DBNull):                     Set((DBNull)value);                     return;                 case nameof(Int64):                     Set((Int64)value);                     return;                 case nameof(UInt64):                     Set((UInt64)value);                     return;                 case nameof(Decimal):                     Set((Decimal)value);                     return;                 case nameof(DateTime):                     Set((DateTime)value);                     return;                 case nameof(Char):                     Set((Char)value);                     return;                 case nameof(String):                     Set((String)value);                     return;                 default:                     _typeCode = (byte)TypeCode.Object;
                    _storageObject = value;                     return;             }
        }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(SByte value)         {             _typeCode = (byte)TypeCode.SByte;             _storageSByte = value;                         _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Byte value)         {             _typeCode = (byte)TypeCode.Byte;             _storageByte = value;                         _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Int16 value)         {             _typeCode = (byte)TypeCode.Int16;             _storageInt16 = value;                         _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(UInt16 value)         {             _typeCode = (byte)TypeCode.UInt16;             _storageUInt16 = value;                         _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Int32 value)         {             _typeCode = (byte)TypeCode.Int32;             _storageInt32 = value;                         _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(UInt32 value)         {             _typeCode = (byte)TypeCode.UInt32;             _storageUInt32 = value;                         _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Boolean value)         {             _typeCode = (byte)TypeCode.Boolean;             _storageBoolean = value;                         _storageObject = null;         }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Char value)         {             _typeCode = (byte)TypeCode.UInt32;             _storageChar = value;
            _storageObject = null;         }

        /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Single value)         {             _typeCode = (byte)TypeCode.Single;
            _storageSingle = value;             _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Double value)         {             _typeCode = (byte)TypeCode.Double;                         _storageDouble = value;             _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Decimal value)         {             _typeCode = (byte)TypeCode.Decimal;
            _storageDecimal = value;             _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(Int64 value)         {             _typeCode = (byte)TypeCode.Int64;
            _storageInt64 = value;             _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(UInt64 value)         {             _typeCode = (byte)TypeCode.UInt64;
            _storageUInt64 = value;             _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(DateTime value)         {             _typeCode = (byte)TypeCode.DateTime;
            _storageDateTime = value;             _storageObject = null;         }          /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(DBNull value)         {             _typeCode = (byte)TypeCode.DBNull;                         _storageObject = value;         }                  /// <summary>         ///          /// </summary>         /// <param name="value"></param>         public void Set(String value)         {                         _typeCode = (byte)TypeCode.String;                         _storageObject = value;         }          public void Set(Dictionary<string, Any> value)         {
            _typeCode = (byte)TypeCode.Dictionary;
            _storageObject = value;         }

        public void Set(List<Any> value)         {
            _typeCode = (byte)TypeCode.List;
            _storageObject = value;         }

        /// <summary>         ///          /// </summary>         /// <returns></returns>         public object? ValueAsObject()         {             try             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return _storageSByte;                     case TypeCode.Byte:                         return _storageByte;                     case TypeCode.Int16:                         return _storageInt16;                     case TypeCode.UInt16:                         return _storageUInt16;                     case TypeCode.Int32:                         return _storageInt32;                     case TypeCode.UInt32:                         return _storageUInt32;                     case TypeCode.Boolean:                         return _storageBoolean;                     case TypeCode.Char:                         return _storageChar;                     case TypeCode.Single:                         return _storageSingle;                     case TypeCode.Double:                         return _storageDouble;                     case TypeCode.Empty:                         return null;                     case TypeCode.DBNull:                         return _storageObject;                     case TypeCode.Int64:                         return _storageInt64;                     case TypeCode.UInt64:                         return _storageUInt64;                     case TypeCode.Decimal:                         return _storageDecimal;                     case TypeCode.DateTime:                         return _storageDateTime;                                         case TypeCode.String:                         return _storageObject;                     case TypeCode.Dictionary:                         return _storageObject;                     case TypeCode.List:                         return _storageObject;                     case TypeCode.Object:                         return _storageObject;                 }             }             catch (Exception)             {                 return null;             }              throw new InvalidOperationException();         }          /// <summary>                 /// </summary>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public String ValueAsString(bool stringIsLocalized, string? stringFormat = null)         {             try             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return _storageSByte.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Byte:                         return _storageByte.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Int16:                         return _storageInt16.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt16:                         return _storageUInt16.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Int32:                         return _storageInt32.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt32:                         return _storageUInt32.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Boolean:                         return _storageBoolean.ToString(GetCultureInfo(stringIsLocalized));                     case TypeCode.Char:                         return _storageChar.ToString();                     case TypeCode.Single:                         return _storageSingle.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Double:                         return _storageDouble.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Empty:                         return String.Empty;                     case TypeCode.DBNull:                                                 return ((DBNull)_storageObject!).ToString(GetCultureInfo(stringIsLocalized));                     case TypeCode.Int64:                                                 return _storageInt64.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.UInt64:                                                 return _storageUInt64.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.Decimal:                                                 return _storageDecimal.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                     case TypeCode.DateTime:                                                 return _storageDateTime.ToString(stringFormat, GetCultureInfo(stringIsLocalized));                                             case TypeCode.String:                                                 return (String)_storageObject!;
                    case TypeCode.Dictionary:                         return _storageObject!.ToString() ?? @"";                     case TypeCode.List:                         return _storageObject!.ToString() ?? @"";                     case TypeCode.Object:                         return ConvertToString(_storageObject, stringIsLocalized);                 }             }             catch (Exception)             {                 return String.Empty;             }              throw new InvalidOperationException();         }          /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public Int32 ValueAsInt32(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return _storageSByte;                     case TypeCode.Byte:                         return _storageByte;                     case TypeCode.Int16:                         return _storageInt16;                     case TypeCode.UInt16:                         return _storageUInt16;                     case TypeCode.Int32:                         return _storageInt32;                     case TypeCode.UInt32:                         return (Int32) _storageUInt32;                     case TypeCode.Boolean:                         return _storageBoolean ? 1 : 0;                     case TypeCode.Char:                         return _storageChar;                     case TypeCode.Single:                         return (Int32)_storageSingle;                     case TypeCode.Double:                         return (Int32)_storageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (Int32)_storageInt64;                     case TypeCode.UInt64:                                                 return (Int32)_storageUInt64;                     case TypeCode.Decimal:                                                 return (Int32)_storageDecimal;                     case TypeCode.DateTime:                         return 0;                     case TypeCode.String:                                                 Int32 result;                         if (!Int32.TryParse((string)_storageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(_storageObject!, typeof(int), stringIsLocalized);                         if (obj is null)                              return 0;                         else return (int)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public UInt32 ValueAsUInt32(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return (UInt32)_storageSByte;                     case TypeCode.Byte:                         return (UInt32)_storageByte;                     case TypeCode.Int16:                         return (UInt32)_storageInt16;                     case TypeCode.UInt16:                         return (UInt32)_storageUInt16;                     case TypeCode.Int32:                         return (UInt32)_storageInt32;                     case TypeCode.UInt32:                         return _storageUInt32;                     case TypeCode.Boolean:                         return _storageBoolean ? 1u : 0u;                     case TypeCode.Char:                         return (UInt32)_storageChar;                     case TypeCode.Single:                         return (UInt32)_storageSingle;                     case TypeCode.Double:                         return (UInt32)_storageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (UInt32)_storageInt64;                     case TypeCode.UInt64:                                                 return (UInt32)_storageUInt64;                     case TypeCode.Decimal:                                                 return (UInt32)_storageDecimal;                     case TypeCode.DateTime:                         return 0;                     case TypeCode.String:                                                 UInt32 result;                         if (!UInt32.TryParse((string)_storageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(_storageObject!, typeof(UInt32), stringIsLocalized);                         if (obj is null) return 0;                         else return (UInt32)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          public Int64 ValueAsInt64(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return (Int64)_storageSByte;                     case TypeCode.Byte:                         return (Int64)_storageByte;                     case TypeCode.Int16:                         return (Int64)_storageInt16;                     case TypeCode.UInt16:                         return (Int64)_storageUInt16;                     case TypeCode.Int32:                         return (Int64)_storageInt32;                     case TypeCode.UInt32:                         return (Int64)_storageUInt32;                     case TypeCode.Boolean:                         return _storageBoolean ? 1L : 0L;                     case TypeCode.Char:                         return (Int64)_storageChar;                     case TypeCode.Single:                         return (Int64)_storageSingle;                     case TypeCode.Double:                         return (Int64)_storageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:
                        return (Int64)_storageInt64;                     case TypeCode.UInt64:
                        return (Int64)_storageUInt64;                     case TypeCode.Decimal:
                        return (Int64)_storageDecimal;                     case TypeCode.DateTime:                         return 0;                     case TypeCode.String:
                        Int64 result;                         if (!Int64.TryParse((string)_storageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:
                        object? obj = TypeCodeObject_ValueAs(_storageObject!, typeof(Int64), stringIsLocalized);                         if (obj is null)                             return 0;                         else return (Int64)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }          public UInt64 ValueAsUInt64(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return (UInt64)_storageSByte;                     case TypeCode.Byte:                         return (UInt64)_storageByte;                     case TypeCode.Int16:                         return (UInt64)_storageInt16;                     case TypeCode.UInt16:                         return (UInt64)_storageUInt16;                     case TypeCode.Int32:                         return (UInt64)_storageInt32;                     case TypeCode.UInt32:                         return (UInt64)_storageUInt32;                     case TypeCode.Boolean:                         return _storageBoolean ? 1ul : 0ul;                     case TypeCode.Char:                         return (UInt64)_storageChar;                     case TypeCode.Single:                         return (UInt64)_storageSingle;                     case TypeCode.Double:                         return (UInt64)_storageDouble;                     case TypeCode.Empty:                         return 0;                     case TypeCode.DBNull:                         return 0;                     case TypeCode.Int64:                                                 return (UInt64)_storageInt64;                     case TypeCode.UInt64:                                                 return (UInt64)_storageUInt64;                     case TypeCode.Decimal:                                                 return (UInt64)_storageDecimal;                     case TypeCode.DateTime:                         return 0;                     case TypeCode.String:                                                 UInt64 result;                         if (!UInt64.TryParse((string)_storageObject!, NumberStyles.Integer,                                 GetCultureInfo(stringIsLocalized), out result))
                            return 0;                         return result;                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(_storageObject!, typeof(UInt64), stringIsLocalized);                         if (obj is null)                              return 0;                         else return (UInt64)obj;                 }             }             catch (Exception)             {                 return 0;             }              throw new InvalidOperationException();         }        

        /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public Double ValueAsDouble(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return _storageSByte;                     case TypeCode.Byte:                         return _storageByte;                     case TypeCode.Int16:                         return _storageInt16;                     case TypeCode.UInt16:                         return _storageUInt16;                     case TypeCode.Int32:                         return _storageInt32;                     case TypeCode.UInt32:                         return _storageUInt32;                     case TypeCode.Boolean:                         return _storageBoolean ? 1d : 0d;                     case TypeCode.Char:                         return _storageChar;                     case TypeCode.Single:                         return _storageSingle;                     case TypeCode.Double:                         return _storageDouble;                     case TypeCode.Empty:                         return 0.0d;                     case TypeCode.DBNull:                         return 0.0d;                     case TypeCode.Int64:                                                 return _storageInt64;                     case TypeCode.UInt64:                                                 return _storageUInt64;                     case TypeCode.Decimal:                                                 return (Double)_storageDecimal;                     case TypeCode.DateTime:                         return 0.0d;                     case TypeCode.String:                                                 return ConvertToDouble((string)_storageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return 0;                     case TypeCode.List:                         return 0;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(_storageObject!, typeof(double), stringIsLocalized);                         if (obj is null)                             return 0.0d;                         else return (double)obj;                 }             }             catch (Exception)             {                 return 0.0d;             }              throw new InvalidOperationException();         }          /// <summary>         ///          /// </summary>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         public bool ValueAsBoolean(bool stringIsLocalized)         {             try             {                 switch ((TypeCode)_typeCode)                 {                     case TypeCode.SByte:                         return _storageSByte != 0;                     case TypeCode.Byte:                         return _storageByte != 0;                     case TypeCode.Int16:                         return _storageInt16 != 0;                     case TypeCode.UInt16:                         return _storageUInt16 != 0;                     case TypeCode.Int32:                         return _storageInt32 != 0;                     case TypeCode.UInt32:                         return _storageUInt32 != 0;                     case TypeCode.Boolean:                         return _storageBoolean;                     case TypeCode.Char:                         return _storageChar != 0;                     case TypeCode.Single:                         return _storageSingle != 0.0 && !Single.IsNaN(_storageSingle);                     case TypeCode.Double:                         return _storageDouble != 0.0 && !Double.IsNaN(_storageDouble);                     case TypeCode.Empty:                         return false;                     case TypeCode.DBNull:                         return false;                     case TypeCode.Int64:                                                 return _storageInt64 != 0;                     case TypeCode.UInt64:                                                 return _storageUInt64 != 0;                     case TypeCode.Decimal:                                                 return _storageDecimal != (decimal) 0.0;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                                                 return ConvertToBoolean((string)_storageObject!, stringIsLocalized);                     case TypeCode.Dictionary:                         return false;                     case TypeCode.List:                         return false;                     case TypeCode.Object:                                                 object? obj = TypeCodeObject_ValueAs(_storageObject!, typeof(bool), stringIsLocalized);                         if (obj is null)                             return false;                         else return (bool)obj;                 }             }             catch (Exception)             {                 return false;             }              throw new InvalidOperationException();         }          public Dictionary<string, Any> ValueAsDictionary()         {             if ((TypeCode)_typeCode == TypeCode.Dictionary)                 return (Dictionary<string, Any>)_storageObject!;             else                 return new();         }

        public List<Any> ValueAsList()         {             if ((TypeCode)_typeCode == TypeCode.List)                 return (List<Any>)_storageObject!;             else                 return new();         }

        /// <summary>         ///          /// </summary>         /// <typeparam name="T"></typeparam>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public T? ValueAs<T>(bool stringIsLocalized, string? stringFormat = null)             where T : notnull         {             return (T?)ValueAs(typeof(T), stringIsLocalized, stringFormat);         }          /// <summary>         ///     Returns requested type or null.          /// </summary>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         public object? ValueAs(Type? asType, bool stringIsLocalized, string? stringFormat = null)         {             if (asType is null || asType == typeof(object) || asType == ValueType)             {                 return ValueAsObject();             }              if (asType.IsEnum)             {                 return ValueAsIsEnum(asType, stringIsLocalized);                             }                          if (_typeCode == (byte)TypeCode.Object)             {                                 return TypeCodeObject_ValueAs(_storageObject!, asType, stringIsLocalized);
            }

            TypeCode asTypeCode;
            if (asType.IsGenericType)             {                 return null;
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

        public void SerializeOwnedData(SerializationWriter writer, object? context)         {             writer.Write(_typeCode);             switch ((TypeCode)_typeCode)
            {
                case TypeCode.SByte:
                    writer.Write(_storageSByte);
                    break;
                case TypeCode.Byte:
                    writer.Write(_storageByte);
                    break;
                case TypeCode.Int16:
                    writer.Write(_storageInt16);
                    break;
                case TypeCode.UInt16:
                    writer.Write(_storageUInt16);
                    break;
                case TypeCode.Int32:
                    writer.Write(_storageInt32);
                    break;
                case TypeCode.UInt32:
                    writer.Write(_storageUInt32);
                    break;
                case TypeCode.Boolean:
                    writer.Write(_storageBoolean);
                    break;
                case TypeCode.Char:
                    writer.Write(_storageChar);
                    break;
                case TypeCode.Single:
                    writer.Write(_storageSingle);
                    break;
                case TypeCode.Double:
                    writer.Write(_storageDouble);
                    break;
                case TypeCode.Empty:
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.Int64:
                    writer.Write(_storageInt64);
                    break;
                case TypeCode.UInt64:
                    writer.Write(_storageUInt64);
                    break;
                case TypeCode.Decimal:
                    writer.Write(_storageDecimal);
                    break;
                case TypeCode.DateTime:
                    writer.Write(_storageDateTime);
                    break;
                case TypeCode.String:
                    writer.Write((String)_storageObject!);
                    break;
                case TypeCode.Dictionary:
                    writer.WriteDictionaryOfOwnedDataSerializable((Dictionary<string, Any>)_storageObject!, null);
                    break;
                case TypeCode.List:
                    writer.WriteListOfOwnedDataSerializable((List<Any>)_storageObject!, null);
                    break;
                case TypeCode.Object:
                    writer.WriteObject(_storageObject);
                    break;
                default:
                    throw new InvalidOperationException();
            }         }          public void DeserializeOwnedData(SerializationReader reader, object? context)         {             _typeCode = reader.ReadByte();                         switch ((TypeCode)_typeCode)
            {
                case TypeCode.SByte:
                    _storageSByte = reader.ReadSByte();                     _storageObject = null;
                    break;
                case TypeCode.Byte:
                    _storageByte = reader.ReadByte();                     _storageObject = null;
                    break;
                case TypeCode.Int16:
                    _storageInt16 = reader.ReadInt16();                     _storageObject = null;
                    break;
                case TypeCode.UInt16:
                    _storageUInt16 = reader.ReadUInt16();                     _storageObject = null;
                    break;
                case TypeCode.Int32:
                    _storageInt32 = reader.ReadInt32();                     _storageObject = null;
                    break;
                case TypeCode.UInt32:
                    _storageUInt32 = reader.ReadUInt32();                     _storageObject = null;
                    break;
                case TypeCode.Boolean:
                    _storageBoolean = reader.ReadBoolean();                     _storageObject = null;
                    break;
                case TypeCode.Char:
                    _storageChar = reader.ReadChar();                     _storageObject = null;
                    break;
                case TypeCode.Single:
                    _storageSingle = reader.ReadSingle();                     _storageObject = null;
                    break;
                case TypeCode.Double:
                    _storageDouble = reader.ReadDouble();                     _storageObject = null;
                    break;
                case TypeCode.Empty:
                    _storageObject = null;
                    break;
                case TypeCode.DBNull:
                    _storageObject = DBNull.Value;
                    break;
                case TypeCode.Int64:
                    _storageInt64 = reader.ReadInt64();
                    _storageObject = null;
                    break;
                case TypeCode.UInt64:
                    _storageUInt64 = reader.ReadUInt64();
                    _storageObject = null;
                    break;
                case TypeCode.Decimal:
                    _storageDecimal = reader.ReadDecimal();
                    _storageObject = null;
                    break;
                case TypeCode.DateTime:
                    _storageDateTime = reader.ReadDateTime();
                    _storageObject = null;
                    break;
                case TypeCode.String:
                    _storageObject = reader.ReadString();                    
                    break;
                case TypeCode.Dictionary:                   
                    _storageObject = reader.ReadDictionaryOfOwnedDataSerializable(() => new Any(), null);
                    break;
                case TypeCode.List:
                    _storageObject = reader.ReadListOfOwnedDataSerializable(() => new Any(), null);
                    break;
                case TypeCode.Object:
                    _storageObject = reader.ReadObject()!;                    
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
                }                             }              try             {                 return Activator.CreateInstance(asType);             }             catch (Exception)             {             }             return null;         }          /// <summary>                 /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static string ConvertToString(object? value, bool stringIsLocalized)         {             if (value is null) return String.Empty;              Type type = value.GetType();             if (type == typeof(object))                  return String.Empty;              // TODO:             //System.Windows.Markup.ValueSerializer valueSerializer =             //                ValueSerializer.GetSerializerFor(type);             //if (valueSerializer is not null && valueSerializer.CanConvertToString(value, null))             //{             //    try             //    {             //        string result = valueSerializer.ConvertToString(value, null);             //        if (result is not null) return result;             //    }             //    catch (Exception)             //    {             //    }             //}              TypeConverter converter = TypeDescriptor.GetConverter(type);             if (converter.CanConvertTo(typeof(string)))             {                 try                 {                     string? result = converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), value, typeof(string)) as string;                     if (result is not null) return result;                 }                 catch (Exception)                 {                 }             }              return value.ToString() ?? @"";         }          /// <summary>         ///     Returns false, if String.IsNullOrWhiteSpace(value) || value.ToUpperInvariant() == "FALSE" || value == "0",         ///     otherwise true.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static bool ConvertToBoolean(string? value, bool stringIsLocalized)         {             Any any = ConvertToBestType(value, stringIsLocalized);             if (any._typeCode == (byte)TypeCode.String)                  return false;             return any.ValueAsBoolean(stringIsLocalized);         }          /// <summary>         ///     Returns Double 0.0 if String.IsNullOrWhiteSpace(value) or value is not correct number.         /// </summary>         /// <param name="value"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private static double ConvertToDouble(string value, bool stringIsLocalized)         {             double result;             if (String.IsNullOrWhiteSpace(value) || !Double.TryParse(value, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out result))
                result = 0.0d;             return result;         }          /// <summary>         ///     Returns true, if succeeded.         ///     if conversion fails, destination doesn't change.         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object, source.ValueTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"> </param>         /// <param name="source"> </param>         /// <param name="toTypeCode"> </param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns> true if succeded, false otherwise </returns>         private static bool Convert(ref Any destination, Any source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat = null)         {                         switch (source.ValueTypeCode)             {                 case TypeCode.SByte:                     return Convert(ref destination, source._storageSByte, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Byte:                     return Convert(ref destination, source._storageByte, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Int16:                     return Convert(ref destination, source._storageInt16, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt16:                     return Convert(ref destination, source._storageUInt16, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Int32:                     return Convert(ref destination, source._storageInt32, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt32:                     return Convert(ref destination, source._storageUInt32, toTypeCode, stringIsLocalized,                          stringFormat);                 case TypeCode.Boolean:                     return Convert(ref destination, source._storageBoolean, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Char:                     return Convert(ref destination, source._storageChar, toTypeCode, stringIsLocalized,                          stringFormat);                 case TypeCode.Single:                     return Convert(ref destination, source._storageSingle, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Double:                     return Convert(ref destination, source._storageDouble, toTypeCode, stringIsLocalized, stringFormat);                 case TypeCode.Empty:                     return ConvertFromNullOrDBNull(ref destination, toTypeCode);                 case TypeCode.DBNull:                     return ConvertFromNullOrDBNull(ref destination, toTypeCode);                 case TypeCode.Int64:                                         return Convert(ref destination, source._storageInt64, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.UInt64:                                         return Convert(ref destination, source._storageUInt64, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.Decimal:                                         return Convert(ref destination, source._storageDecimal, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.String:                                         return Convert(ref destination, (String)source._storageObject!, toTypeCode, stringIsLocalized,                         stringFormat);                 case TypeCode.DateTime:                                         return Convert(ref destination, source._storageDateTime, toTypeCode, stringIsLocalized,                         stringFormat);                             }              throw new InvalidOperationException();         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, SByte source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set(source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Byte source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set(source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int16 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set(source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt16 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set(source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int32 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set(source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt32 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set(source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Char source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32 ) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set(source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString());                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Int64 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set(source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, UInt64 source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set(source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Boolean source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) (source ? 1 : 0));                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) (source ? 1 : 0));                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) (source ? 1 : 0));                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) (source ? 1 : 0));                         return true;                     case TypeCode.Int32:                         destination.Set((source ? 1 : 0));                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) (source ? 1 : 0));                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) (source ? 1 : 0));                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) (source ? 1 : 0));                         return true;                     case TypeCode.Boolean:                         destination.Set(source);                         return true;                     case TypeCode.Char:                         destination.Set(source ? 'Y' : 'N');                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.Single:                         destination.Set((Single) (source ? 1 : 0));                         return true;                     case TypeCode.Double:                         destination.Set((Double) (source ? 1 : 0));                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) (source ? 1 : 0));                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, DateTime source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.DateTime:                         destination.Set(source);                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Single source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0.0 && !Single.IsNaN(source));                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.Single:                         destination.Set(source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Double source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != 0.0 && !Double.IsNaN(source));                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set(source);                         return true;                     case TypeCode.Decimal:                         destination.Set((Decimal) source);                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, Decimal source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                         destination.Set((SByte) source);                         return true;                     case TypeCode.Byte:                         destination.Set((Byte) source);                         return true;                     case TypeCode.Int16:                         destination.Set((Int16) source);                         return true;                     case TypeCode.UInt16:                         destination.Set((UInt16) source);                         return true;                     case TypeCode.Int32:                         destination.Set((Int32) source);                         return true;                     case TypeCode.UInt32:                         destination.Set((UInt32) source);                         return true;                     case TypeCode.Int64:                         destination.Set((Int64) source);                         return true;                     case TypeCode.UInt64:                         destination.Set((UInt64) source);                         return true;                     case TypeCode.Boolean:                         destination.Set(source != (decimal) 0.0);                         return true;                     case TypeCode.Char:                         destination.Set((Char) source);                         return true;                     case TypeCode.DateTime:                         return false;                     case TypeCode.Single:                         destination.Set((Single) source);                         return true;                     case TypeCode.Double:                         destination.Set((Double) source);                         return true;                     case TypeCode.Decimal:                         destination.Set(source);                         return true;                     case TypeCode.String:                         destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="toTypeCode"></param>         /// <returns></returns>         private static bool ConvertFromNullOrDBNull(ref Any destination, TypeCode toTypeCode)         {             switch (toTypeCode)             {                 case TypeCode.SByte:                     destination.Set((SByte)0);                     return true;                 case TypeCode.Byte:                     destination.Set((Byte)0);                     return true;                 case TypeCode.Int16:                     destination.Set((Int16)0);                     return true;                 case TypeCode.UInt16:                     destination.Set((UInt16)0);                     return true;                 case TypeCode.Int32:                     destination.Set(0);                     return true;                 case TypeCode.UInt32:                     destination.Set((UInt32)0);                     return true;                 case TypeCode.Boolean:                     destination.Set(false);                     return true;                 case TypeCode.Char:                     destination.Set((Char)0);                     return true;                 case TypeCode.Single:                     destination.Set(0.0f);                     return true;                 case TypeCode.Double:                     destination.Set(0.0d);                     return true;                 case TypeCode.Int64:                     destination.Set((Int64)0);                     return true;                 case TypeCode.UInt64:                     destination.Set((UInt64)0);                     return true;                 case TypeCode.Decimal:                     destination.Set((Decimal)0);                     return true;                 case TypeCode.DateTime:                     destination.Set(DateTime.MinValue);                     return true;                 case TypeCode.String:                     destination.Set(String.Empty);                     return true;                 default:                     return false;             }         }          /// <summary>         ///     toTypeCode != (byte)TypeCode.Empty, toTypeCode != (byte)TypeCode.DBNull, toTypeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="destination"></param>         /// <param name="source"></param>         /// <param name="toTypeCode"></param>         /// <param name="stringIsLocalized"></param>         /// <param name="stringFormat"></param>         /// <returns></returns>         private static bool Convert(ref Any destination, String source, TypeCode toTypeCode, bool stringIsLocalized,             string? stringFormat)         {             try             {                 switch (toTypeCode)                 {                     case TypeCode.SByte:                     {                         SByte value;                         if (String.IsNullOrWhiteSpace(source) ||                             !SByte.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Byte:                     {                         Byte value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Byte.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Int16:                     {                         Int16 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int16.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt16:                     {                         UInt16 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt16.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Int32:                     {                         Int32 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int32.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt32:                     {                         UInt32 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt32.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Boolean:                         destination.Set(ConvertToBoolean(source, stringIsLocalized));                         return true;                     case TypeCode.Single:                     {                         Single value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Single.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0.0f;                         }                                                 destination.Set(value);                     }                                                 return true;                     case TypeCode.Double:                         destination.Set(ConvertToDouble(source, stringIsLocalized));                         return true;                     case TypeCode.Int64:                     {                         Int64 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Int64.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.UInt64:                     {                         UInt64 value;                         if (String.IsNullOrWhiteSpace(source) ||                             !UInt64.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.Decimal:                     {                         Decimal value;                         if (String.IsNullOrWhiteSpace(source) ||                             !Decimal.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))                         {                             value = 0;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.DateTime:                     {                         DateTime value;                         if (String.IsNullOrWhiteSpace(source) ||                             !DateTime.TryParse(source, GetCultureInfo(stringIsLocalized), DateTimeStyles.None, out value))                         {                             value = DateTime.MinValue;                         }                         destination.Set(value);                     }                         return true;                     case TypeCode.String:                         destination.Set(source);                         return true;                     default:                         return false;                 }             }             catch             {                 return false;             }         }                                        /// <summary>            ///     asType is Enum         /// </summary>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private object? ValueAsIsEnum(Type asType, bool stringIsLocalized)         {             if (ValueTypeCode == TypeCode.String)             {                 string stringValue = ValueAsString(stringIsLocalized);                 if (stringValue != @"")
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
            }             return Activator.CreateInstance(asType);         }          /// <summary>         ///     asType has System.TypeCode.Object, _typeCode != (byte)TypeCode.Object         /// </summary>         /// <param name="asType"></param>         /// <param name="stringIsLocalized"></param>         /// <returns></returns>         private object? ValueAsObject(Type asType, bool stringIsLocalized)         {             if (_typeCode == (byte)TypeCode.Empty)             {                 if (asType.IsClass)                 {                     return null;                 }                 else                 {                     try                     {                         return Activator.CreateInstance(asType);                     }                     catch (Exception)                     {                     }                     return null;                 }                             }              if (_typeCode == (byte)TypeCode.String)             {                                 if ((string)_storageObject! == @"")                 {                     try                     {                         return Activator.CreateInstance(asType);                     }                     catch (Exception)                     {                                             }                     return null;                 }             }              TypeConverter converter = TypeDescriptor.GetConverter(asType);             if (converter.CanConvertFrom(ValueType))             {                 try                 {                     return converter.ConvertFrom(null, GetCultureInfo(stringIsLocalized), ValueAsObject()!); // _typeCode == (byte)TypeCode.Empty handled earlier.
                }                 catch (Exception)                 {                 }                             }              try             {                 return Activator.CreateInstance(asType);             }             catch (Exception)             {             }             return null;         }

        #endregion 
        #region private fields       
        [FieldOffset(0)]         private object? _storageObject;          [FieldOffset(8)]         private sbyte _storageSByte;          [FieldOffset(8)]         private byte _storageByte;          [FieldOffset(8)]         private short _storageInt16;          [FieldOffset(8)]         private ushort _storageUInt16;          [FieldOffset(8)]         private int _storageInt32;          [FieldOffset(8)]         private uint _storageUInt32;          [FieldOffset(8)]         private bool _storageBoolean;          [FieldOffset(8)]         private char _storageChar;

        /// <summary>
        ///     4 bytes
        /// </summary>         [FieldOffset(8)]         private float _storageSingle;

        /// <summary>
        ///     8 bytes
        /// </summary>         [FieldOffset(8)]         private double _storageDouble;

        [FieldOffset(8)]         private long _storageInt64;

        [FieldOffset(8)]         private ulong _storageUInt64;

        /// <summary>
        ///     16 bytes
        /// </summary>
        [FieldOffset(8)]         private decimal _storageDecimal;

        [FieldOffset(8)]         private DateTime _storageDateTime;        

        [FieldOffset(24)]         private byte _typeCode;

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