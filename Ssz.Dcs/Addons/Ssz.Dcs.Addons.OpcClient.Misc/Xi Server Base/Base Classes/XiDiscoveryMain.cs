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
using System.Diagnostics;
using Ssz.Utils;
using Ssz.Utils.Net4;
using Xi.Contracts.Constants;

namespace Xi.Server.Base
{
	public class XiDiscoveryMain
	{
		/// <summary>
		/// 
		/// </summary>
		public enum MainProgramType
		{
			NoAction,
			ConsoleModeDiscoveryServer, 
			ServiceModeDiscoveryServer
		};

		private MainProgramType _mainType;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mainType"></param>
		public XiDiscoveryMain(MainProgramType mainType)
		{
			_mainType = mainType;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		private void WriteLine(string str)
		{
			switch (_mainType)
			{
				case MainProgramType.ConsoleModeDiscoveryServer:
					Console.WriteLine(str);
					Logger.Info(str);
					break;

				case MainProgramType.ServiceModeDiscoveryServer:
					Logger.Info(str);
					break;

				default:
					break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		public void ConsoleWriteLine(string str)
		{
			switch (_mainType)
			{
				case MainProgramType.ConsoleModeDiscoveryServer:
					Console.WriteLine(str);
					break;

				case MainProgramType.ServiceModeDiscoveryServer:
					break;

				default:
					break;
			}
		}

		// ####################################################################################

		/// <summary>
		/// 
		/// </summary>
		public void OnStartDiscoveryServer()
		{
			Debug.Assert(_mainType != MainProgramType.NoAction);
			// TODO: Put in name of your discovery server here
			WriteLine("OPC .NET Discovery Server Starting");

			ServerRoot.Initialize(typeof(ServerRoot));

			ServerRoot.Start(PnrpMeshNames.XiDiscoveryServerMesh, false);

			ServerRoot.PnrpMeshName = PnrpMeshNames.XiDiscoveryServerMesh;
			ServerRoot.RegisterPNRP();

			// TODO: Put in name of your discovery server here
			WriteLine("OPC .NET Discovery Server Running");
		}

		/// <summary>
		/// 
		/// </summary>
		public void OnStopDiscoveryServer()
		{
			ServerRoot.Stop();
			// TODO: Put in name of your discovery server here
			WriteLine("OPC .NET Discovery Server Stopped");
		}
	}
}
