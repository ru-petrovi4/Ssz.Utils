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
	/// This class is a subclass of DataValueArrays and adds the aliases 
	/// for the inherited values, statuses, timestamps.  Three arrays of 
	/// aliases are defined by this class, one associated with double values, 
	/// one associated with long values, and one associated with object values.  
	/// The index used for given alias is the same as that used for its value, 
	/// status, and timestamp in the appropriate array set (double, long, object). 
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class DataValueArraysWithAlias : DataValueArrays
	{
		/// <summary>
		/// When used in a read context (returned from the server) 
		/// this is the Client Alias.  When used in a write context 
		/// (sent to the server) this is the Server Alias.
		/// </summary>
		[DataMember] public uint[]? DoubleAlias;

		/// <summary>
		/// When used in a read context (returned from the server) 
		/// this is the Client Alias.  When used in a write context 
		/// (sent to the server) this is the Server Alias.
		/// </summary>
		[DataMember] public uint[]? UintAlias;

		/// <summary>
		/// When used in a read context (returned from the server) 
		/// this is the Client Alias.  When used in a write context 
		/// (sent to the server) this is the Server Alias.
		/// </summary>
		[DataMember] public uint[]? ObjectAlias;

		/// <summary>
		/// This constructor creates a DataValueArraysWithAlias object 
		/// with empty arrays of the specified sizes.
		/// </summary>
		/// <param name="doubleArraySize">
		/// The number of elements in the array containing double values.
		/// </param>
		/// <param name="uintArraySize">
		/// The number of elements in the array containing uint values.
		/// </param>
		/// <param name="objectArraySize">
		/// The number of elements in the array containing object values.
		/// </param>
		public DataValueArraysWithAlias(int doubleArraySize, int uintArraySize, int objectArraySize)
			: base(doubleArraySize, uintArraySize, objectArraySize)
		{
			DoubleAlias = (0 != doubleArraySize) ? new uint[doubleArraySize] : null;
			UintAlias = (0 != uintArraySize) ? new uint[uintArraySize] : null;
			ObjectAlias = (0 != objectArraySize) ? new uint[objectArraySize] : null;
		}

		/// <summary>
		/// This constructor creates a DataValueArraysWithAlias object 
		/// from a set of arrays.
		/// </summary>
		/// <param name="doubleAliases">
		/// The array of aliases for double values.
		/// </param>
		/// <param name="doubleStatusCodes">
		/// The array of status codes for double values.
		/// </param>
		/// <param name="doubleTimestamps">
		/// The array of timestamps for double values.
		/// </param>
		/// <param name="doubleValues">
		/// The array of double values.
		/// </param>
		/// <param name="uintClientAliases">
		/// The array of aliases for uint values.
		/// </param>
		/// <param name="uintStatusCodes">
		/// The array of status codes for uint values.
		/// </param>
		/// <param name="uintTimestamps">
		/// The array of timestamps for uint values.
		/// </param>
		/// <param name="uintValues">
		/// The array of uint values.
		/// </param>
		/// <param name="objectClientAliases">
		/// The array of aliases for object values.
		/// </param>
		/// <param name="objectStatusCodes">
		/// The array of status codes for object values.
		/// </param>
		/// <param name="objectTimestamps">
		/// The array of timestamps for object values.
		/// </param>
		/// <param name="objectValues">
		/// The array of object values.
		/// </param>
		public DataValueArraysWithAlias(
			ref uint[] doubleAliases,       ref uint[] doubleStatusCodes, ref DateTime[] doubleTimestamps, ref double[] doubleValues,
			ref uint[] uintClientAliases,   ref uint[] uintStatusCodes,   ref DateTime[] uintTimestamps,   ref uint[] uintValues,
			ref uint[] objectClientAliases, ref uint[] objectStatusCodes, ref DateTime[] objectTimestamps, ref object[] objectValues)
			: base(ref doubleStatusCodes, ref doubleTimestamps, ref doubleValues,
				ref uintStatusCodes, ref uintTimestamps, ref uintValues,
				ref objectStatusCodes, ref objectTimestamps, ref objectValues)
		{
			DoubleAlias = doubleAliases;
			UintAlias = uintClientAliases;
			ObjectAlias = objectClientAliases;
		}

		/// <summary>
		/// This method sets the arrays of this object to null.
		/// </summary>
		public new void Clear()
		{
			base.Clear();
			DoubleAlias = null;
			UintAlias = null;
			ObjectAlias = null;
		}

		/// <summary>
		/// This method sets a double value and its associated alias, status, 
		/// timestamp in the appropriate arrays.
		/// </summary>
		/// <param name="idx">
		/// The index of the array entries to be updated.
		/// </param>
		/// <param name="clientAlias">
		/// The client alias of the value.
		/// </param>
		/// <param name="statusCode">
		/// The status of the value.
		/// </param>
		/// <param name="timestamp">
		/// The timestamp of the value.
		/// </param>
		/// <param name="value">
		/// The double value.
		/// </param>
		public void SetDouble(int idx, uint clientAlias, 
			uint statusCode, DateTime timestamp, double value)
		{
			if (DoubleAlias is null) throw new InvalidOperationException();
			DoubleAlias[idx] = clientAlias;
			base.SetDouble(idx, statusCode, timestamp, value);
		}

		/// <summary>
		/// This method sets a long value and its associated alias, status, 
		/// timestamp in the appropriate arrays.
		/// </summary>
		/// <param name="idx">
		/// The index of the array entries to be updated.
		/// </param>
		/// <param name="clientAlias">
		/// The client alias of the value.
		/// </param>
		/// <param name="statusCode">
		/// The status of the value.
		/// </param>
		/// <param name="timestamp">
		/// The timestamp of the value.
		/// </param>
		/// <param name="value">
		/// The long value.
		/// </param>
		public void SetUint(int idx, uint clientAlias, 
			uint statusCode, DateTime timestamp, uint value)
		{
			if (UintAlias is null) throw new InvalidOperationException();
			UintAlias[idx] = clientAlias;
			base.SetUint(idx, statusCode, timestamp, value);
		}

		/// <summary>
		/// This method sets an object value and its associated alias, status, 
		/// timestamp in the appropriate arrays.
		/// </summary>
		/// <param name="idx">
		/// The index of the array entries to be updated.
		/// </param>
		/// <param name="clientAlias">
		/// The client alias of the value.
		/// </param>
		/// <param name="statusCode">
		/// The status of the value.
		/// </param>
		/// <param name="timestamp">
		/// The timestamp of the value.
		/// </param>
		/// <param name="value">
		/// The object value.
		/// </param>
		public void SetObject(int idx, uint clientAlias, uint statusCode, DateTime timestamp, 
			object? value)
		{
			if (ObjectAlias is null) throw new InvalidOperationException();
			ObjectAlias[idx] = clientAlias;
			base.SetObject(idx, statusCode, timestamp, value);
		}

		/// <summary>
		/// This method sets the arrays used to convey double values.
		/// </summary>
		/// <param name="clientAliasArray">
		/// The client alias array.
		/// </param>
		/// <param name="statusCodeArray">
		/// The status code array.
		/// </param>
		/// <param name="timestampArray">
		/// The timestamp array.
		/// </param>
		/// <param name="valueArray">
		/// The value array.
		/// </param>
		/// <returns>
		/// True if the array could be set.
		/// </returns>
		public bool CreateDoubleArraysWithAlias(uint[]? clientAliasArray, uint[]? statusCodeArray,
			DateTime[]? timestampArray, double[]? valueArray)
		{
			if (   (clientAliasArray is null)
				|| (statusCodeArray is null)
				|| (timestampArray is null)
				|| (valueArray is null)
			   )
			{
				DoubleAlias = null;
				return base.CreateDoubleArrays(null, null, null);
			}
			else if (   (clientAliasArray.Length == statusCodeArray.Length)
					 && (clientAliasArray.Length == timestampArray.Length)
					 && (clientAliasArray.Length == valueArray.Length)
			   )
			{
				DoubleAlias = clientAliasArray;
				ObjectAlias = clientAliasArray;
				return base.CreateDoubleArrays(statusCodeArray, timestampArray, valueArray);
			}
			return false;
		}

		/// <summary>
		/// This method sets the arrays used to convey integer values.
		/// </summary>
		/// <param name="clientAliasArray">
		/// The client alias array.
		/// </param>
		/// <param name="statusCodeArray">
		/// The status code array.
		/// </param>
		/// <param name="timestampArray">
		/// The timestamp array.
		/// </param>
		/// <param name="valueArray">
		/// The value array.
		/// </param>
		/// <returns>
		/// True if the array could be set.
		/// </returns>
		public bool CreateUintArraysWithAlias(uint[] clientAliasArray, uint[] statusCodeArray,
			DateTime[] timestampArray, uint[] valueArray)
		{
			if (   (clientAliasArray is null)
				|| (statusCodeArray is null)
				|| (timestampArray is null)
				|| (valueArray is null)
			   )
			{
				UintAlias = null;
				return base.CreateUintArrays(null, null, null);
			}
			else if (   (clientAliasArray.Length == statusCodeArray.Length)
					 && (clientAliasArray.Length == timestampArray.Length)
					 && (clientAliasArray.Length == valueArray.Length)
			   )
			{
				UintAlias = clientAliasArray;
				ObjectAlias = clientAliasArray;
				return base.CreateUintArrays(statusCodeArray, timestampArray, valueArray);
			}
			return false;
		}

		/// <summary>
		/// This method sets the arrays used to convey object values.
		/// </summary>
		/// <param name="clientAliasArray">
		/// The client alias array.
		/// </param>
		/// <param name="statusCodeArray">
		/// The status code array.
		/// </param>
		/// <param name="timestampArray">
		/// The timestamp array.
		/// </param>
		/// <param name="valueArray">
		/// The value array.
		/// </param>
		/// <returns>
		/// True if the array could be set.
		/// </returns>
		public bool CreateObjectArraysWithAlias(uint[] clientAliasArray, uint[] statusCodeArray,
			DateTime[] timestampArray, object[] valueArray)
		{
			if (   (clientAliasArray is null)
				|| (statusCodeArray is null)
				|| (timestampArray is null)
				|| (valueArray is null)
			   )
			{
				ObjectAlias = null;
				return base.CreateObjectArrays(null, null, null);
			}
			else if (   (clientAliasArray.Length == statusCodeArray.Length)
					 && (clientAliasArray.Length == timestampArray.Length)
					 && (clientAliasArray.Length == valueArray.Length)
			   )
			{
				ObjectAlias = clientAliasArray;
				return base.CreateObjectArrays(statusCodeArray, timestampArray, valueArray);
			}
			return false;
		}
	}
}