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
	/// This class defines the standard namespaces defined for the Xi specification.
	/// </summary>
	public class XiNamespace
	{
		/// <summary>
		/// The Xi namespace. The string for the Xi Namespace is null because 
		/// the .NET namespace is embedded in the type id when calling the 
		/// TypeId.ToString() method to create a TypeId string.
		/// </summary>
		public const string Xi = null;

		/// <summary>
		/// The OPC DA 2.05 namespace.
		/// </summary>
		public const string OPCDA205 = "OpcDa2.05";

		/// <summary>
		/// The OPC DA 3.0 namespace.
		/// </summary>
		public const string OPCDA30 = "OpcDa3.0";

		/// <summary>
		/// The namespace for OPC Alarms and Events Attributes.
		/// </summary>
		public const string OPCAEAttribute = "Opc.AE.Attribute";

		/// <summary>
		/// The namespace for OPC Alarms and Events Categories.
		/// </summary>
		public const string OPCAECategory = "Opc.AE.Category";

		/// <summary>
		/// The OPC HDA 1.2 namespace.
		/// </summary>
		public const string OPCHDA = "Opc.Hda";

		/// <summary>
		/// The OPC UA 1.0 namespace.
		/// </summary>
		public const string OPCUA10 = "OpcUa1.0";
	}

}
