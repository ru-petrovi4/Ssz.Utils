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
	/// This class defines the passthrough messages that 
	/// can be sent a given recipient.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class RecipientPassthroughs
	{
		/// <summary>
		/// The identifier of the recipient of one or more passthrough 
		/// messages.  The recipient represents the entity to which 
		/// the client sends the messages and that is responsible for 
		/// processing or otherwise consuming the message.
		/// </summary>
		[DataMember] public InstanceId RecipientId { get; set; }

		/// <summary>
		/// The list of Passthough messages that can be sent to the 
		/// recipient.
		/// </summary>
		[DataMember] public List<PassthroughMessage> PassthroughMessages { get; set; }

	}
}