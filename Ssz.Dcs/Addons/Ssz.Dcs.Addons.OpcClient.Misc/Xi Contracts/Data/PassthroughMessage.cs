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
	/// This class defines the attributes of passthrough messages.  
	/// Passthrough messages are messages sent by the client to the 
	/// server, who forwards them unchanged to the recipient. The 
	/// recipient represents the entity responsible for processing 
	/// or otherwise consuming the message.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class PassthroughMessage : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data contract 
		/// class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

		/// <summary>
		/// The name of the Passthrough as known by the Recipient.
		/// </summary>
		[DataMember] public string Name { get; set; }

		/// <summary>
		/// The text description of the Passthrough.
		/// </summary>
		[DataMember] public string Description { get; set; }

		/// <summary>
		/// Indicates, when TRUE, that the Passthrough returns its response 
		/// asynchrononously via the callback or poll interface.
		/// </summary>
		[DataMember] public bool Asynch { get; set; }

		/// <summary>
		/// The definition of the Passthrough's input message parameters.  
		/// The server is responsible for passing these parameters to the 
		/// recipient.  It is possible that the entire input passthrough 
		/// message is defined by a single data type.
		/// </summary>
		[DataMember] public List<ParameterDefinition> InParameters { get; set; }

		/// <summary>
		/// The definition of the Passthrough's output message parameters.
		/// The server is responsible for passing these parameters from the 
		/// recipient.  It is possible that the entire output passthrough 
		/// message is defined by a single data type. 
		/// </summary>
		[DataMember] public List<ParameterDefinition> OutParameters { get; set; }

	}
}