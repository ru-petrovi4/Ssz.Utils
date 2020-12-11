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
	/// This enumeration specifies the standard Xi schema types.
	/// </summary>
	public class XiSchemaType 
	{
		/// <summary>
		/// <para>This SchemaType indicates that the type is defined by the 
		/// Xi Interface Contracts or by the .NET CLS, and that LocalId member 
		/// of the Xi TypeId is the string representation of the type using the 
		/// typeof() method. For this schema type, the Namespace element of the 
		/// TypeId is always null, since the namespace is incorporated into the 
		/// .NET type name.  </para> 
		/// <para>E.g. "typeof(double).ToString()" results in a TypeId as follows:</para>
		///	<para>	SchemaType = null</para>
		///	<para>	Namespace = null</para>
		///	<para>	LocalId = "System.Double"</para>
		/// <para>The string for the Xi SchemaType is null to 
		/// allow the TypeId.ToString() method to create a simple 
		/// TypeId string.</para>
		/// </summary>
		public const string Xi           = null;

		/// <summary>
		/// This SchemaType indicates that the type is defined by the 
		/// local server and that type of the LocalId member of the TypeId 
		/// is one of the standard CLS scalar types (e.g. int, string). 
		/// </summary>
		public const string LocalServer  = "LocalServer";

		/// <summary>
		/// This SchemaType indicates that the type is defined using 
		/// a W3C XML Schema.  
		/// </summary>
		public const string Xml          = "XML";

		/// <summary>
		/// This SchemaType indicates that the type is defined using 
		/// the CCITT X.680 Abstract Syntax Notation One (ASN.1). 
		/// ASN.1 is used for defining data types.
		/// </summary>
		public const string ASN1         = "ASN1";

		/// <summary>
		/// <para>This SchemaType indicates that the type is defined 
		/// using the IEC 61804 EDDL language. The EDDL SchemaType 
		/// is used to define object types for device parameters 
		/// and blocks. Example parameters include Setpoint and 
		/// ProcessVariable, and example blocks include PID, AI, 
		/// and AO blocks.</para>
		/// <para>The EDDL SchemaType is also used to define the data 
		/// types for device blocks and parameters.</para>
		/// </summary>
		public const string EDDL         = "EDDL";

		/// <summary>
		/// <para>This SchemaType indicates that the type is defined 
		/// using the IEC 61158 FMS Object Dictionary (FMS OD). The 
		/// FMS OD SchemaType is used to define data types for device 
		/// data, including function block data, network management 
		/// data, and system management data.</para>
		/// </summary>
		public const string FmsOdIndex   = "FmsOdIndex";

		/// <summary>
		/// <para>This SchemaType indicates that the type is defined 
		/// by the Fieldbus Foundation using a profile number.</para>
		/// </summary>
		public const string FFProfileNumber = "FFProfileNumber";

		/// <summary>
		/// This SchemaType indicates that the LocalId of the TypeId 
		/// identifies the semantic type of an OPC element, such as a 
		/// DA Property, AE Attribute, AE Condition, or HDA Attribute. 
		/// </summary>
		public const string OPC          = "OPC";

	}
}
