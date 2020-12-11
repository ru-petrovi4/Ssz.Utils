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
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class is used to transfer the server-assigned alias of 
	/// a data object and its value.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
    [KnownType(typeof(System.DBNull))]
    // one dimensional arrays of basic types
    [KnownType(typeof(float[]))]
	[KnownType(typeof(double[]))]
	[KnownType(typeof(byte[]))]
	[KnownType(typeof(sbyte[]))]
	[KnownType(typeof(short[]))]
	[KnownType(typeof(ushort[]))]
	[KnownType(typeof(int[]))]
	[KnownType(typeof(uint[]))]
	[KnownType(typeof(long[]))]
	[KnownType(typeof(ulong[]))]
	[KnownType(typeof(bool[]))]
	//[KnownType(typeof(System.Collections.Generic.List<bool>))]  //?????
	[KnownType(typeof(string[]))]
	[KnownType(typeof(object[]))]
	[KnownType(typeof(System.DateTime[]))]
	[KnownType(typeof(System.TimeSpan[]))]
	[KnownType(typeof(System.Decimal[]))]
	// two dimensional arrays of basic types
	[KnownType(typeof(float[][]))]
	[KnownType(typeof(double[][]))]
	[KnownType(typeof(byte[][]))]
	[KnownType(typeof(sbyte[][]))]
	[KnownType(typeof(short[][]))]
	[KnownType(typeof(ushort[][]))]
	[KnownType(typeof(int[][]))]
	[KnownType(typeof(uint[][]))]
	[KnownType(typeof(long[][]))]
	[KnownType(typeof(ulong[][]))]
	[KnownType(typeof(bool[][]))]
	[KnownType(typeof(string[][]))]
	[KnownType(typeof(object[][]))]
	[KnownType(typeof(System.DateTime[][]))]
	[KnownType(typeof(System.TimeSpan[][]))]
	[KnownType(typeof(System.Decimal[][]))]
	// Xi types
	[KnownType(typeof(TypeId))]
	[KnownType(typeof(ServerStatus))]
	[KnownType(typeof(StringTableEntry))]
	[KnownType(typeof(StringTableEntry[]))]
	public class WriteValueArrays
	{
		#region Data Members
		/// <summary>
		/// The array of server-assigned aliases of the double values to be written.
		/// The size and order of this array matches the size and order of the 
		/// DoubleValues array.
		/// </summary>
		[DataMember] public uint[]? DoubleServerAlias { get; private set; }

		/// <summary>
		/// The array of server-assigned aliases of the uint values to be written.
		/// The size and order of this array matches the size and order of the 
		/// UintValues array.
		/// </summary>
		[DataMember] public uint[]? UintServerAlias { get; private set; }

		/// <summary>
		/// The array of server-assigned aliases of the object values to be written.
		/// The size and order of this array matches the size and order of the 
		/// ObjectValues array.
		/// </summary>
		[DataMember] public uint[]? ObjectServerAlias { get; private set; }

		/// <summary>
		/// The array of double values to be written.
		/// </summary>
		[DataMember] public double[]? DoubleValues { get; private set; }
		
		/// <summary>
		/// The array of integer values to be written.
		/// Used to transfer byte, sbyte, short, ushort, int and uint values.
		/// </summary>
		[DataMember] public uint[]? UintValues { get; private set; }
		
		/// <summary>
		/// The array of object values to be written.
		/// </summary>
		[DataMember] public object[]? ObjectValues { get; private set; }
		#endregion // Data Members

		#region Constructors and Methods
		/// <summary>
		/// This constructor initializes a WriteValueList object with empty  
		/// lists of server aliases and object values.
		/// </summary>
		/// <param name="doubleArraySize">
		/// The size of the arrays associated with double values.
		/// </param>
		/// <param name="uintArraySize">
		/// The size of the arrays associated with long values.
		/// </param>
		/// <param name="objectArraySize">
		/// The size of the arrays associated with object values.
		/// </param>
		public WriteValueArrays(int doubleArraySize, int uintArraySize, int objectArraySize)
		{
			if (0 == doubleArraySize)
			{
				DoubleServerAlias = null;
				DoubleValues = null;
			}
			else
			{
				DoubleServerAlias = new uint[doubleArraySize];
				DoubleValues = new double[doubleArraySize];
			}
			if (0 == uintArraySize)
			{
				UintServerAlias = null;
				UintValues = null;
			}
			else
			{
				UintServerAlias = new uint[uintArraySize];
				UintValues = new uint[uintArraySize];
			}
			if (0 == objectArraySize)
			{
				ObjectServerAlias = null;
				ObjectValues = null;
			}
			else
			{
				ObjectServerAlias = new uint[objectArraySize];
				ObjectValues = new object[objectArraySize];
			}
		}

		/// <summary>
		/// This method adds a double value to a WriteValueList
		/// </summary>
		/// <param name="idx">
		/// The array index of the value to be added.  The same index 
		/// is used to add the server alias to associated server alias array.
		/// </param>
		/// <param name="serverAlias">
		/// The server-alias of the value.
		/// </param>
		/// <param name="value">
		/// The double value.
		/// </param>
		public void SetDouble(int idx, uint serverAlias, double value)
		{
			if (DoubleServerAlias == null || DoubleValues == null) throw new InvalidOperationException();
			DoubleServerAlias[idx] = serverAlias;
			DoubleValues[idx] = value;
		}

		/// <summary>
		/// This method adds a long value to a WriteValueList
		/// </summary>
		/// <param name="idx">
		/// The array index of the value to be added.  The same index 
		/// is used to add the server alias to associated server alias array.
		/// </param>
		/// <param name="serverAlias">
		/// The server-alias of the value.
		/// </param>
		/// <param name="value">
		/// The long value.
		/// </param>
		public void SetUint(int idx, uint serverAlias, uint value)
		{
			if (UintServerAlias == null || UintValues == null) throw new InvalidOperationException();
			UintServerAlias[idx] = serverAlias;
			UintValues[idx] = value;
		}

		/// <summary>
		/// This method adds an object value to a WriteValueList
		/// </summary>
		/// <param name="idx">
		/// The array index of the value to be added.  The same index 
		/// is used to add the server alias to associated server alias array.
		/// </param>
		/// <param name="serverAlias">
		/// The server-alias of the value.
		/// </param>
		/// <param name="value">
		/// The object value.
		/// </param>
		public void SetObject(int idx, uint serverAlias, object value)
		{
			if (ObjectServerAlias == null || ObjectValues == null) throw new InvalidOperationException();
			ObjectServerAlias[idx] = serverAlias;
			ObjectValues[idx] = value;
		}
		#endregion // Constructors and Methods
	}
}