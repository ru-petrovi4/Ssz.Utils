/**********************************************************************
 * Copyright © 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
 *
 * The source code and all binaries built with the OPC .NET 3.0 source
 * code are subject to the terms of the Express Interface Public
 * License (Xi-PL).  See http://www.opcfoundation.org/License/Xi-PL/
 *
 * The source code may be distributed from an OPC member company in
 * its original or modified form to its customers and to any others who
 * have software that needs to interoperate with the OPC member's OPC
* .NET 3.0 products. No other redistribution is permitted.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *********************************************************************/

using System;
using System.Text;

namespace Xi.OPC.COM.API
{
	/// <summary>
	/// This enum mimics the Microsoft VARENUM
	/// at least with the values needed by the
	/// OPC Wrapper / Adapter.
	/// </summary>
	[Flags]
	public enum cliVARENUM : ushort
	{
		VT_EMPTY = 0,
		VT_NULL = 1,
		VT_I2 = 2,
		VT_I4 = 3,
		VT_R4 = 4,
		VT_R8 = 5,
		VT_CY = 6,
		VT_DATE = 7,
		VT_BSTR = 8,
		VT_DISPATCH = 9,
		VT_ERROR = 10,
		VT_BOOL = 11,
		VT_VARIANT = 12,
		VT_UNKNOWN = 13,
		VT_DECIMAL = 14,
		VT_I1 = 16,
		VT_UI1 = 17,
		VT_UI2 = 18,
		VT_UI4 = 19,
		VT_I8 = 20,
		VT_UI8 = 21,
		VT_INT = 22,
		VT_UINT = 23,
		VT_VOID = 24,
		VT_HRESULT = 25,
		VT_PTR = 26,
		VT_SAFEARRAY = 27,
		VT_CARRAY = 28,
		VT_USERDEFINED = 29,
		VT_LPSTR = 30,
		VT_LPWSTR = 31,
		VT_RECORD = 36,
		VT_INT_PTR = 37,
		VT_UINT_PTR = 38,
		VT_FILETIME = 64,
		VT_BLOB = 65,
		VT_STREAM = 66,
		VT_STORAGE = 67,
		VT_STREAMED_OBJECT = 68,
		VT_STORED_OBJECT = 69,
		VT_BLOB_OBJECT = 70,
		VT_CF = 71,
		VT_CLSID = 72,
		VT_VERSIONED_STREAM = 73,
		VT_BSTR_BLOB = 0xfff,

		VT_VECTOR = 0x1000,
		VT_ARRAY = 0x2000,
		VT_BYREF = 0x4000,
		VT_RESERVED = 0x8000,
		VT_ILLEGAL = 0xffff,
		VT_ILLEGALMASKED = 0xfff,
		VT_TYPEMASK = 0xfff
	}

	public static class cliDataTypeString
	{
		public static string Boolean = typeof(Boolean).ToString();
		public static string Int16 = typeof(Int16).ToString();
		public static string Int32 = typeof(Int32).ToString();
		public static string Int64 = typeof(Int64).ToString();
		public static string UInt16 = typeof(UInt16).ToString();
		public static string UInt32 = typeof(UInt32).ToString();
		public static string UInt64 = typeof(UInt64).ToString();
		public static string Float = typeof(float).ToString();
		public static string Double = typeof(double).ToString();
		public static string DateTime = typeof(DateTime).ToString();
		public static string TimeSpan = typeof(TimeSpan).ToString();
		public static string String = typeof(String).ToString();
		public static string SByte = typeof(sbyte).ToString();
		public static string Byte = typeof(byte).ToString();
		public static string Bytestring = typeof(byte[]).ToString();
	}

	/// <summary>
	/// The cliVARIANT mimics the Microsoft COM VARIANT type.
	/// Providing a .NET / CLI friendly version of the COM VARIANT.
	/// In many ways it provides functionality similar to the 
	/// _variant_t ATL COM class.  Providing overloaded operator 
	/// for assignment both to and from native .NET / CLI basic 
	/// data ypes programmer friendly.
	/// </summary>
	public struct cliVARIANT : IDisposable
	{
		private object vDataValue;

