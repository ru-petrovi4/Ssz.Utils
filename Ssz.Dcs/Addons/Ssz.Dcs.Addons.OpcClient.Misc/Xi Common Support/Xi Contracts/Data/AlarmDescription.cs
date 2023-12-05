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
	/// <para>This class is used to provide the description 
	/// of an alarm.</para>  
	/// <para>The concepts for alarms and events accessible through 
	/// this interface are defined in EEMUA Publication 191 "Alarm 
	/// Systems: A Guide to Design, Management and Procurement".
	/// See http://www.eemua.org</para>
	/// <para>EEMUA Publication 191 describes alarms as "signals which 
	/// are annunciated to the operator, typically by an audible sound, 
	/// some form of visual indication, using flashing, and by the 
	/// presentation of a message or some other identifier."</para>  
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class AlarmDescription : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data 
		/// contract class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

		/// <summary>
		/// The namespace qualified name of the container for alarms with 
		/// multiple conditions, such as grouped or eclipsed alarms. .  
		/// Null if the alarm is not a grouped or eclipsed alarm. 
		/// </summary>
		[DataMember] public TypeId MultiplexedAlarmContainer;

		/// <summary>
		/// The namespace qualified name of the alarm condition.  
		/// Examples include HI_HI, HI, LO, and LO_LO.  If the alarm 
		/// is a simple alarm, then this list contains a single 
		/// </summary>
		[DataMember] public List<TypeId> AlarmConditionNames;

	}
}