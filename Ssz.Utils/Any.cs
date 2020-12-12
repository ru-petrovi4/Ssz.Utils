using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Markup;

namespace Ssz.Utils
{
    /// <summary>
    ///     If func param stringIsLocalized = false, InvariantCulture is used.
    ///     If func param stringIsLocalized = true, CultureHelper.SystemCultureInfo is used, which is corresponds operating system culture (see CultureHelper class).
    /// </summary>
    public struct Any
    {
        #region StorageType enum
        
        /// <summary>        
        /// </summary>
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public Any(object? value)
        {
            _typeCode = TypeCode.Empty;
            _storageUInt32 = 0;
            _storageDouble = 0.0d;
            _storageObject = null;

            if (value != null) SetInternal(value);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="that"></param>
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

        /// <summary>
        /// 
        /// </summary>
        public uint StorageUInt32
        {
            get { return _storageUInt32; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double StorageDouble
        {
            get { return _storageDouble; }
        }

        /// <summary>
        /// 
        /// </summary>
        public object? StorageObject
        {
            get { return _storageObject; }
        }

        /// <summary>
        /// 
        /// </summary>
        public TypeCode ValueTypeCode
        {
            get { return _typeCode; }
        }

        /// <summary>        
        /// </summary>
        public Type ValueType
        {
            get
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return typeof (SByte);
                    case TypeCode.Byte:
                        return typeof (Byte);
                    case TypeCode.Int16:
                        return typeof (Int16);
                    case TypeCode.UInt16:
                        return typeof (UInt16);
                    case TypeCode.Int32:
                        return typeof (Int32);
                    case TypeCode.UInt32:
                        return typeof (UInt32);
                    case TypeCode.Boolean:
                        return typeof (Boolean);
                    case TypeCode.Single:
                        return typeof (Single);
                    case TypeCode.Double:
                        return typeof (Double);
                    case TypeCode.Empty:
                        return typeof (Object);
                    case TypeCode.DBNull:
                        return typeof (DBNull);
                    case TypeCode.Int64:
                        return typeof (Int64);
                    case TypeCode.UInt64:
                        return typeof (UInt64);
                    case TypeCode.Decimal:
                        return typeof (Decimal);
                    case TypeCode.DateTime:
                        return typeof (DateTime);
                    case TypeCode.String:
                        return typeof (String);
                    case TypeCode.Char:
                        return typeof (Char);
                    case TypeCode.Object:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return _storageObject.GetType();
                }
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
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
        /// </summary>
        /// <param name="sValue"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        public static Any ConvertToBestType(string? sValue, bool stringIsLocalized)
        {
            if (sValue == null) return new Any(null);
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

        /// <summary>        
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
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

        /// <summary>        
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(Any left, Any right)
        {
            return !(left == right);
        }

        /// <summary>
        ///     Uses ValueAsDouble(false), ValueAsInt32(false), ValueAsString(false) depending of ValueStorageType.
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
        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Any)) return false;

            return this == (Any) obj;
        }

        /// <summary>        
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return 0;
        }