		/// <summary>
		/// Returns the .NET / CLI data type being represented.
		/// </summary>
		public Type GetDataType()
		{
			return vDataValue.GetType();
		}

		/// <summary>
		/// Returns the actual data value as a .NET / CLI Object.
		/// </summary>
		public object DataValue
		{
			get { return vDataValue; }
		}

		/// <summary>
		/// Constructor to create a cloned instance.
		/// </summary>
		/// <param name="v">cliVARIANT to clone</param>
		public cliVARIANT(cliVARIANT v)
		{
			vDataValue = v.MemberwiseClone();
		}

		/// <summary>
		/// Construct a cliVARIANT given a .NET / CLI object;
		/// <code>object objVar = new object()</code>
		/// <code>cliVARIANT cliVar = new cliVARIANT(objVar)</code>
		/// </summary>
		/// <param name="v">The .NET / CLI object to construct from.</param>
		public cliVARIANT(object v) { vDataValue = v; }

		/// <summary>
		/// Construct a cliVARIANT given a .NET / CLI bool;
		/// <code>bool bVar = true;</code>
		/// <code>cliVARIANT cliVar = new cliVARIANT(bVar)</code>
		/// </summary>
		/// <param name="v">The .NET / CLI bool to construct from.</param>
		public cliVARIANT(bool v) { vDataValue = v; }

		/// <summary>
		/// Construct a cliVARIANT given a .NET / CLI byte;
		/// <code>byte bVar = true;</code>
		/// <code>cliVARIANT cliVar = new cliVARIANT(bVar)</code>
		/// </summary>
		/// <param name="v">The .NET / CLI byte to construct from.</param>
		public cliVARIANT(byte v) { vDataValue = v; }

		/// <summary>
		/// Construct a cliVARIANT given a .NET / CLI sbyte;
		/// <code>sbyte sbVar = 64;</code>
		/// <code>cliVARIANT cliVar = new cliVARIANT(sbVar)</code>
		/// </summary>
		/// <param name="v">The .NET / CLI short to construct from.</param>
		public cliVARIANT(sbyte v) { vDataValue = v; }

		public cliVARIANT(short v) { vDataValue = v; }
		public cliVARIANT(ushort v) { vDataValue = v; }
		public cliVARIANT(int v) { vDataValue = v; }
		public cliVARIANT(uint v) { vDataValue = v; }
		public cliVARIANT(long v) { vDataValue = v; }
		public cliVARIANT(ulong v) { vDataValue = v; }
		public cliVARIANT(char v) { vDataValue = v; }
		public cliVARIANT(double v) { vDataValue = v; }
		public cliVARIANT(float v) { vDataValue = v; }
		public cliVARIANT(string v) { vDataValue = v; }

		/// <summary>
		/// Construct a cliVARIANT given a .NET / CLI DateTime;
		/// </summary>
		/// <param name="v">The .NET / CLI DateTime to construct from.</param>
		public cliVARIANT(DateTime v) { vDataValue = v; }
		public cliVARIANT(TimeSpan v) { vDataValue = v; }

		/// <summary>
		/// This explicit operator allows the assignment of a 
		/// cliVARIANT to a .NET / CLI variable of type bool.
		/// <code>cliVariant cliVar = new cliVARIANT(true);</code>
		/// <code>bool bVal = (bool)cliVar;</code>
		/// </summary>
		/// <param name="v">The cliVARIANT to be typecast;</param>
		/// <returns></returns>
		public static explicit operator bool(cliVARIANT v)
		{
			if (typeof(bool) == v.GetDataType()) return (bool)v.vDataValue;
			throw new InvalidCastException(v.ExceptionString("bool"));
		}

