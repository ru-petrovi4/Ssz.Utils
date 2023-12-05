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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// Attributes for a list - includes data, journal, event and history lists.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ListAttributes : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data contract 
		/// class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

		#region Data Members

		/// <summary>
		/// Result Code from the list being defined or condition of list for list attributes.
		/// </summary>
		[DataMember] public uint ResultCode { get; set; }

		/// <summary>
		/// The client-defined identifier used to access the list
		/// This value is used as the listId for responses from the server.
		/// </summary>
		[DataMember] public uint ClientId { get; set; }

		/// <summary>
		/// The server-defined identifier used to access the list
		/// This value is used as the listId for requests made to 
		/// the server.
		/// </summary>
		[DataMember] public uint ServerId { get; set; }

		/// <summary>
		/// Indicates the type of list to be created.
		/// The standard list types are defined by the Xi.Contacts.Constants.StandardListType enumeration. 
		/// </summary>
		[DataMember] public uint ListType { get; set; }

		/// <summary>
		/// When a list is Enabled read, write and poll operations may be 
		/// performed on the list elements.  Also only enabled lists will 
		/// report data by way of callbacks.  Note that polling and callbacks 
		/// are generally mutually exclusive.
		/// </summary>
		[DataMember] public bool Enabled { get; set; }

		/// <summary>
		/// The rate, expressed in milliseconds, at which the server 
		/// updates the elements of a list with values from the 
		/// underlying system.  A value of 0 indicates that updating 
		/// is exception-based.
		/// </summary>
		[DataMember] public uint UpdateRate { get; set; }

		/// <summary>
		/// <para>An optional-use member that indicates that the server is 
		/// to buffer data updates, rather than overwriting them, until either 
		/// the time span defined by the buffering rate expires or the values 
		/// are transmitted to the client in a callback or poll response. If 
		/// the time span expires, then the oldest value for a data object is 
		/// discarded when a new value is received from the underlying system.</para>
		/// <para>The value of the bufferingRate is set to 0 to indicate 
		/// that it is not to be used and that new values overwrite (replace) existing 
		/// cached values.  </para>
		/// <para>When used, this parameter contains the client-requested buffering 
		/// rate, which the server may negotiate up or down, or to 0 if the 
		/// server does not support the buffering rate. </para>
		/// <para>The FeaturesSupported member of the StandardMib is used to indicate 
		/// server support for the buffering rate.</para>
		/// </summary>
		[DataMember] public uint BufferingRate { get; set; }

		/// <summary>
		/// The current number of the elements in the list.
		/// </summary>
		[DataMember] public int CurrentCount { get; set; }

		/// <summary>
		/// Specifies if and how the list is sorted.
		/// Standard values are defined by the SortType enumeration.
		/// The high-order 8 bits are used to define non-standard sort 
		/// orders. 
		/// </summary>
		[DataMember] public ushort HowSorted { get; set; }

		/// <summary>
		/// <para>Specifies the sort keys for the list.  The sort keys 
		/// are identified by their names or their InstanceIds.</para>
		/// <para>For example, if the list is a list of EventMessages that 
		/// is sorted on the OccurrenceTime and then on the 
		/// SourceId, this list will contain "OccurrenceTime" and 
		/// "SourceId".</para>
		/// </summary>
		[DataMember] public List<string> SortKeys { get; set; }

		/// <summary>
		/// The current Filter Set for this list.
		/// </summary>
		[DataMember] public FilterSet FilterSet { get; set; }

		#endregion
	}
}