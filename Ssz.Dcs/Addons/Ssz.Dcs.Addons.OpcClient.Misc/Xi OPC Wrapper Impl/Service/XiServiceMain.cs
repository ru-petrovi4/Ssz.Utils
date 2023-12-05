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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.Server.Base;

using ContextManager = Xi.Server.Base.ContextManager<Xi.OPC.Wrapper.Impl.ContextImpl, Xi.Server.Base.ListRoot>;
using Ssz.Utils;


namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// 
	/// </summary>
	public class XiServiceMain
	{
		/// <summary>
		/// 
		/// </summary>
		public enum MainProgramType
		{
			NoAction,
			ConsoleModeDataServer,
			ServiceModeDataServer
		};

		private MainProgramType _mainType;

		private int _xiInitiateDelay;
		private int _numCOMserverRetries;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mainType"></param>
		public XiServiceMain(MainProgramType mainType)
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
				case MainProgramType.ConsoleModeDataServer:
					//Console.WriteLine(str);
					//Logger.Info(str);
					break;

				case MainProgramType.ServiceModeDataServer:
					//Logger.Info(str);
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
				case MainProgramType.ConsoleModeDataServer:
					Console.WriteLine(str);
					break;

				case MainProgramType.ServiceModeDataServer:
					break;

				default:
					break;
			}
		}

		// ####################################################################################
		/// <summary>
		/// 
		/// </summary>
		public bool OnStartDataServer(Ssz.Utils.CaseInsensitiveDictionary<string> contextParams)
		{
			return OnStartDataServer(contextParams, 0, 0);
		}

		public XiOPCWrapper Server { get; private set; }

        /// <summary>
        /// This method starts the Xi server and attempts to connect to the wrapped COM servers
        /// </summary>
        /// <param name="xiInitiateDelay">Time to wait before attempting to connect to the 
        /// Xi server.  This allows the startup to wait for external conditions, such as the 
        /// startup of the COM servers to take place.</param>
        /// <param name="numCOMserverRetries">The number of times the startup will retry to connect 
        /// to each wrapped COM server. The value of 0 indicates that only the initial attempt 
        /// will be made.</param>
        /// <returns>Returns true if the startup succeeded. Otherwise false.</returns>
        public bool OnStartDataServer(Ssz.Utils.CaseInsensitiveDictionary<string> contextParams, int xiInitiateDelay, int numCOMserverRetries)
		{
			_xiInitiateDelay = xiInitiateDelay;
			_numCOMserverRetries = numCOMserverRetries;

			Debug.Assert(_mainType != MainProgramType.NoAction);
			// TODO: Replace "OPC .NET" with the name of the server in the startup message below 
			WriteLine("OPC .NET Server Starting");

			XiOPCWrapper.Initialize();
            XiOPCWrapper.Initialize(contextParams);

            ContextManager.StartContextMonitor();

			XiOPCWrapper.Start();
			Server = new XiOPCWrapper();

            return true;
		}        

        /// <summary>
        /// 
        /// </summary>
        public void OnStopDataServer()
		{
			ServerStatus serverStatus = new ServerStatus();
			serverStatus.ServerType = XiOPCWrapper.BaseXiServerType;
			serverStatus.ServerState = ServerState.Aborting;
			serverStatus.ServerName = XiOPCWrapper.ServerDescription.ServerName;
			serverStatus.CurrentTime = DateTime.UtcNow;
			// TODO: Replace "OPC .NET" with the name of the server in the server stopped message below 
			ContextManager.OnShutdown(serverStatus, "OPC .NET Service Stopped");
			ContextManager.StopContextMonitor();			
			// TODO: Replace "OPC .NET" with the name of the server in the server stopped message below 
			WriteLine("OPC .NET Service Stopped");
		}		
	}
}