		/// <summary>
		/// This explicit operator allows the assignment of a 
		/// cliVARIANT to a .NET / CLI variable of type byte.
		/// <code>cliVariant cliVar = new cliVARIANT((byte)32);</code>
		/// <code>byte bVal = (byte)cliVar;</code>
		/// </summary>
		/// <param name="v">The cliVARIANT to be typecast;</param>
		/// <returns></returns>
		public static explicit operator byte(cliVARIANT v)
		{
			if (typeof(byte) == v.GetDataType()) return (byte)v.vDataValue;
			if (typeof(bool) == v.GetDataType()) return (byte)(((bool)v.DataValue) ? 1 : 0);
			throw new InvalidCastException(v.ExceptionString("byte"));
		}

		/// <summary>
		/// This explicit operator allows the assignment of a 
		/// cliVARIANT to a .NET / CLI variable of type sbyte.
		/// <code>cliVariant cliVar = new cliVARIANT((byte)32);</code>
		/// <code>byte sbVal = (sbyte)cliVar;</code>
		/// </summary>
		/// <param name="v">The cliVARIANT to be typecast;</param>
		/// <returns></returns>
		public static explicit operator sbyte(cliVARIANT v)
		{
			if (typeof(sbyte) == v.GetDataType()) return (sbyte)v.vDataValue;
			if (typeof(bool) == v.GetDataType()) return (sbyte)(((bool)v.DataValue) ? 1 : 0);
			throw new InvalidCastException(v.ExceptionString("sbyte"));
		}

