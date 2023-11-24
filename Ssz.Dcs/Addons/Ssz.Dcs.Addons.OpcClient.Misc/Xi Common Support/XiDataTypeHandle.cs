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

namespace Xi.Common.Support
{
	/// <summary>
	/// This enumeration defines handles (short form identifiers) for 
	/// standard Xi Data Type Ids, typically derived from COM data types.
	/// </summary>
	public enum XiDataTypeHandle : short
	{
		/// <summary>
		/// VT_EMPTY is transported using long with this data type.
		/// </summary>
		DataValueTypeUnknown = 0,

		/// <summary>
		/// Signed integers are transported as long with one of these data types.
		/// </summary>
		DataValueTypeInt8		= 1,
		DataValueTypeInt16		= 2,
		DataValueTypeInt32		= 3,
		DataValueTypeInt64		= 4,

		/// <summary>
		/// Unsigned integers are transported as long with one of these data types.
		/// </summary>
		DataValueTypeUInt8		= 9,
		DataValueTypeUInt16	= 10,
		DataValueTypeUint32	= 11,
		DataValueTypeUInt64	= 12,

		/// <summary>
		/// Floating point values are transported as Double with one of these data Types.
		/// </summary>
		DataValueTypeFloat32	= 17,
		DataValueTypeFloat64	= 18,

		/// <summary>
		/// Most other data types are transported as object using one of these data types.
		/// However, some of these may be transported as long.
		/// </summary>
		DataValueTypeObject	= 32,
		DataValueTypeString	= 33,
		DataValueTypeIntPtr	= 34,
		DataValueTypeCurrency	= 35,
		DataValueTypeDate		= 36,
		DataValueTypeScode		= 37,
		DataValueTypeHresult	= 38,
		DataValueTypeBoolean	= 39,
		DataValueTypeVariant	= 40,
		DataValueTypeIUnknown	= 41,
		DataValueTypeIDispatch	= 42,
		DataValueTypeDecimal	= 43,
		DataValueTypeVoid		= 44,
		DataValueTypeSafeArray	= 45,

		/// <summary>
		/// The data type was not established.
		/// </summary>
		DataValueTypeNotEstablished = 128,
	}
}
