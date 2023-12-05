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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xi.Contracts.Constants
{
	/// <summary>
	/// <para>List Element Options provides a set of flags that may be set as part 
	/// of adding elements to an Xi list. </para>
	/// </summary>
	[Flags]
	public enum ListElementOptions : uint
	{
		/// <summary>
		/// No options are set for the List Element.  
		/// </summary>
		Default = 0x00000000,

		/// <summary>
		/// Override the default type of a data object list element, 
		/// and access it as a string type.
		/// </summary>
		AccessAsString = 0x00000001,

	}
}
