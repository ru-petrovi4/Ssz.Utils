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
	/// <para>This class defines the configuration of a category.  
	/// Categories are defined as groupings of alarms and events 
	/// for reporting purposes.</para> 
	/// <para>The concepts for categories accessible through 
	/// this interface are defined in EEMUA Publication 191 "Alarm 
	/// Systems: A Guide to Design, Management and Procurement".
	/// See http://www.eemua.org</para>
	/// <para>EEMUA Publication 191 recommends that "Grouping 
	/// of alarms into categories and providing facilities to select 
	/// alarm lists filtered on these categories is a highly desirable 
	/// feature."</para>  
	/// <para>A category may be composed of either alarms or events, 
	/// but not both. The EventTypes and AlarmDescriptions members of 
	/// this class are used to list the the alarms or events that 
	/// belong to the category. One of these must be present, and the 
	/// other must be null.</para>
	/// <para>Occurrences of the alarms or event type that belong to 
	/// this category are reported using event messages that 
	/// contain the fields listed in the EventMessageFields member 
	/// of this class.</para>
	/// <para>Note that alarms or events that are assigned to a 
	/// given category may change during the life of a system.</para>  
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class CategoryConfiguration
	{
		/// <summary>
		/// The identifier for the category.  
		/// </summary>
		[DataMember] public uint CategoryId;

		/// <summary>
		/// The name of the category.  
		/// </summary>
		[DataMember] public string Name;

		/// <summary>
		/// The text description of the category.  
		/// </summary>
		[DataMember] public string Description;

		/// <summary>
		/// The event type associated with this category.  
		/// </summary>
		[DataMember] public EventType EventType;

		/// <summary>
		/// Event message fields supported by the server that the client 
		/// can add to event messages sent for the category.  A flag is 
		/// included for each field that indicates whether or not it 
		/// can be used for filtering.
		/// </summary>
		[DataMember] public List<ParameterDefinition> EventMessageFields;

		/// <summary>
		/// The list of Alarms that have been assigned to this Category.  
		/// If this member is null the category is configured to report 
		/// events.    
		/// </summary>
		[DataMember] public List<AlarmDescription> AlarmDescriptions;
	}
}