		public static explicit operator short(cliVARIANT v)
		{
			Type vt = v.GetDataType();
			if (typeof(short) == vt) return (short)v.vDataValue;
			if (typeof(byte) == vt) return (short)(byte)v.vDataValue;
			if (typeof(sbyte) == vt) return (short)(sbyte)v.vDataValue;
			if (typeof(char) == vt) return (short)(char)v.DataValue;
			if (typeof(bool) == vt) return (short)(((bool)v.DataValue) ? 1 : 0);
			throw new InvalidCastException(v.ExceptionString("short"));
		}
		public static explicit operator ushort(cliVARIANT v)
		{
			Type vt = v.GetDataType();
			if (typeof(ushort) == vt) return (ushort)v.vDataValue;
			if (typeof(byte) == vt) return (ushort)(byte)v.vDataValue;
			if (typeof(char) == vt) return (ushort)(char)v.DataValue;
			if (typeof(bool) == vt) return (ushort)(((bool)v.DataValue) ? 1 : 0);
			throw new InvalidCastException(v.ExceptionString("ushort"));
		}
		public static explicit operator int(cliVARIANT v)
		{
			Type vt = v.GetDataType();
			if (typeof(int) == vt) return (int)v.vDataValue;
			if (typeof(byte) == vt) return (int)(byte)v.vDataValue;
			if (typeof(sbyte) == vt) return (int)(sbyte)v.vDataValue;
			if (typeof(short) == vt) return (int)(short)v.vDataValue;
			if (typeof(ushort) == vt) return (int)(ushort)v.vDataValue;
			if (typeof(char) == vt) return (int)(char)v.DataValue;
			if (typeof(bool) == vt) return (int)(((bool)v.DataValue) ? 1 : 0);
			throw new InvalidCastException(v.ExceptionString("int"));
		}
		public static explicit operator uint(cliVARIANT v)
		{
			Type vt = v.GetDataType();
			if (typeof(int) == vt) return (uint)(int)v.DataValue;
			if (typeof(uint) == vt) return (uint)v.vDataValue;
			if (typeof(byte) == vt) return (uint)(byte)v.vDataValue;
			if (typeof(sbyte) == vt) return (uint)(sbyte)v.DataValue;
			if (typeof(short) == vt) return (uint)(short)v.DataValue;
			if (typeof(ushort) == vt) return (uint)(ushort)v.vDataValue;
			if (typeof(char) == vt) return (uint)(char)v.DataValue;
			if (typeof(bool) == vt) return (uint)(((bool)v.DataValue) ? 1 : 0);
			throw new InvalidCastException(v.ExceptionString("uint"));
		}
		public static explicit operator long(cliVARIANT v)
		{
			Type vt = v.GetDataType();
			if (typeof(long) == vt) return (long)v.vDataValue;
			if (typeof(byte) == vt) return (long)(byte)v.vDataValue;
			if (typeof(sbyte) == vt) return (long)(sbyte)v.vDataValue;
			if (typeof(short) == vt) return (long)(short)v.vDataValue;
			if (typeof(ushort) == vt) return (long)(ushort)v.vDataValue;
			if (typeof(int) == vt) return (long)(int)v.vDataValue;
			if (typeof(uint) == vt) return (long)(uint)v.vDataValue;
			if (typeof(char) == vt) return (long)(char)v.DataValue;
			if (typeof(bool) == vt) return (long)(((bool)v.DataValue) ? 1 : 0);
			throw new InvalidCastException(v.ExceptionString("long"));
		}
		public static explicit operator ulong(cliVARIANT v)
		{
			Type vt = v.GetDataType();
			if (typeof(ulong) == vt) return (ulong)v.vDataValue;
			if (typeof(byte) == vt) return (ulong)(byte)v.vDataValue;
			if (typeof(ushort) == vt) return (ulong)(ushort)v.vDataValue;
			if (typeof(uint) == vt) return (ulong)(uint)v.vDataValue;
			if (typeof(char) == vt) return (ulong)(char)v.DataValue;
			if (typeof(bool) == vt) return (ulong)(((bool)v.DataValue) ? 1 : 0);
			throw new InvalidCastException(v.ExceptionString("ulong"));
		}
		public static explicit operator float(cliVARIANT v)
		{
			Type vt = v.GetDataType();
			if (typeof(float) == vt) return (float)v.vDataValue;
			if (typeof(byte) == vt) return (float)(byte)v.vDataValue;
			if (typeof(sbyte) == vt) return (float)(sbyte)v.vDataValue;
			if (typeof(short) == vt) return (float)(short)v.vDataValue;
			if (typeof(ushort) == vt) return (float)(ushort)v.vDataValue;
			if (typeof(char) == vt) return (float)(char)v.DataValue;
			throw new InvalidCastException(v.ExceptionString("float"));
		}
		public static explicit operator double(cliVARIANT v)
		{
			Type vt = v.GetDataType();
			if (typeof(double) == vt) return (double)v.vDataValue;
			if (typeof(float) == vt) return (double)(float)v.vDataValue;
			if (typeof(byte) == vt) return (double)(byte)v.vDataValue;
			if (typeof(sbyte) == vt) return (double)(sbyte)v.vDataValue;
			if (typeof(short) == vt) return (double)(short)v.vDataValue;
			if (typeof(ushort) == vt) return (double)(ushort)v.vDataValue;
			if (typeof(int) == vt) return (double)(int)v.vDataValue;
			if (typeof(uint) == vt) return (double)(uint)v.vDataValue;
			if (typeof(char) == vt) return (double)(char)v.DataValue;
			throw new InvalidCastException(v.ExceptionString("double"));
		}
		public static explicit operator string(cliVARIANT v)
		{
			if (typeof(string) == v.GetDataType()) return (string)v.vDataValue;
			throw new InvalidCastException(v.ExceptionString("string"));
		}

		/// <summary>
		/// This explicit operator allows the assignment of a 
		/// cliVARIANT to a .NET / CLI variable of type DateTime.
		/// </summary>
		/// <param name="v">The cliVARIANT to be typecast;</param>
		/// <returns></returns>
		public static explicit operator DateTime(cliVARIANT v)
		{
			if (typeof(DateTime) == v.GetDataType()) return (DateTime)v.vDataValue;
			throw new InvalidCastException(v.ExceptionString("DateTime"));
		}
		public static explicit operator TimeSpan(cliVARIANT v)
		{
			if (typeof(TimeSpan) == v.GetDataType()) return (TimeSpan)v.vDataValue;
			throw new InvalidCastException(v.ExceptionString("TimeSpan"));
		}