        /// <summary>        
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {            
            return ValueAsString(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageUInt32"></param>
        /// <param name="valueTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        public bool Set(UInt32 storageUInt32, TypeCode valueTypeCode, bool stringIsLocalized)
        {
            try
            {
                switch (valueTypeCode)
                {
                    case TypeCode.SByte:
                        Set((SByte) storageUInt32);
                        return true;
                    case TypeCode.Byte:
                        Set((Byte) storageUInt32);
                        return true;
                    case TypeCode.Int16:
                        Set((Int16) storageUInt32);
                        return true;
                    case TypeCode.UInt16:
                        Set((UInt16) storageUInt32);
                        return true;
                    case TypeCode.Int32:
                        Set((Int32) storageUInt32);
                        return true;
                    case TypeCode.UInt32:
                        Set(storageUInt32);
                        return true;
                    case TypeCode.Boolean:
                        Set(storageUInt32 != 0u);
                        return true;
                    case TypeCode.Single:
                        Set((Single) storageUInt32);
                        return true;
                    case TypeCode.Double:
                        Set((Double) storageUInt32);
                        return true;                    
                    case TypeCode.Int64:
                        Set((Int64) storageUInt32);
                        return true;
                    case TypeCode.UInt64:
                        Set((UInt64) storageUInt32);
                        return true;
                    case TypeCode.Decimal:
                        Set((Decimal) storageUInt32);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.Char:
                        Set((Char) storageUInt32);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageDouble"></param>
        /// <param name="valueTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        public bool Set(Double storageDouble, TypeCode valueTypeCode, bool stringIsLocalized)
        {
            try
            {
                switch (valueTypeCode)
                {
                    case TypeCode.SByte:
                        Set((SByte) storageDouble);
                        return true;
                    case TypeCode.Byte:
                        Set((Byte) storageDouble);
                        return true;
                    case TypeCode.Int16:
                        Set((Int16) storageDouble);
                        return true;
                    case TypeCode.UInt16:
                        Set((UInt16) storageDouble);
                        return true;
                    case TypeCode.Int32:
                        Set((Int32) storageDouble);
                        return true;
                    case TypeCode.UInt32:
                        Set((UInt32) storageDouble);
                        return true;
                    case TypeCode.Boolean:
                        Set(storageDouble != 0.0d);
                        return true;
                    case TypeCode.Single:
                        Set((Single) storageDouble);
                        return true;
                    case TypeCode.Double:
                        Set(storageDouble);
                        return true;                    
                    case TypeCode.Int64:
                        Set((Int64) storageDouble);
                        return true;
                    case TypeCode.UInt64:
                        Set((UInt64) storageDouble);
                        return true;
                    case TypeCode.Decimal:
                        Set((Decimal) storageDouble);
                        return true;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.Char:
                        Set((Char) storageDouble);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(object? value)
        {
            if (value == null)
            {
                SetNull();                
            }
            else
            {
                SetInternal(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(SByte value)
        {
            _typeCode = TypeCode.SByte;
            _storageUInt32 = (UInt32) value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Byte value)
        {
            _typeCode = TypeCode.Byte;
            _storageUInt32 = value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Int16 value)
        {
            _typeCode = TypeCode.Int16;
            _storageUInt32 = (UInt32) value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(UInt16 value)
        {
            _typeCode = TypeCode.UInt16;
            _storageUInt32 = value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Int32 value)
        {
            _typeCode = TypeCode.Int32;
            _storageUInt32 = (UInt32) value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(UInt32 value)
        {
            _typeCode = TypeCode.UInt32;
            _storageUInt32 = value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Boolean value)
        {
            _typeCode = TypeCode.Boolean;
            _storageUInt32 = value ? 1u : 0u;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Single value)
        {
            _typeCode = TypeCode.Single;
            _storageUInt32 = 0u;
            _storageDouble = value;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Double value)
        {
            _typeCode = TypeCode.Double;
            _storageUInt32 = 0u;
            _storageDouble = value;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Decimal value)
        {
            _typeCode = TypeCode.Decimal;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Int64 value)
        {
            _typeCode = TypeCode.Int64;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(UInt64 value)
        {
            _typeCode = TypeCode.UInt64;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(DateTime value)
        {
            _typeCode = TypeCode.DateTime;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(DBNull value)
        {
            _typeCode = TypeCode.DBNull;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(Char value)
        {
            _typeCode = TypeCode.UInt32;
            _storageUInt32 = (UInt32) value;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Set(String value)
        {            
            _typeCode = TypeCode.String;
            _storageUInt32 = 0u;
            _storageDouble = 0.0d;
            _storageObject = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object? ValueAsObject()
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return (SByte) _storageUInt32;
                    case TypeCode.Byte:
                        return (Byte) _storageUInt32;
                    case TypeCode.Int16:
                        return (Int16) _storageUInt32;
                    case TypeCode.UInt16:
                        return (UInt16) _storageUInt32;
                    case TypeCode.Int32:
                        return (Int32) _storageUInt32;
                    case TypeCode.UInt32:
                        return _storageUInt32;
                    case TypeCode.Boolean:
                        return _storageUInt32 != 0u;
                    case TypeCode.Char:
                        return (Char) _storageUInt32;
                    case TypeCode.Single:
                        return (Single) _storageDouble;
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
        /// </summary>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public String ValueAsString(bool stringIsLocalized, string? stringFormat = null)
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return ((SByte) _storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Byte:
                        return ((Byte) _storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Int16:
                        return ((Int16) _storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.UInt16:
                        return ((UInt16) _storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Int32:
                        return ((Int32) _storageUInt32).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.UInt32:
                        return _storageUInt32.ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Boolean:
                        return (_storageUInt32 != 0u).ToString(GetCultureInfo(stringIsLocalized));
                    case TypeCode.Char:
                        return ((Char)_storageUInt32).ToString();
                    case TypeCode.Single:
                        return ((Single) _storageDouble).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Double:
                        return _storageDouble.ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Empty:
                        return String.Empty;
                    case TypeCode.DBNull:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return ((DBNull)_storageObject).ToString(GetCultureInfo(stringIsLocalized));
                    case TypeCode.Int64:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return ((Int64) _storageObject).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.UInt64:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return ((UInt64) _storageObject).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.Decimal:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return ((Decimal) _storageObject).ToString(stringFormat, GetCultureInfo(stringIsLocalized));
                    case TypeCode.DateTime:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return ((DateTime) _storageObject).ToString(stringFormat, GetCultureInfo(stringIsLocalized));                        
                    case TypeCode.String:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (String) _storageObject;                    
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        public Int32 ValueAsInt32(bool stringIsLocalized)
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return (SByte) _storageUInt32;
                    case TypeCode.Byte:
                        return (Byte) _storageUInt32;
                    case TypeCode.Int16:
                        return (Int16) _storageUInt32;
                    case TypeCode.UInt16:
                        return (UInt16) _storageUInt32;
                    case TypeCode.Int32:
                        return (Int32) _storageUInt32;
                    case TypeCode.UInt32:
                        return (Int32) _storageUInt32;
                    case TypeCode.Boolean:
                        return _storageUInt32 != 0u ? 1 : 0;
                    case TypeCode.Char:
                        return (Int32)_storageUInt32;
                    case TypeCode.Single:
                        return (Int32) (Single) _storageDouble;
                    case TypeCode.Double:
                        return (Int32) _storageDouble;
                    case TypeCode.Empty:
                        return 0;
                    case TypeCode.DBNull:
                        return 0;
                    case TypeCode.Int64:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (Int32) (Int64) _storageObject;
                    case TypeCode.UInt64:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (Int32) (UInt64) _storageObject;
                    case TypeCode.Decimal:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (Int32) (Decimal) _storageObject;
                    case TypeCode.DateTime:
                        return 0;
                    case TypeCode.String:
                        if (_storageObject == null) throw new InvalidOperationException();
                        Int32 result;
                        if (!Int32.TryParse((string) _storageObject, NumberStyles.Integer,
                            GetCultureInfo(stringIsLocalized), out result))
                        {
                            return 0;
                        }
                        return result;
                    case TypeCode.Object:
                        if (_storageObject == null) throw new InvalidOperationException();
                        object? obj = ObjectValueAs(_storageObject, typeof(int), stringIsLocalized);
                        if (obj == null) return 0;
                        else return (int)obj;
                }
            }
            catch (Exception)
            {
                return 0;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        public Double ValueAsDouble(bool stringIsLocalized)
        {
            try
            {
                switch (_typeCode)
                {
                    case TypeCode.SByte:
                        return (SByte) _storageUInt32;
                    case TypeCode.Byte:
                        return (Byte) _storageUInt32;
                    case TypeCode.Int16:
                        return (Int16) _storageUInt32;
                    case TypeCode.UInt16:
                        return (UInt16) _storageUInt32;
                    case TypeCode.Int32:
                        return (Int32) _storageUInt32;
                    case TypeCode.UInt32:
                        return _storageUInt32;
                    case TypeCode.Boolean:
                        return _storageUInt32 != 0u ? 1d : 0d;
                    case TypeCode.Char:
                        return _storageUInt32;
                    case TypeCode.Single:
                        return (Single) _storageDouble;
                    case TypeCode.Double:
                        return _storageDouble;
                    case TypeCode.Empty:
                        return 0.0d;
                    case TypeCode.DBNull:
                        return 0.0d;
                    case TypeCode.Int64:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (Int64) _storageObject;
                    case TypeCode.UInt64:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (UInt64) _storageObject;
                    case TypeCode.Decimal:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (Double) (Decimal) _storageObject;
                    case TypeCode.DateTime:
                        return 0.0d;
                    case TypeCode.String:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return ConvertToDouble((string)_storageObject, stringIsLocalized);
                    case TypeCode.Object:
                        if (_storageObject == null) throw new InvalidOperationException();
                        object? obj = ObjectValueAs(_storageObject, typeof(double), stringIsLocalized);
                        if (obj == null) return 0.0d;
                        else return (double)obj;
                }
            }
            catch (Exception)
            {
                return 0.0d;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
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
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (Int64) _storageObject != 0;
                    case TypeCode.UInt64:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (UInt64) _storageObject != 0;
                    case TypeCode.Decimal:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return (Decimal) _storageObject != (decimal) 0.0;
                    case TypeCode.DateTime:
                        return false;
                    case TypeCode.String:
                        if (_storageObject == null) throw new InvalidOperationException();
                        return ConvertToBoolean((string) _storageObject, stringIsLocalized);
                    case TypeCode.Object:
                        if (_storageObject == null) throw new InvalidOperationException();
                        object? obj = ObjectValueAs(_storageObject, typeof(bool), stringIsLocalized);
                        if (obj == null) return false;
                        else return (bool)obj;
                }
            }
            catch (Exception)
            {
                return false;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public T? ValueAs<T>(bool stringIsLocalized, string? stringFormat = null)
            where T : notnull
        {
            return (T?)ValueAs(typeof(T), stringIsLocalized, stringFormat);
        }

        /// <summary>
        ///     Returns requested type or null. 
        /// </summary>
        /// <param name="asType"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public object? ValueAs(Type? asType, bool stringIsLocalized, string? stringFormat = null)
        {
            if (asType == null || asType == typeof(object) || asType == ValueType)
            {
                return ValueAsObject();
            }

            if (asType.IsEnum)
            {
                return ValueAsIsEnum(asType, stringIsLocalized);                
            }

            if (_typeCode == TypeCode.Object)
            {
                if (_storageObject == null) throw new InvalidOperationException();
                return ObjectValueAs(_storageObject, asType, stringIsLocalized);                
            }

            TypeCode asTypeCode = Type.GetTypeCode(asType);
            if (asTypeCode == TypeCode.Object)
            {
                return ValueAsObject(asType, stringIsLocalized);                
            }

            if (asTypeCode == TypeCode.DBNull)
            {
                return DBNull.Value;
            }

            var destination = new Any();
            if (Convert(ref destination, this, asTypeCode, stringIsLocalized, stringFormat))
            {
                return destination.ValueAsObject();
            }
            else
            {
                try
                {
                    return Activator.CreateInstance(asType);
                }
                catch (Exception)
                {
                }
                return null;
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     storageObject.ValueTypeCode == TypeCode.Object
        /// </summary>
        /// <param name="storageObject"></param>
        /// <param name="asType"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        private static object? ObjectValueAs(object storageObject, Type asType, bool stringIsLocalized)
        {
            if (asType == typeof(string))
            {
                return ConvertToString(storageObject, stringIsLocalized);
            }
            
            TypeConverter converter = TypeDescriptor.GetConverter(storageObject.GetType());
            if (converter.CanConvertTo(asType))
            {
                try
                {
                    return converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), storageObject, asType);
                }
                catch (Exception)
                {
                }
            }

            if (storageObject.GetType().IsEnum)
            {
                switch (Type.GetTypeCode(asType))
                {
                    case TypeCode.SByte:
                        return (SByte)storageObject;                        
                    case TypeCode.Byte:
                        return (Byte)storageObject;                        
                    case TypeCode.Int16:
                        return (Int16)storageObject;                        
                    case TypeCode.UInt16:
                        return (UInt16)storageObject;                        
                    case TypeCode.Int32:
                        return (Int32)storageObject;                        
                    case TypeCode.UInt32:
                        return (UInt32)storageObject;                        
                    case TypeCode.Single:
                        return (Single)(Int32)storageObject;                        
                    case TypeCode.Double:
                        return (Double)(Int32)storageObject;                        
                    case TypeCode.Int64:
                        return (Int64)storageObject;                        
                    case TypeCode.UInt64:
                        return (UInt64)storageObject;                        
                    case TypeCode.Decimal:
                        return (Decimal)(Int32)storageObject;                                        
                }
            }

            try
            {
                return Activator.CreateInstance(asType);
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>        
        /// </summary>
        /// <param name="value"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        private static string ConvertToString(object? value, bool stringIsLocalized)
        {
            if (value == null) return String.Empty;

            Type type = value.GetType();
            if (type == typeof(object)) return String.Empty;

            // TODO:
            //System.Windows.Markup.ValueSerializer valueSerializer =
            //                ValueSerializer.GetSerializerFor(type);
            //if (valueSerializer != null && valueSerializer.CanConvertToString(value, null))
            //{
            //    try
            //    {
            //        string result = valueSerializer.ConvertToString(value, null);
            //        if (result != null) return result;
            //    }
            //    catch (Exception)
            //    {
            //    }
            //}

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertTo(typeof(string)))
            {
                try
                {
                    string? result = converter.ConvertTo(null, GetCultureInfo(stringIsLocalized), value, typeof(string)) as string;
                    if (result != null) return result;
                }
                catch (Exception)
                {
                }
            }

            return value.ToString() ?? @"";
        }

        /// <summary>
        ///     Returns false, if String.IsNullOrWhiteSpace(value) || value.ToUpperInvariant() == "FALSE" || value == "0",
        ///     otherwise true.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        private static bool ConvertToBoolean(string? value, bool stringIsLocalized)
        {
            Any any = ConvertToBestType(value, stringIsLocalized);
            if (any.ValueTypeCode == TypeCode.String) return false;
            return any.ValueAsBoolean(stringIsLocalized);
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
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object, source.ValueTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"> </param>
        /// <param name="source"> </param>
        /// <param name="toTypeCode"> </param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns> true if succeded, false otherwise </returns>
        private static bool Convert(ref Any destination, Any source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat = null)
        {            
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
                    return ConvertFromNullOrDBNull(ref destination, toTypeCode);
                case TypeCode.DBNull:
                    return ConvertFromNullOrDBNull(ref destination, toTypeCode);
                case TypeCode.Int64:
                    if (source._storageObject == null) throw new InvalidOperationException();
                    return Convert(ref destination, (Int64)source._storageObject, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.UInt64:
                    if (source._storageObject == null) throw new InvalidOperationException();
                    return Convert(ref destination, (UInt64)source._storageObject, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.Decimal:
                    if (source._storageObject == null) throw new InvalidOperationException();
                    return Convert(ref destination, (Decimal)source._storageObject, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.String:
                    if (source._storageObject == null) throw new InvalidOperationException();
                    return Convert(ref destination, (String)source._storageObject, toTypeCode, stringIsLocalized,
                        stringFormat);
                case TypeCode.DateTime:
                    if (source._storageObject == null) throw new InvalidOperationException();
                    return Convert(ref destination, (DateTime)(source._storageObject), toTypeCode, stringIsLocalized,
                        stringFormat);                
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, SByte source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Byte source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Int16 source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, UInt16 source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Int32 source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, UInt32 source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Char source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Int64 source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, UInt64 source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Boolean source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, DateTime source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Single source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Double source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, Decimal source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="toTypeCode"></param>
        /// <returns></returns>
        private static bool ConvertFromNullOrDBNull(ref Any destination, TypeCode toTypeCode)
        {
            switch (toTypeCode)
            {
                case TypeCode.SByte:
                    destination.Set((SByte)0);
                    return true;
                case TypeCode.Byte:
                    destination.Set((Byte)0);
                    return true;
                case TypeCode.Int16:
                    destination.Set((Int16)0);
                    return true;
                case TypeCode.UInt16:
                    destination.Set((UInt16)0);
                    return true;
                case TypeCode.Int32:
                    destination.Set(0);
                    return true;
                case TypeCode.UInt32:
                    destination.Set((UInt32)0);
                    return true;
                case TypeCode.Boolean:
                    destination.Set(false);
                    return true;
                case TypeCode.Char:
                    destination.Set((Char)0);
                    return true;
                case TypeCode.Single:
                    destination.Set(0.0f);
                    return true;
                case TypeCode.Double:
                    destination.Set(0.0d);
                    return true;
                case TypeCode.Int64:
                    destination.Set((Int64)0);
                    return true;
                case TypeCode.UInt64:
                    destination.Set((UInt64)0);
                    return true;
                case TypeCode.Decimal:
                    destination.Set((Decimal)0);
                    return true;
                case TypeCode.DateTime:
                    destination.Set(DateTime.MinValue);
                    return true;
                case TypeCode.String:
                    destination.Set(String.Empty);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     toTypeCode != TypeCode.Empty, toTypeCode != TypeCode.DBNull, toTypeCode != TypeCode.Object
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="toTypeCode"></param>
        /// <param name="stringIsLocalized"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        private static bool Convert(ref Any destination, String source, TypeCode toTypeCode, bool stringIsLocalized,
            string? stringFormat)
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
            if (localized) return ConfigurationHelper.SystemCultureInfo;
            return CultureInfo.InvariantCulture;
        }

        private void SetNull()
        {
            _typeCode = TypeCode.Empty;
            _storageUInt32 = 0;
            _storageDouble = 0.0d;
            _storageObject = null;
        }

        private void SetInternal(object value)
        {
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
                    SetNull();
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

        /// <summary>   
        ///     asType is Enum
        /// </summary>
        /// <param name="asType"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        private object? ValueAsIsEnum(Type asType, bool stringIsLocalized)
        {
            if (ValueStorageType != StorageType.Object)
            {
                try
                {
                    return Enum.ToObject(asType, ValueAsInt32(stringIsLocalized));
                }
                catch
                {
                }                
            }
            else
            {
                try
                {
                    return Enum.Parse(asType, ValueAsString(stringIsLocalized), true);
                }
                catch
                {
                    try
                    {
                        return Enum.ToObject(asType, ValueAsInt32(stringIsLocalized));
                    }
                    catch
                    {
                    }
                }                
            }
            return Activator.CreateInstance(asType);
        }

        /// <summary>
        ///     asType has TypeCode.Object, _typeCode != TypeCode.Object
        /// </summary>
        /// <param name="asType"></param>
        /// <param name="stringIsLocalized"></param>
        /// <returns></returns>
        private object? ValueAsObject(Type asType, bool stringIsLocalized)
        {
            if (_typeCode == TypeCode.Empty)
            {
                if (asType.IsClass)
                {
                    return null;
                }
                else
                {
                    try
                    {
                        return Activator.CreateInstance(asType);
                    }
                    catch (Exception)
                    {
                    }
                    return null;
                }                
            }

            if (_typeCode == TypeCode.String)
            {
                if (_storageObject == null) throw new InvalidOperationException();
                if ((string)_storageObject == @"")
                {
                    try
                    {
                        return Activator.CreateInstance(asType);
                    }
                    catch (Exception)
                    {                        
                    }
                    return null;
                }
            }

            TypeConverter converter = TypeDescriptor.GetConverter(asType);
            if (converter.CanConvertFrom(ValueType))
            {
                try
                {
                    return converter.ConvertFrom(null, GetCultureInfo(stringIsLocalized), ValueAsObject());
                }
                catch (Exception)
                {
                }                
            }

            try
            {
                return Activator.CreateInstance(asType);
            }
            catch (Exception)
            {
            }
            return null;
        }

        #endregion

        #region private fields

        private double _storageDouble;
        private object? _storageObject;
        private uint _storageUInt32;
        private TypeCode _typeCode;

        #endregion
    }
}