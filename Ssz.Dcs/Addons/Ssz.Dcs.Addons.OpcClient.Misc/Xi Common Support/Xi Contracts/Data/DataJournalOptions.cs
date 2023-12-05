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

using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class contains the options supported by the server 
	/// for history data accessible through Journal reads and 
	/// writes.  
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class DataJournalOptions
	{
		/// <summary>
		/// Historical Data Math Library supported by the server. Each 
		/// math/statistical function in the library is identified 
		/// using an InstanceId.  The namespace element of the InstanceId 
		/// identifies the party responsible for defining the function.
		/// </summary>
		[DataMember] public List<TypeAttributes> MathLibrary { get; set; }

		/// <summary>
		/// The standard and non-standard Historical Data Properties 
		/// supported by the server, and an indicator of which can 
		/// be used for filtering.    
		/// </summary>
		[DataMember] public List<ParameterDefinition> Properties;

		/// <summary>
		/// The maximum number of sample value the server will return.
		/// </summary>
		[DataMember] public uint MaxReturnValues;
	}
}