		/// <summary>
		/// These operators provide for simple assignemnts to a cliVARIANT;
		/// </summary>
		/// <param name="v">Value to be assigned.</param>
		/// <returns></returns>
		public static implicit operator cliVARIANT(bool v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(byte v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(sbyte v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(short v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(ushort v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(int v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(uint v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(long v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(ulong v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(char v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(double v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(float v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(string v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(DateTime v) { return new cliVARIANT(v); }
		public static implicit operator cliVARIANT(TimeSpan v) { return new cliVARIANT(v); }

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(vDataValue.GetType().ToString());
			sb.Append(" Value = ");
			sb.Append(vDataValue.ToString());
			return sb.ToString();
		}

		public void Dispose()
		{
			vDataValue = null;
		}

		public static Type CliTypeFrom(cliVARIANT varval)
		{
			return CliTypeFrom((cliVARENUM)(short)varval);
		}

		public static Type CliTypeFrom(ushort varEnum)
		{
			return CliTypeFrom((cliVARENUM)varEnum);
		}

		public static Type CliTypeFrom(cliVARENUM varenum)
		{
			switch (varenum)
			{
				case cliVARENUM.VT_I2: return typeof(short);
				case cliVARENUM.VT_I4: return typeof(int);
				case cliVARENUM.VT_R4: return typeof(float);
				case cliVARENUM.VT_R8: return typeof(double);
				case cliVARENUM.VT_CY: return typeof(object);		// Used for both DateTime and TimeSpan2
				case cliVARENUM.VT_DATE: return typeof(DateTime);
				case cliVARENUM.VT_BSTR: return typeof(string);
				case cliVARENUM.VT_BOOL: return typeof(bool);
				case cliVARENUM.VT_I1: return typeof(sbyte);
				case cliVARENUM.VT_UI1: return typeof(byte);
				case cliVARENUM.VT_UI2: return typeof(ushort);
				case cliVARENUM.VT_UI4: return typeof(uint);
				case cliVARENUM.VT_I8: return typeof(long);
				case cliVARENUM.VT_UI8: return typeof(ulong);
				case cliVARENUM.VT_INT: return typeof(int);
				case cliVARENUM.VT_UINT: return typeof(uint);
				case cliVARENUM.VT_HRESULT: return typeof(int);
				case cliVARENUM.VT_FILETIME: return typeof(long);
				case cliVARENUM.VT_BLOB: return typeof(byte[]);

				default: return typeof(object);
			}
		}

		public static ushort UshortTypeFromCliString(string type)
		{
			if (type == cliDataTypeString.Boolean) return (ushort)cliVARENUM.VT_BOOL;
			if (type == cliDataTypeString.Int16) return (ushort)cliVARENUM.VT_I2;
			if (type == cliDataTypeString.Int32) return (ushort)cliVARENUM.VT_I4;
			if (type == cliDataTypeString.Int64) return (ushort)cliVARENUM.VT_I8;
			if (type == cliDataTypeString.UInt16) return (ushort)cliVARENUM.VT_UI2;
			if (type == cliDataTypeString.UInt32) return (ushort)cliVARENUM.VT_UI4;
			if (type == cliDataTypeString.UInt64) return (ushort)cliVARENUM.VT_UI8;
			if (type == cliDataTypeString.Float) return (ushort)cliVARENUM.VT_R4;
			if (type == cliDataTypeString.Double) return (ushort)cliVARENUM.VT_R8;
			if (type == cliDataTypeString.String) return (ushort)cliVARENUM.VT_BSTR;
			if (type == cliDataTypeString.DateTime) return (ushort)cliVARENUM.VT_CY;
			if (type == cliDataTypeString.DateTime) return (ushort)cliVARENUM.VT_DATE;
			if (type == cliDataTypeString.SByte) return (ushort)cliVARENUM.VT_I1;
			if (type == cliDataTypeString.Byte) return (ushort)cliVARENUM.VT_UI1;
			if (type == cliDataTypeString.Bytestring) return (ushort)cliVARENUM.VT_BLOB;
			if (type == cliDataTypeString.TimeSpan) return (ushort)cliVARENUM.VT_CY;
			return 0;
		}

		private string ExceptionString(string toStr)
		{
			StringBuilder sb = new StringBuilder("Invalid Variant Conversion, from ");
			sb.Append(GetDataType().ToString());
			sb.Append(" to ");
			sb.Append(toStr);
			return sb.ToString();
		}
	}

}
