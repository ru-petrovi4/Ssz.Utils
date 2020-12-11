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
using System.Collections.Generic;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// <para>This enum contains the element ids for each of the 
	/// StandardMib elements that can be included in the list of  
	/// ChangedMibElementIds.</para>
	/// </summary>
	[Flags]
	[DataContract(Namespace = "urn:xi/data")]
	public enum MibElementIds : uint
	{
		/// <summary>
		/// The identifier for the OptionalMethodsSupported MIB element.  
		/// </summary>
		[EnumMember] MethodsSupported                   = 0x01,

		/// <summary>
		/// The identifier for the OptionalFeaturesSupported MIB element.  
		/// </summary>
		[EnumMember] FeaturesSupported                  = 0x02,

		/// <summary>
		/// The identifier for the RecipientPassthroughs MIB element.  
		/// </summary>
		[EnumMember] RecipientPassthroughs              = 0x04,

		/// <summary>
		/// The identifier for the ObjectRoles MIB element.  
		/// </summary>
		[EnumMember] ObjectRoles                        = 0x08,

		/// <summary>
		/// The identifier for the EventMessageFilters MIB element.  
		/// </summary>
		[EnumMember] EventMessageFilters                = 0x10,

		/// <summary>
		/// The identifier for the CategoryConfiguration MIB element.  
		/// </summary>
		[EnumMember] EventCategoryConfigurations        = 0x20,

		/// <summary>
		/// The identifier for the DataJournalFilters MIB element.  
		/// </summary>
		[EnumMember] DataJournalFilters                 = 0x40,

		/// <summary>
		/// The identifier for the DataJournalOptions MIB element.  
		/// </summary>
		[EnumMember] DataJournalOptions                 = 0x80,

		/// <summary>
		/// The identifier for the EventJournalMessageFilters MIB element.  
		/// </summary>
		[EnumMember] EventJournalMessageFilters         = 0x100
	}

	/// <summary>
	/// <para>This class defines the standard Management Objects of the 
	/// server.  Management objects in this class that are not supported 
	/// by the server are set to null.</para>  
	/// <para>See the Xi GetStandardMib and GetVendorMib methods for 
	/// additional information.</para> 
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class StandardMib
	{
		#region Data Members

		/// <summary>
		/// The current version of the standard MIB. Each time a MIB 
		/// element changes, the version number of the MIB is incremented.  
		/// The InstanceId of the Current Version of the MIB is 
		/// "Xi:MIB/CurrentVersion".  Clients may add the Current 
		/// Version to a List to detect MIB Changes.  
		/// </summary>
		[DataMember] public uint CurrentVersion { get; set; }

		/// <summary>
		/// Ids of the standard MIB Elements that changed causing the  
		/// current version to be incremented.  Each time the Current 
		/// Version is updated, the ChangeFlags are cleared, and then 
		/// reset to identify the newly changed standard MIB Elements.
		/// </summary>
		[DataMember] public uint ChangedMibElementIds;

		/// <summary>
		/// The list of optional Xi Methods supported by the server.  
		/// This list may be different depending on the security access 
		/// restrictions placed on the context.  The Xi methods are 
		/// defined by the XiMethods enumeration.
		/// </summary>
		[DataMember] public ulong MethodsSupported;

		/// <summary>
		/// The list of optional Xi Features supported by the server.  
		/// The Xi features are defined by the XiFeatures enumeration.
		/// </summary>
		[DataMember] public ulong FeaturesSupported;

		/// <summary>
		/// The list of Recipients and the Passthroughs that can be 
		/// sent to them.  
		/// </summary>
		[DataMember] public List<RecipientPassthroughs>? RecipientPassthroughsList { get; set; }

		/// <summary>
		/// The list of Object Roles supported by the server.  
		/// </summary>
		[DataMember] public List<ObjectRole>? ObjectRoles { get; set; }

		/// <summary>
		/// <para>Names of the event message fields that can be used for filtering.  
		/// Standard field names that can be used in filters are defined in the 
		/// FilterOperand class.</para> 
		/// <para>Names of non-standard event message fields that a server can include 
		/// in this list are defined in the EventMessageFields element for each 
		/// CategoryConfiguration contained in the EventCategoryConfigurations 
		/// StandardMib element. </para>
		/// </summary>
		[DataMember] public List<string>? EventMessageFilters;

		/// <summary>
		/// The configuration of the server's Event Categories.
		/// </summary>
		[DataMember] public List<CategoryConfiguration>? EventCategoryConfigurations { get; set; }

		/// <summary>
		/// <para>Names of the historical data properties that can be used for filtering.  
		/// Standard historical data properties that can be used in filters are defined 
		/// in the FilterOperand class.</para> 
		/// <para>Names of non-standard historical data properties that a server can 
		/// include in this list are contained in the DataJournalOptions StandardMib 
		/// element. </para>
		/// </summary>
		[DataMember] public List<string>? DataJournalFilters;

		/// <summary>
		/// The Data Journal options supported by the server. 
		/// </summary>
		[DataMember] public DataJournalOptions? DataJournalOptions { get; set; }

		/// <summary>
		/// <para>Names of the event message fields that can be used for filtering 
		/// historical alarms and events.  Standard field names that can be used in 
		/// filters are defined in the FilterOperand class.</para> 
		/// <para>The server may include the names of additional, non-standard 
		/// event message fields in this list. </para>
		/// </summary>
		[DataMember] public List<string>? EventJournalMessageFilters;

		/// <summary>
		/// The configuration of the Event Journal's Event Categories.
		/// </summary>
		[DataMember] public List<CategoryConfiguration>? EventJournalCategoryConfiguration { get; set; }

		/// <summary>
		/// The identities and descriptions of Vendor objects included 
		/// in the MIB.
		/// </summary>
		[DataMember] public List<ObjectAttributes>? VendorMibObjects;

		#endregion
	}
}