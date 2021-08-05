using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Markup;

namespace Ssz.Utils
{
    /// <summary>
    ///     If func param stringIsLocalized = False, InvariantCulture is used.
    ///     If func param stringIsLocalized = True, CultureHelper.SystemCultureInfo is used, which is corresponds operating system culture (see CultureHelper class).
    /// </summary>
    [CLSCompliant(false)]
    public struct Any
    {
        #region StorageType enum

        public enum StorageType : short
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

        #endregion

        public Any(object value)
        {
            _typeCode = TypeCode.Empty;
            _storageUInt32 = 0;
            _storageDouble = 0.0d;
            _storageObject = null;

            if (!ReferenceEquals(value, null)) Set(value);
        }

        public Any(Any that)
        {
            _typeCode = that._typeCode;
            switch (that.ValueStorageType)
            {
                case StorageType.Object:
                    _storageUInt32 = 0;
                    _storageDouble = 0.0d;
                    _storageObject = that._storageObject;
                    return;
                case StorageType.Double:
                    _storageUInt32 = 0;
                    _storageDouble = that._storageDouble;
                    _storageObject = null;
                    return;
                case StorageType.UInt32:
                    _storageUInt32 = that._storageUInt32;
                    _storageDouble = 0.0d;
                    _storageObject = null;
                    return;
            }

            throw new InvalidOperationException();
        }

        #region public functions

        public UInt32 StorageUInt32
        {
            get { return _storageUInt32; }
        }

        public Double StorageDouble
        {
            get { return _storageDouble; }
        }

        public Object StorageObject
        {
            get { return _storageObject; }
        }

        public TypeCode ValueTypeCode
        {
            get { return _typeCode; }
        }

