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
        /// The server wraps an USO HDA.
        /// </summary>
        public const uint USO_HDA_Wrapper          = 0x0200;

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

	}
}
