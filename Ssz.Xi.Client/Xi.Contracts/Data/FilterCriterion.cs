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
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class represents a single filter criterion in terms of an expression, 
	/// in which the operand is compared against a value using a comparison operator.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	[DebuggerDisplay("{Filter}")]
	[KnownType(typeof(EventType))]
	[KnownType(typeof(AlarmState))]
	[KnownType(typeof(DateTime))]
	[KnownType(typeof(InstanceId))]
    [KnownType(typeof(DBNull))]
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
	public class FilterCriterion
	{
		/// <summary>
		/// The name of the operand. Standard operand names are defined by the 
		/// FilterOperand class.  Non-standard operands and the data types for 
		/// their values are defined in the Standard MIB by the Event Message Fields 
		/// and History Properties supported by the server.
		/// </summary>
		[DataMember] public string? OperandName { get; set; }

		/// <summary>
		/// The name of the operator. Standard operators are defined by the 
		/// FilterOperator enumeration.  Operator values between 0 and 
		/// UInt16.MaxValue are reserved.
		/// </summary>
		[DataMember] public uint Operator { get; set; }

		/// <summary>
		/// The comparison value.
		/// </summary>
		[DataMember] public object? ComparisonValue { get; set; }

		/// <summary>
		/// This method compares this FilterCriterion against the filterToCompare 
		/// to determine if they are identical. Identical FilterCriterion are are those 
		/// with the same operand, operator, and comparison value.
		/// </summary>
		/// <param name="filterToCompare">
		/// The FilterCriterion to compare against this FilterCriterion.
		/// </param>
		/// <returns>
		/// Returns TRUE if the FilterCriterion are identical. Otherwise returns FALSE.
		/// </returns>
		public bool CompareIdentical(FilterCriterion filterToCompare)
		{
			if (filterToCompare == null)
				return false;
			if ((this.OperandName == null) && (filterToCompare.OperandName != null))
				return false;
			if ((this.OperandName != null) && (filterToCompare.OperandName == null))
				return false;
			if ((this.ComparisonValue == null) && (filterToCompare.ComparisonValue != null))
				return false;
			if ((this.ComparisonValue != null) && (filterToCompare.ComparisonValue == null))
				return false;

			// now check to see if the members are the same. If not, return false.
			if ((this.OperandName != filterToCompare.OperandName)
				|| (this.Operator != filterToCompare.Operator)
				|| (this.ComparisonValue != filterToCompare.ComparisonValue)
			   )
			{
				return false;
			}
			return true;
		}

	}
}