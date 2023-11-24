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
	/// This class defines standard server types.  A server may 
	/// support one or more server types.
	/// </summary>
	public class ServerType
	{
		#region Standard Server Type Ids

		/// <summary>
		/// The server is a server discovery server.
		/// </summary>
		public const uint Xi_ServerDiscoveryServer = 0x0001;

		/// <summary>
		/// The server is a native Xi data server.
		/// </summary>
		public const uint Xi_DataServer            = 0x0002;

		/// <summary>
		/// The server is a native Xi event server.
		/// </summary>
		public const uint Xi_EventServer           = 0x0004;

		/// <summary>
		/// The server is a native Xi data journal server.
		/// </summary>
		public const uint Xi_DataJournalServer     = 0x0008;

		/// <summary>
		/// The server is a native Xi event journal server.
		/// </summary>
		public const uint Xi_EventJournalServer    = 0x0010;

		/// <summary>
		/// The server wraps an OPC DA 2.05 server.
		/// </summary>
		public const uint OPC_DA205_Wrapper        = 0x0020;

		/// <summary>
		/// The server wraps an OPC Alarms and Events 1.1 server.
		/// </summary>
		public const uint OPC_AE11_Wrapper         = 0x0040;

		/// <summary>
		/// The server wraps an OPC HDA 1.2 server.
		/// </summary>
		public const uint OPC_HDA12_Wrapper        = 0x0080;

		/// <summary>
		/// The server wraps an OPC DA 3.0 server.
		/// </summary>
		public const uint OPC_DA30_Wrapper         = 0x0100;

		/// <summary>
		/// The server wraps an OPC XMLDA server.
		/// </summary>
		public const uint OPC_XMLDA_Wrapper        = 0x0200;

		/// <summary>
		/// The server wraps an OPC UA Data Access server.
		/// </summary>
		public const uint OPC_UA_DA_Wrapper        = 0x0400;

		/// <summary>
		/// The server wraps an OPC UA Alarms and Conditions server.
		/// </summary>
		public const uint OPC_UA_AC_Wrapper        = 0x0800;

		/// <summary>
		/// The server wraps an OPC UA Historical Data Access server.
		/// </summary>
		public const uint OPC_UA_HDA_Wrapper       = 0x1000;

		/// <summary>
		/// The base Xi server that wraps one or more OPC servers.
		/// </summary>
		public const uint Xi_BaseServer            = 0x2000;

		#endregion

		#region ToString() Method for Server Types

		/// <summary>
		/// This method constructs a string that contains each of the server 
		/// types specified by the serverTypes parameter.
		/// </summary>
		/// <param name="serverTypes">
		/// A bit-mask that identifies each of the server types.
		/// </param>
		/// <returns>
		/// The string representation of the server types.
		/// </returns>
		public static string ToString(uint serverTypes)
		{
			string returnString = "";
			if ((serverTypes & ServerType.Xi_ServerDiscoveryServer) > 0)
			{
				returnString = "ServerDiscoveryServer";
				serverTypes ^= ServerType.Xi_ServerDiscoveryServer; // mask off this type
			}

			if ((serverTypes & ServerType.Xi_BaseServer) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "Xi_BaseServer";
				serverTypes ^= ServerType.Xi_BaseServer; // mask off this type
			}

			if ((serverTypes & ServerType.Xi_DataServer) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "Xi_DataServer";
				serverTypes ^= ServerType.Xi_DataServer; // mask off this type
			}

			if ((serverTypes & ServerType.Xi_EventServer) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "Xi_EventServer";
				serverTypes ^= ServerType.Xi_EventServer; // mask off this type
			}

			if ((serverTypes & ServerType.Xi_DataJournalServer) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "Xi_DataJournalServer";
				serverTypes ^= ServerType.Xi_DataJournalServer; // mask off this type
			}

			if ((serverTypes & ServerType.Xi_EventJournalServer) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "Xi_EventJournalServer";
				serverTypes ^= ServerType.Xi_EventJournalServer; // mask off this type
			}

			if ((serverTypes & ServerType.OPC_DA205_Wrapper) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "OPC_DA205_Wrapper";
				serverTypes ^= ServerType.OPC_DA205_Wrapper; // mask off this type
			}

			if ((serverTypes & ServerType.OPC_AE11_Wrapper) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "OPC_AE11_Wrapper";
				serverTypes ^= ServerType.OPC_AE11_Wrapper; // mask off this type
			}

			if ((serverTypes & ServerType.OPC_HDA12_Wrapper) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "OPC_HDA12_Wrapper";
				serverTypes ^= ServerType.OPC_HDA12_Wrapper; // mask off this type
			}

			if ((serverTypes & ServerType.OPC_DA30_Wrapper) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "OPC_DA30_Wrapper";
				serverTypes ^= ServerType.OPC_DA30_Wrapper; // mask off this type
			}

			if ((serverTypes & ServerType.OPC_XMLDA_Wrapper) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "OPC_XMLDA_Wrapper";
				serverTypes ^= ServerType.OPC_XMLDA_Wrapper; // mask off this type
			}

			if ((serverTypes & ServerType.OPC_UA_DA_Wrapper) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "OPC_UA_DA_Wrapper";
				serverTypes ^= ServerType.OPC_UA_DA_Wrapper; // mask off this type
			}

			if ((serverTypes & ServerType.OPC_UA_AC_Wrapper) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "OPC_UA_AC_Wrapper";
				serverTypes ^= ServerType.OPC_UA_AC_Wrapper; // mask off this type
			}

			if ((serverTypes & ServerType.OPC_UA_HDA_Wrapper) > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				returnString += "OPC_UA_HDA_Wrapper";
				serverTypes ^= ServerType.OPC_UA_HDA_Wrapper; // mask off this type
			}

			if (serverTypes > 0)
			{
				if (returnString.Length > 0)
					returnString += ", ";
				string customTypesHex = String.Format("X", returnString);
				returnString += "Custom Server Type " + customTypesHex;
			}
			return returnString;
		}

		#endregion

		#region ConvertToContextOptions() Method for Server Types
		/// <summary>
		/// This method returns the context options for the Server Type.
		/// </summary>
		/// <param name="serverType">The server type to be converted to the corresponding ContextOptions</param>
		/// <returns></returns>
		public static ContextOptions ConvertToContextOptions(uint serverType)
		{
			ContextOptions contextOptions = 0;
			if ((0 != (serverType & ServerType.OPC_DA205_Wrapper))
				|| (0 != (serverType & ServerType.OPC_UA_DA_Wrapper))
				|| (0 != (serverType & ServerType.Xi_DataServer))
				|| (0 != (serverType & ServerType.OPC_XMLDA_Wrapper)))
				contextOptions |= (Xi.Contracts.Constants.ContextOptions.EnableDataAccess);
			if ((0 != (serverType & ServerType.OPC_AE11_Wrapper))
				|| (0 != (serverType & ServerType.OPC_UA_AC_Wrapper))
				|| (0 != (serverType & ServerType.Xi_EventServer)))
				contextOptions |= (Xi.Contracts.Constants.ContextOptions.EnableAlarmsAndEventsAccess);
			if ((0 != (serverType & ServerType.OPC_HDA12_Wrapper))
				|| (0 != (serverType & ServerType.OPC_UA_HDA_Wrapper))
				|| (0 != (serverType & ServerType.Xi_EventServer)))
				contextOptions |= (Xi.Contracts.Constants.ContextOptions.EnableJournalDataAccess);
			if (0 != (serverType & ServerType.Xi_EventJournalServer))
				contextOptions |= (Xi.Contracts.Constants.ContextOptions.EnableJournalAlarmsAndEventsAccess);
			return contextOptions;
		}
		#endregion // GetContextOptionsForServerType() Method for Server Types

	}
}