        /// <summary>
        ///     Result != null
        /// </summary>
        public Type ValueType
        {
            get
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return typeof(SByte);
                    case TypeCode.Byte:
                        return typeof(Byte);
                    case TypeCode.Int16:
                        return typeof(Int16);
                    case TypeCode.UInt16:
                        return typeof(UInt16);
                    case TypeCode.Int32:
                        return typeof(Int32);
                    case TypeCode.UInt32:
                        return typeof(UInt32);
                    case TypeCode.Boolean:
                        return typeof(Boolean);
                    case TypeCode.Single:
                        return typeof(Single);
                    case TypeCode.Double:
                        return typeof(Double);
                    case TypeCode.Empty:
                        return typeof(object);
                    case TypeCode.DBNull:
                        return typeof(object);
                    case TypeCode.Int64:
                        return typeof(Int64);
                    case TypeCode.UInt64:
                        return typeof(UInt64);
                    case TypeCode.Decimal:
                        return typeof(Decimal);
                    case TypeCode.DateTime:
                        return typeof(DateTime);
                    case TypeCode.String:
                        return typeof(String);
                    case TypeCode.Char:
                        return typeof(Char);
                    case TypeCode.Object:
                        return _storageObject.GetType();
                }
                throw new InvalidOperationException();
            }
        }

        public StorageType ValueStorageType
        {
            get
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return StorageType.UInt32;
                    case TypeCode.Byte:
                        return StorageType.UInt32;
                    case TypeCode.Int16:
                        return StorageType.UInt32;
                    case TypeCode.UInt16:
                        return StorageType.UInt32;
                    case TypeCode.Int32:
                        return StorageType.UInt32;
                    case TypeCode.UInt32:
                        return StorageType.UInt32;
                    case TypeCode.Boolean:
                        return StorageType.UInt32;
                    case TypeCode.Char:
                        return StorageType.UInt32;
                    case TypeCode.Single:
                        return StorageType.Double;
                    case TypeCode.Double:
                        return StorageType.Double;
                    case TypeCode.Empty:
                        return StorageType.Object;
                    case TypeCode.DBNull:
                        return StorageType.Object;
                    case TypeCode.Int64:
                        return StorageType.Object;
                    case TypeCode.UInt64:
                        return StorageType.Object;
                    case TypeCode.Decimal:
                        return StorageType.Object;
                    case TypeCode.DateTime:
                        return StorageType.Object;
                    case TypeCode.String:
                        return StorageType.Object;
                    case TypeCode.Object:
                        return StorageType.Object;
                }
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        ///     Soft compare, converts value if needed
        /// </summary>
        /// <param name="that"></param>
        /// <returns></returns>
        public bool Compare(Any that)
        {
            switch (ValueStorageType)
            {
                case StorageType.Double:
                    return ValueAsDouble(false) == that.ValueAsDouble(false);
                case StorageType.UInt32:
                    return ValueAsInt32(false) == that.ValueAsInt32(false);
                case StorageType.Object:
                    return ValueAsString(false) == that.ValueAsString(false);
            }
            return false;
        }

        /// <summary>
        ///     Strictly copare, no conversions
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Any)) return false;

            return this == (Any)obj;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return ValueAsString(false);
        }

        public static bool operator ==(Any left, Any right)
        {
            if (left._typeCode != right._typeCode) return false;

            switch (left.ValueStorageType)
            {
                case StorageType.Object:
                    return left._storageObject == right._storageObject;
                case StorageType.Double:
                    return left._storageDouble == right._storageDouble;
                case StorageType.UInt32:
                    return left._storageUInt32 == right._storageUInt32;
            }

            return false;
        }

        public static bool operator !=(Any left, Any right)
        {
            return !(left == right);
        }

        public bool Set(UInt32 storageUInt32, TypeCode valueTypeCode, bool stringIsLocalized)
        {
            try
            {
                switch (valueTypeCode)
                {
                    case TypeCode.SByte:
                        Set((SByte)storageUInt32);
                        return true;
                    case TypeCode.Byte:
                        Set((Byte)storageUInt32);
                        return true;
                    case TypeCode.Int16:
                        Set((Int16)storageUInt32);
                        return true;
                    case TypeCode.UInt16:
                        Set((UInt16)storageUInt32);
                        return true;
                    case TypeCode.Int32:
                        Set((Int32)storageUInt32);
                        return true;
                    case TypeCode.UInt32:
                        Set(storageUInt32);
                        return true;
                    case TypeCode.Boolean:
                        Set(storageUInt32 != 0u);
                        return true;
                    case TypeCode.Single:
                        Set((Single)storageUInt32);
                        return true;
                    case TypeCode.Double:
                        Set((Double)storageUInt32);
                        return true;
                    case TypeCode.Int64:
                        Set((Int64)storageUInt32);
                        return true;
                    case TypeCode.UInt64:
                        Set((UInt64)storageUInt32);
                        return true;
                    case TypeCode.Decimal:
                        Set((Decimal)storageUInt32);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.Char:
                        Set((Char)storageUInt32);
                        return true;
                    case TypeCode.String:
                        Set(storageUInt32.ToString(GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        Set(storageUInt32);
                        return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Set(Double storageDouble, TypeCode valueTypeCode, bool stringIsLocalized)
        {
            try
            {
                switch (valueTypeCode)
                {
                    case TypeCode.SByte:
                        Set((SByte)storageDouble);
                        return true;
                    case TypeCode.Byte:
                        Set((Byte)storageDouble);
                        return true;
                    case TypeCode.Int16:
                        Set((Int16)storageDouble);
                        return true;
                    case TypeCode.UInt16:
                        Set((UInt16)storageDouble);
                        return true;
                    case TypeCode.Int32:
                        Set((Int32)storageDouble);
                        return true;
                    case TypeCode.UInt32:
                        Set((UInt32)storageDouble);
                        return true;
                    case TypeCode.Boolean:
                        Set(storageDouble != 0.0d);
                        return true;
                    case TypeCode.Single:
                        Set((Single)storageDouble);
                        return true;
                    case TypeCode.Double:
                        Set(storageDouble);
                        return true;
                    case TypeCode.Int64:
                        Set((Int64)storageDouble);
                        return true;
                    case TypeCode.UInt64:
                        Set((UInt64)storageDouble);
                        return true;
                    case TypeCode.Decimal:
                        Set((Decimal)storageDouble);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.Char:
                        Set((Char)storageDouble);
                        return true;
                    case TypeCode.String:
                        Set(storageDouble.ToString(GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        Set(storageDouble);
                        return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// </summary>
        public void Set(Any value)
        {
            _typeCode = value._typeCode;
            switch (value.ValueStorageType)
            {
                case StorageType.Object:
                    _storageUInt32 = 0;
                    _storageDouble = 0.0d;
                    _storageObject = value._storageObject;
                    return;
                case StorageType.Double:
                    _storageUInt32 = 0;
                    _storageDouble = value._storageDouble;
                    _storageObject = null;
                    return;
                case StorageType.UInt32:
                    _storageUInt32 = value._storageUInt32;
                    _storageDouble = 0.0d;
                    _storageObject = null;
                    return;
            }

            throw new InvalidOperationException();
        }

        public void Set(Object value)
        {
            if (ReferenceEquals(value, null))
            {
                _typeCode = TypeCode.Empty;
                _storageUInt32 = 0;
                _storageDouble = 0.0d;
                _storageObject = null;
                return;
            }

            if (value is Any)
            {
                Set((Any)value);
                return;
            }

            Type valueType = value.GetType();

            if (valueType.IsEnum)
            {
                _typeCode = TypeCode.Object;
                _storageUInt32 = 0;
                _storageDouble = 0.0d;
                _storageObject = value;
                return;
            }

            TypeCode typeCode = Type.GetTypeCode(valueType);
            switch (typeCode)
            {
                case TypeCode.SByte:
                    Set((SByte)value);
                    return;
                case TypeCode.Byte:
                    Set((Byte)value);
                    return;
                case TypeCode.Int16:
                    Set((Int16)value);
                    return;
                case TypeCode.UInt16:
                    Set((UInt16)value);
                    return;
                case TypeCode.Int32:
                    Set((Int32)value);
                    return;
                case TypeCode.UInt32:
                    Set((UInt32)value);
                    return;
                case TypeCode.Boolean:
                    Set((Boolean)value);
                    return;
                case TypeCode.Single:
                    Set((Single)value);
                    return;
                case TypeCode.Double:
                    Set((Double)value);
                    return;
                case TypeCode.Empty:
                    Set((object)null);
                    return;
                case TypeCode.DBNull:
                    Set((DBNull)value);
                    return;
                case TypeCode.Int64:
                    Set((Int64)value);
                    return;
                case TypeCode.UInt64:
                    Set((UInt64)value);
                    return;
                case TypeCode.Decimal:
                    Set((Decimal)value);
                    return;
                case TypeCode.DateTime:
                    Set((DateTime)value);
                    return;
                case TypeCode.Char:
                    Set((Char)value);
                    return;
                case TypeCode.String:
                    Set((String)value);
                    return;
                case TypeCode.Object:
                    _typeCode = TypeCode.Object;
                    _storageUInt32 = 0;
                    _storageDouble = 0.0d;
                    _storageObject = value;
                    return;
            }
        }

        public void Set(SByte value)
        {
            _typeCode = TypeCode.SByte;
            _storageUInt32 = (UInt32)value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        public void Set(Byte value)
        {
            _typeCode = TypeCode.Byte;
            _storageUInt32 = value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        public void Set(Int16 value)
        {
            _typeCode = TypeCode.Int16;
            _storageUInt32 = (UInt32)value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        public void Set(UInt16 value)
        {
            _typeCode = TypeCode.UInt16;
            _storageUInt32 = value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        public void Set(Int32 value)
        {
            _typeCode = TypeCode.Int32;
            _storageUInt32 = (UInt32)value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        public void Set(UInt32 value)
        {
            _typeCode = TypeCode.UInt32;
            _storageUInt32 = value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        public void Set(Boolean value)
        {
            _typeCode = TypeCode.Boolean;
            _storageUInt32 = value ? 1u : 0u;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        public void Set(Single value)
        {
            _typeCode = TypeCode.Single;
            _storageUInt32 = 0u;
            _storageDouble = value;
            _storageObject = null;
        }

        public void Set(Double value)
        {
            _typeCode = TypeCode.Double;
            _storageUInt32 = 0u;
            _storageDouble = value;
            _storageObject = null;
        }

        public void Set(Decimal value)
        {
            _typeCode = TypeCode.Decimal;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        public void Set(Int64 value)
        {
            _typeCode = TypeCode.Int64;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        public void Set(UInt64 value)
        {
            _typeCode = TypeCode.UInt64;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        public void Set(DateTime value)
        {
            _typeCode = TypeCode.DateTime;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        public void Set(DBNull value)
        {
            _typeCode = TypeCode.DBNull;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        public void Set(Char value)
        {
            _typeCode = TypeCode.UInt32;
            _storageUInt32 = (UInt32)value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        public void Set(String value)
        {
            if (ReferenceEquals(value, null))
            {
                value = String.Empty;
            }

            _typeCode = TypeCode.String;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        public Object ValueAsObject()
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return (SByte)_storageUInt32;
                    case TypeCode.Byte:
                        return (Byte)_storageUInt32;
                    case TypeCode.Int16:
                        return (Int16)_storageUInt32;
                    case TypeCode.UInt16:
                        return (UInt16)_storageUInt32;
                    case TypeCode.Int32:
                        return (Int32)_storageUInt32;
                    case TypeCode.UInt32:
                        return _storageUInt32;
                    case TypeCode.Boolean:
                        return _storageUInt32 != 0u;
                    case TypeCode.Char:
                        return (Char)_storageUInt32;
                    case TypeCode.Single:
                        return (Single)_storageDouble;
                    case TypeCode.Double:
                        return _storageDouble;
                    case TypeCode.Empty:
                        return null;
                    case TypeCode.DBNull:
                        return _storageObject;
                    case TypeCode.Int64:
                        return _storageObject;
                    case TypeCode.UInt64:
                        return _storageObject;
                    case TypeCode.Decimal:
                        return _storageObject;
                    case TypeCode.DateTime:
                        return _storageObject;
                    case TypeCode.String:
                        return _storageObject;
                    case TypeCode.Object:
                        return _storageObject;
                }
            }
            catch (Exception)
            {
                return null;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Result != null
        /// </summary>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public String ValueAsString(bool stringIsLocalized, string stringFormat = null)
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return ((SByte)_storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Byte:
                        return ((Byte)_storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Int16:
                        return ((Int16)_storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.UInt16:
                        return ((UInt16)_storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Int32:
                        return ((Int32)_storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.UInt32:
                        return _storageUInt32.ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Boolean:
                        return (_storageUInt32 != 0u).ToString(GetCultureInfo(stringIsLocalized));
                    case TypeCode.Char:
                        return ((Char)_storageUInt32).ToString();
                    case TypeCode.Single:
                        return ((Single)_storageDouble).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Double:
                        return _storageDouble.ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Empty:
                        return String.Empty;
                    case TypeCode.DBNull:
                        return ((DBNull)_storageObject).ToString(GetCultureInfo(stringIsLocalized));
                    case TypeCode.Int64:
                        return ((Int64)_storageObject).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.UInt64:
                        return ((UInt64)_storageObject).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Decimal:
                        return ((Decimal)_storageObject).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.DateTime:
                        return ((DateTime)_storageObject).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.String:
                        return (String)_storageObject;
                    case TypeCode.Object:
                        return ConvertToString(_storageObject, stringIsLocalized);
                }
            }
            catch (Exception)
            {
                return String.Empty;
            }

            throw new InvalidOperationException();
        }

        public Int32 ValueAsInt32(bool stringIsLocalized)
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return (SByte)_storageUInt32;
                    case TypeCode.Byte:
                        return (Byte)_storageUInt32;
                    case TypeCode.Int16:
                        return (Int16)_storageUInt32;
                    case TypeCode.UInt16:
                        return (UInt16)_storageUInt32;
                    case TypeCode.Int32:
                        return (Int32)_storageUInt32;
                    case TypeCode.UInt32:
                        return (Int32)_storageUInt32;
                    case TypeCode.Boolean:
                        return _storageUInt32 != 0u ? 1 : 0;
                    case TypeCode.Char:
                        return (Int32)_storageUInt32;
                    case TypeCode.Single:
                        return (Int32)(Single)_storageDouble;
                    case TypeCode.Double:
                        return (Int32)_storageDouble;
                    case TypeCode.Empty:
                        return 0;
                    case TypeCode.DBNull:
                        return 0;
                    case TypeCode.Int64:
                        return (Int32)(Int64)_storageObject;
                    case TypeCode.UInt64:
                        return (Int32)(UInt64)_storageObject;
                    case TypeCode.Decimal:
                        return (Int32)(Decimal)_storageObject;
                    case TypeCode.DateTime:
                        return 0;
                    case TypeCode.String:
                        Int32 result;
                        Int32.TryParse((string)_storageObject, NumberStyles.Integer,
                            GetCultureInfo(stringIsLocalized), out result);
                        return result;
                    case TypeCode.Object:
                        return ConvertTo<int>(_storageObject, stringIsLocalized);
                }
            }
            catch (Exception)
            {
                return 0;
            }

            throw new InvalidOperationException();
        }

        public Double ValueAsDouble(bool stringIsLocalized)
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return (SByte)_storageUInt32;
                    case TypeCode.Byte:
                        return (Byte)_storageUInt32;
                    case TypeCode.Int16:
                        return (Int16)_storageUInt32;
                    case TypeCode.UInt16:
                        return (UInt16)_storageUInt32;
                    case TypeCode.Int32:
                        return (Int32)_storageUInt32;
                    case TypeCode.UInt32:
                        return _storageUInt32;
                    case TypeCode.Boolean:
                        return _storageUInt32 != 0u ? 1d : 0d;
                    case TypeCode.Char:
                        return _storageUInt32;
                    case TypeCode.Single:
                        return (Single)_storageDouble;
                    case TypeCode.Double:
                        return _storageDouble;
                    case TypeCode.Empty:
                        return 0.0d;
                    case TypeCode.DBNull:
                        return 0.0d;
                    case TypeCode.Int64:
                        return (Int64)_storageObject;
                    case TypeCode.UInt64:
                        return (UInt64)_storageObject;
                    case TypeCode.Decimal:
                        return (Double)(Decimal)_storageObject;
                    case TypeCode.DateTime:
                        return 0.0d;
                    case TypeCode.String:
                        return ConvertToDouble((string)_storageObject, stringIsLocalized);
                    case TypeCode.Object:
                        return ConvertTo<double>(_storageObject, stringIsLocalized);
                }
            }
            catch (Exception)
            {
                return 0.0d;
            }

            throw new InvalidOperationException();
        }

        public bool ValueAsBoolean(bool stringIsLocalized)
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return _storageUInt32 != 0;
                    case TypeCode.Byte:
                        return _storageUInt32 != 0;
                    case TypeCode.Int16:
                        return _storageUInt32 != 0;
                    case TypeCode.UInt16:
                        return _storageUInt32 != 0;
                    case TypeCode.Int32:
                        return _storageUInt32 != 0;
                    case TypeCode.UInt32:
                        return _storageUInt32 != 0;
                    case TypeCode.Boolean:
                        return _storageUInt32 != 0;
                    case TypeCode.Char:
                        return _storageUInt32 != 0;
                    case TypeCode.Single:
                        return _storageDouble != 0.0 && !Double.IsNaN(_storageDouble);
                    case TypeCode.Double:
                        return _storageDouble != 0.0 && !Double.IsNaN(_storageDouble);
                    case TypeCode.Empty:
                        return false;
                    case TypeCode.DBNull:
                        return false;
                    case TypeCode.Int64:
                        return (Int64)_storageObject != 0;
                    case TypeCode.UInt64:
                        return (UInt64)_storageObject != 0;
                    case TypeCode.Decimal:
                        return (Decimal)_storageObject != (decimal)0.0;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        return ConvertToBoolean((string)_storageObject, stringIsLocalized);
                    case TypeCode.Object:
                        return ConvertTo<bool>(_storageObject, stringIsLocalized);
                }
            }
            catch (Exception)
            {
                return false;
            }

            throw new InvalidOperationException();
        }

        public T ValueAs<T>(bool stringIsLocalized)
        {
            return (T)ConvertTo(ValueAsObject(), typeof(T), stringIsLocalized);
        }

        public static bool IsNumber(string sValue, bool stringIsLocalized)
        {
            var any = ConvertToBestType(sValue, stringIsLocalized);
            return any.ValueType == typeof (double) || any.ValueType == typeof (int);
        }

        public static Any ConvertToBestType(string sValue, bool stringIsLocalized)
        {
            if (String.IsNullOrWhiteSpace(sValue)) return new Any(sValue);
            switch (sValue.ToUpperInvariant())
            {
                case "TRUE":
                    return new Any(true);
                case "FALSE":
                    return new Any(false);                
            }
            Double dValue;
            bool parsedAsDouble = Double.TryParse(sValue, NumberStyles.Any,
                GetCultureInfo(stringIsLocalized), out dValue);
            if (!parsedAsDouble) return new Any(sValue);            
            bool isInt32 = Math.Abs(dValue % 1) <= Double.Epsilon * 100;
            if (isInt32)
            {
                isInt32 = Int32.MinValue <= dValue && dValue <= Int32.MaxValue;
                if (isInt32) return new Any((int)dValue);
            }
            return new Any(dValue);            
        }

        public static T ConvertTo<T>(object value, bool stringIsLocalized, string stringFormat = null)
        {
            try
            {
                return (T)ConvertTo(value, typeof(T), stringIsLocalized, stringFormat);
            }
            catch (Exception)
            {
                return default (T);
            }            
        }

        /// <summary>
        ///     if conversion fails, returns default instance for value types or null for reference types.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="toType"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public static object ConvertTo(object value, Type toType, bool stringIsLocalized, string stringFormat = null)
        {
            if (toType == null || toType == typeof(object)) return value;

            var any = new Any(value);
            if (Convert(ref any, any, toType, stringIsLocalized, stringFormat))
            {
                try
                {
                    return any.ValueAsObject();
                }
                catch (Exception)
                {
                }
            }

            if (toType.IsValueType)
            {
                return Activator.CreateInstance(toType);
            }

            return null;
        }

        #endregion

        #region private functions

        /// <summary>
        ///     result != null
        /// </summary>
        /// <param name="value"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        private static string ConvertToString(object value, bool stringIsLocalized)
        {
            if (value == null) return String.Empty;

            Type type = value.GetType();
            if (type == typeof(object)) return String.Empty;
            ValueSerializer valueSerializer =
                            ValueSerializer.GetSerializerFor(type);
            if (valueSerializer != null && valueSerializer.CanConvertToString(value, null))
            {
                try
                {
                    string result = valueSerializer.ConvertToString(value, null);
                    if (result != null) return result;
                }
                catch (Exception)
                {
                }
            }

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter != null && converter.CanConvertTo(typeof(string)))
            {
                try
                {
                    string result = converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), value, typeof(string)) as string;
                    if (result != null) return result;
                }
                catch (Exception)
                {
                }
            }

            return value.ToString();
        }

        /// <summary>
        ///     Returns false, if String.IsNullOrWhiteSpace(value) || value.ToUpperInvariant() == "FALSE" || value == "0",
        ///     otherwise true.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        private static bool ConvertToBoolean(string value, bool stringIsLocalized)
        {
            if (String.IsNullOrWhiteSpace(value) || value.ToUpperInvariant() == "FALSE" || value == "0")
                return false;
            return true;
        }

        /// <summary>
        ///     Returns Double 0.0 if String.IsNullOrWhiteSpace(value) or value is not correct number.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        private static double ConvertToDouble(string value, bool stringIsLocalized)
        {
            double result;
            if (String.IsNullOrWhiteSpace(value) || !Double.TryParse(value, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out result))
            {
                result = 0.0d;
            }
            return result;
        }

        /// <summary>
        ///     Returns true, if succeeded.
        ///     if conversion fails, destination doesn't change.
        ///     toType != null, toType != typeof(object)
        /// </summary>
        /// <param name="destination"> </param>
        /// <param name="source"> </param>
        /// <param name="toType"> </param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns> true if succeded, false otherwise </returns>
        private static bool Convert(ref Any destination, Any source, Type toType, bool stringIsLocalized,
            string stringFormat = null)
        {
            if (toType == source.ValueType)
            {
                destination.Set(source);
                return true;
            }

            if (toType.IsEnum)
            {
                if (source.ValueStorageType != StorageType.Object)
                {
                    try
                    {
                        destination.Set(Enum.ToObject(toType, source.ValueAsInt32(stringIsLocalized)));
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    try
                    {
                        destination.Set(Enum.Parse(toType, source.ValueAsString(stringIsLocalized), true));
                    }
                    catch (Exception)
                    {
                        try
                        {
                            destination.Set(Enum.ToObject(toType, source.ValueAsInt32(stringIsLocalized)));
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                        return true;
                    }
                    return true;
                }
            }

            if (source.ValueTypeCode == TypeCode.Object)
            {
                if (toType == typeof(string))
                {
                    destination.Set(ConvertToString(source._storageObject, stringIsLocalized));
                    return true;
                }

                TypeConverter converter = TypeDescriptor.GetConverter(source._storageObject.GetType());
                if (converter != null && converter.CanConvertTo(toType))
                {
                    try
                    {
                        destination.Set(converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), source._storageObject, toType));
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return true;
                }
            }

            TypeCode toTypeCode = Type.GetTypeCode(toType);
            if (toTypeCode == TypeCode.Object)
            {
                if (source.ValueTypeCode == TypeCode.Empty && toType.IsClass)
                {
                    destination.Set((object)null);
                    return true;
                }
                if (source.ValueTypeCode == TypeCode.String && (string)source.StorageObject == @"")
                {
                    try
                    {
                        destination.Set(Activator.CreateInstance(toType));
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return true;
                }

                TypeConverter converter = TypeDescriptor.GetConverter(toType);
                if (converter != null && converter.CanConvertFrom(source.ValueType))
                {
                    try
                    {
                        destination.Set(converter.ConvertFrom(null, GetCultureInfo(stringIsLocalized), source.ValueAsObject()));
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return true;
                }
            }

            return Convert(ref destination, source, toTypeCode, stringIsLocalized, stringFormat);
        }

        /// <summary>
        ///     Returns true, if succeeded.
        ///     if conversion fails, destination doesn't change.
        ///     toTypeCode != TypeCode.Empty
        /// </summary>
        /// <param name="destination"> </param>
        /// <param name="source"> </param>
        /// <param name="toTypeCode"> </param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns> true if succeded, false otherwise </returns>
        private static bool Convert(ref Any destination, Any source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat = null)
        {
            if (toTypeCode == TypeCode.Object || source._typeCode == toTypeCode)
            {
                destination.Set(source);
                return true;
            }

            if (toTypeCode == TypeCode.DBNull)
            {
                destination.Set(DBNull.Value);
                return true;
            }

            switch (source.ValueTypeCode)
            {
                case TypeCode.SByte:
                    return Convert(ref destination, (SByte)source._storageUInt32, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.Byte:
                    return Convert(ref destination, (Byte)source._storageUInt32, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.Int16:
                    return Convert(ref destination, (Int16)source._storageUInt32, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.UInt16:
                    return Convert(ref destination, (UInt16)source._storageUInt32, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.Int32:
                    return Convert(ref destination, (Int32)source._storageUInt32, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.UInt32:
                    return Convert(ref destination, source._storageUInt32, toTypeCode, stringIsLocalized, stringFormat);
                case TypeCode.Boolean:
                    return Convert(ref destination, source._storageUInt32 != 0u, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.Char:
                    return Convert(ref destination, (Char)source._storageUInt32, toTypeCode, stringIsLocalized, stringFormat);
                case TypeCode.Single:
                    return Convert(ref destination, (Single)source._storageDouble, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.Double:
                    return Convert(ref destination, source._storageDouble, toTypeCode, stringIsLocalized, stringFormat);
                case TypeCode.Empty:
                    return Convert(ref destination, (object)null, toTypeCode, stringIsLocalized, stringFormat);
                case TypeCode.DBNull:
                    return Convert(ref destination, source._storageObject, toTypeCode, stringIsLocalized, stringFormat);
                case TypeCode.Int64:
                    return Convert(ref destination, (Int64)source._storageObject, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.UInt64:
                    return Convert(ref destination, (UInt64)source._storageObject, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.Decimal:
                    return Convert(ref destination, (Decimal)source._storageObject, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.String:
                    return Convert(ref destination, (String)source._storageObject, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.DateTime:
                    return Convert(ref destination, (DateTime)(source._storageObject), toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.Object:
                    return Convert(ref destination, source._storageObject, toTypeCode, stringIsLocalized, stringFormat);
            }

            throw new InvalidOperationException();
        }

        private static bool Convert(ref Any destination, SByte source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set(source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Byte source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set(source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Int16 source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set(source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, UInt16 source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set(source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Int32 source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set(source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, UInt32 source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set(source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Char source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32 ) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set(source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString());
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Int64 source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set(source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, UInt64 source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set(source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Boolean source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) (source ? 1 : 0));
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) (source ? 1 : 0));
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) (source ? 1 : 0));
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) (source ? 1 : 0));
                        return true;
                    case TypeCode.Int32:
                        destination.Set((source ? 1 : 0));
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) (source ? 1 : 0));
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) (source ? 1 : 0));
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) (source ? 1 : 0));
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source);
                        return true;
                    case TypeCode.Char:
                        destination.Set(source ? 'Y' : 'N');
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.Single:
                        destination.Set((Single) (source ? 1 : 0));
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) (source ? 1 : 0));
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) (source ? 1 : 0));
                        return true;
                    case TypeCode.String:
                        destination.Set(source.ToString(GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, DateTime source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.DateTime:
                        destination.Set(source);
                        return true;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Single source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0.0 && !Single.IsNaN(source));
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.Single:
                        destination.Set(source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Double source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != 0.0 && !Double.IsNaN(source));
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set(source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) source);
                        return true;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool Convert(ref Any destination, Decimal source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(source != (decimal) 0.0);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) source);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.Single:
                        destination.Set((Single) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set(source);
                        return true;
                    case TypeCode.String:
                        destination.Set(source.ToString(stringFormat, GetCultureInfo(stringIsLocalized)));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     toTypeCode != TypeCode.Empty AND toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, object source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            if (ReferenceEquals(source, null) || source == DBNull.Value)
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) 0);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) 0);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) 0);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) 0);
                        return true;
                    case TypeCode.Int32:
                        destination.Set(0);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) 0);
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(false);
                        return true;
                    case TypeCode.Char:
                        destination.Set((Char) 0);
                        return true;
                    case TypeCode.Single:
                        destination.Set(0.0f);
                        return true;
                    case TypeCode.Double:
                        destination.Set(0.0d);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) 0);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) 0);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) 0);
                        return true;
                    case TypeCode.DateTime:
                        destination.Set(DateTime.MinValue);
                        return true;
                    case TypeCode.String:
                        destination.Set(String.Empty);
                        return true;
                    case TypeCode.DBNull:
                        destination.Set(DBNull.Value);
                        return true;
                    default:
                        return false;
                }
            }

            if (source.GetType().IsEnum)
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                        destination.Set((SByte) source);
                        return true;
                    case TypeCode.Byte:
                        destination.Set((Byte) source);
                        return true;
                    case TypeCode.Int16:
                        destination.Set((Int16) source);
                        return true;
                    case TypeCode.UInt16:
                        destination.Set((UInt16) source);
                        return true;
                    case TypeCode.Int32:
                        destination.Set((Int32) source);
                        return true;
                    case TypeCode.UInt32:
                        destination.Set((UInt32) source);
                        return true;
                    case TypeCode.Single:
                        destination.Set((Single) (Int32) source);
                        return true;
                    case TypeCode.Double:
                        destination.Set((Double) (Int32) source);
                        return true;
                    case TypeCode.Int64:
                        destination.Set((Int64) source);
                        return true;
                    case TypeCode.UInt64:
                        destination.Set((UInt64) source);
                        return true;
                    case TypeCode.Decimal:
                        destination.Set((Decimal) (Int32) source);
                        return true;
                    case TypeCode.String:
                        destination.Set(source.ToString());
                        return true;
                    default:
                        return false;
                }
            }            

            switch (toTypeCode)
            {
                case TypeCode.String:
                    destination.Set(ConvertToString(source, stringIsLocalized));
                    return true;
                default:
                    return false;
            }
        }

        private static bool Convert(ref Any destination, String source, TypeCode toTypeCode, bool stringIsLocalized,
            string stringFormat)
        {
            try
            {
                switch (toTypeCode)
                {
                    case TypeCode.SByte:
                    {
                        SByte value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !SByte.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.Byte:
                    {
                        Byte value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !Byte.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.Int16:
                    {
                        Int16 value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !Int16.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.UInt16:
                    {
                        UInt16 value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !UInt16.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.Int32:
                    {
                        Int32 value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !Int32.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.UInt32:
                    {
                        UInt32 value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !UInt32.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.Boolean:
                        destination.Set(ConvertToBoolean(source, stringIsLocalized));
                        return true;
                    case TypeCode.Single:
                    {
                        Single value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !Single.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0.0f;
                        }                        
                        destination.Set(value);
                    }                        
                        return true;
                    case TypeCode.Double:
                        destination.Set(ConvertToDouble(source, stringIsLocalized));
                        return true;
                    case TypeCode.Int64:
                    {
                        Int64 value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !Int64.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.UInt64:
                    {
                        UInt64 value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !UInt64.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.Decimal:
                    {
                        Decimal value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !Decimal.TryParse(source, NumberStyles.Any, GetCultureInfo(stringIsLocalized), out value))
                        {
                            value = 0;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.DateTime:
                    {
                        DateTime value;
                        if (String.IsNullOrWhiteSpace(source) ||
                            !DateTime.TryParse(source, GetCultureInfo(stringIsLocalized), DateTimeStyles.None, out value))
                        {
                            value = DateTime.MinValue;
                        }
                        destination.Set(value);
                    }
                        return true;
                    case TypeCode.String:
                        destination.Set(source);
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }        

        private static CultureInfo GetCultureInfo(bool localized)
        {
            if (localized) return CultureHelper.SystemCultureInfo;
            return CultureInfo.InvariantCulture;
        }

        #endregion

        #region private fields

        private Double _storageDouble;
        private Object _storageObject;
        private UInt32 _storageUInt32;
        private TypeCode _typeCode;

        #endregion
    }
}