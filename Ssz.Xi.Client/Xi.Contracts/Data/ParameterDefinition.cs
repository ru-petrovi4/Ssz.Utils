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

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class is used to define parameters, fields, and properties.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ParameterDefinition : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data 
		/// contract class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject? IExtensibleDataObject.ExtensionData { get; set; }

		/// <summary>
		/// The display name of the parameter, field, or property.  Names 
		/// are not permitted to contain the forward slash ('/') character.  
		/// This name is used as the FilterOperand in FilterCriterion.
		/// </summary>
		[DataMember] public string? Name;

		/// <summary>
		/// The optional description of the parameter, field, or property.  
		/// Null if unused.
		/// </summary>
		[DataMember] public string? Description;

		/// <summary>
		/// The object type of the parameter, field, or property.
		/// </summary>
		[DataMember] public TypeId? ObjectTypeId;

		/// <summary>
		/// The data type of the parameter, field, or property.
		/// </summary>
		[DataMember] public TypeId? DataTypeId;

	}
}