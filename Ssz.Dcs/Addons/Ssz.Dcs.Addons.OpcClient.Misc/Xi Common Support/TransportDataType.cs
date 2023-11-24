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
	/// This enumeration is not include in any contract interface method.
	/// However, it may be useful to other elements of the system.
	/// </summary>
	public enum TransportDataType : short
	{
		/// <summary>
		/// Unknown how the data value is / was transported.
		/// VT_EMPTY in an Unknown type
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// The data value is / was transported as a double (64 Bits).
		/// </summary>
		Double,

		/// <summary>
		/// The data value is / was transported as a uint (32 Bits).
		/// </summary>
		Uint,

		/// <summary>
		/// The data value is / was transported as an object.
		/// </summary>
		Object,

		/// <summary>
		/// The data value is / was transported as an Event Message;
		/// </summary>
		EventMessage,

		/// <summary>
		/// This must be the last entry in the enum!
		/// </summary>
		MaxTransportDataType
	};
}
