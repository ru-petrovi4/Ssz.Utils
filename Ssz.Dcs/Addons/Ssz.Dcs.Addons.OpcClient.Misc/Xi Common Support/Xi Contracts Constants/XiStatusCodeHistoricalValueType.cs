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

namespace Xi.Contracts.Constants
{
	/// <summary>
	/// The HistoricalValueType is a 3-bit property that describes the 
	/// the historical data value associated with the Status Code.  
	/// </summary>
	public enum XiStatusCodeHistoricalValueType : uint
	{
		/// <summary>
		/// This value may be used to mask (keep) the bits 
		/// used to convey the historical data value type.
		/// </summary>
		HistoricalValueTypeMask = 0x00E00000,

		/// <summary>
		/// This value provides the number of bits to shift 
		/// the historical data value type bits into the 
		/// low bits or into the Xi Status Code bit position.
		/// </summary>
		HistoricalValueTypeShiftCount = 21,

		/// <summary>
		/// The historical value type is not used. 
		/// </summary>
		NotUsed                = 0,

		/// <summary>
		/// The value is the raw value.  If the value is a raw value with more 
		/// than one raw value at the same timestamp, then the ExtraValue 
		/// enumeration should be used instead of this one.
		/// </summary>
		RawValue               = 1,

		/// <summary>
		/// The value is the raw value.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		RawValueBits = 0x00200000,

		/// <summary>
		/// No value exists in the journal for the requested data object 
		/// that meets the specified selection criteria.
		/// </summary>
		NoValue                = 2,

		/// <summary>
		/// No value exists in the journal for the requested data object 
		/// that meets the specified selection criteria.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		NoValueBits = 0x00400000,

		/// <summary>
		/// The value is a raw value, and more than one raw value exists at same timestamp. 
		/// </summary>
		ExtraValue             = 3,

		/// <summary>
		/// More than one value exists at same timestamp. 
		/// This value is in Xi Status Code bit position.
		/// </summary>
		ExtraValueBits = 0x00600000,

		/// <summary>
		/// Collection started / stopped / lost.
		/// </summary>
		LostValue              = 4,

		/// <summary>
		/// Collection started / stopped / lost.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		LostValueBits = 0x00800000,

		/// <summary>
		/// The value has been interpolated.
		/// </summary>
		InterpolatedValue      = 5,

		/// <summary>
		/// The value has been interpolated.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		InterpolatedValueBits = 0x00A00000,

		/// <summary>
		/// The value has been calculated.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		CalculatedValue        = 6,

		/// <summary>
		/// The value has been calculated.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		CalculatedValueBits = 0x00C00000,

		/// <summary>
		/// The value is a calculated value for an incomplete interval.
		/// </summary>
		PartialCalculatedValue = 7,

		/// <summary>
		/// The value is a calculated value for an incomplete interval.
		/// This value is in Xi Status Code bit position.
		/// </summary>
		PartialCalculatedValueBits = 0x00E00000,

		/// <summary>
		/// Xi Status Code Flag bit for historical data with no upper or lower bounding values. 
		/// This flag is not part of the HistoricalValueType bits, but is the bit just before them 
		/// (from the right) in the Xi Status code. Its definition is included in this class because 
		/// of its relationship to the other historical bits.
		/// </summary>
		HistoricalNoBoundingFlg      = 0x00100000,

		/// <summary>
		/// Flag bit for Historical Conversion Error. 
		/// This flag is not part of the HistoricalValueType bits, but is the second bit just before them 
		/// (from the right) in the Xi Status code. Its definition is included in this class because 
		/// of its relationship to the other historical bits.
		/// </summary>
		HistoricalConversionErrorFlg = 0x00080000,
	}
}