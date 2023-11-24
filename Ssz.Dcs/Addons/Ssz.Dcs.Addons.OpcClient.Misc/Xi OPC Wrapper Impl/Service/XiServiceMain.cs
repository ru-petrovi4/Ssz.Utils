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
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Diagnostics;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.Server.Base;

using ContextManager = Xi.Server.Base.ContextManager<Xi.OPC.Wrapper.Impl.ContextImpl, Xi.Server.Base.ListRoot>;


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
		public bool OnStartDataServer()
		{
			return OnStartDataServer(0, 0);
		}

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
		public bool OnStartDataServer(int xiInitiateDelay, int numCOMserverRetries)
		{
			_xiInitiateDelay = xiInitiateDelay;
			_numCOMserverRetries = numCOMserverRetries;

			Debug.Assert(_mainType != MainProgramType.NoAction);
			// TODO: Replace "OPC .NET" with the name of the server in the startup message below 
			WriteLine("OPC .NET Server Starting");

			XiOPCWrapper.Initialize(typeof(XiOPCWrapper));
			ContextManager.StartContextMonitor();

			XiOPCWrapper.Start();
			Collection<ServiceEndpoint> serverDiscoveryEPs = XiOPCWrapper.ServiceHost.Description.
				Endpoints.FindAll(typeof(Xi.Contracts.IServerDiscovery));
			string url = ServerRoot.ReplaceLocalhostInURLwithHostname(serverDiscoveryEPs[0].ListenUri.AbsoluteUri);

			// TODO: Replace "OPC .NET" with the name of the server in the discovery url message below 
			WriteLine("OPC .NET Server Discovery URL:  " + url);

			Collection<ServiceEndpoint> mexEndpoints = XiOPCWrapper.ServiceHost.Description.
				Endpoints.FindAll(typeof(IMetadataExchange));
			url = ServerRoot.ReplaceLocalhostInURLwithHostname(mexEndpoints[0].ListenUri.AbsoluteUri);
			ConsoleWriteLine("MEX URL:  " + url);

			System.Threading.Thread loadMexThread
				= new System.Threading.Thread(LoadMexEndpoints) { Name = "Xi Load Mex Thread", IsBackground = true };
			loadMexThread.Start();
            
			System.Threading.Thread loadInitiateThread
				= new System.Threading.Thread(ConnectToServerToInitializeServerData) { Name = "Xi Load Initiate Thread", IsBackground = true };
			loadInitiateThread.Start();

			ServerRoot.PnrpMeshName = PnrpMeshNames.XiServerMesh;
			System.Threading.Thread pnrpThread
				= new System.Threading.Thread(XiOPCWrapper.RegisterPNRP) { Name = "Xi PNRP Registration Thread", IsBackground = true };
			pnrpThread.Start();
	
			// TODO: Replace "OPC .NET" with the name of the server in the startup successful message below 
			WriteLine("OPC .NET Server Started");
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
			XiOPCWrapper.Stop();
			// TODO: Replace "OPC .NET" with the name of the server in the server stopped message below 
			WriteLine("OPC .NET Service Stopped");
		}

		private void LoadMexEndpoints()
		{
			if (   (XiOPCWrapper.ThisServerEntry != null)
				&& (XiOPCWrapper.ThisServerEntry.MexEndpoints != null)
				&& (XiOPCWrapper.ThisServerEntry.MexEndpoints.Count > 0)
			   )
			{
				foreach (var mexEP in XiOPCWrapper.ThisServerEntry.MexEndpoints)
				{
					if (!string.IsNullOrEmpty(mexEP.Url))
					{
						try
						{
							//ServiceEndpointCollection endpoints = MetadataResolver.Resolve(typeof(IResourceManagement),
							//                    MexEndpointAddress, MetadataExchangeClientMode.MetadataExchange);
							// ---> fails because the default buffer size is too small
							// Alternate approach:

							// Mex WsHttpBinding constructor parameter 
							bool mexReliableEnabled = false;

							WSHttpBinding mexBnd = new WSHttpBinding(SecurityMode.None, mexReliableEnabled);
							mexBnd.MaxReceivedMessageSize = 2147483647;
							mexBnd.ReaderQuotas.MaxArrayLength = 2147483647;
							mexBnd.ReaderQuotas.MaxBytesPerRead = 2147483647;
							mexBnd.ReaderQuotas.MaxDepth = 2147483647;
							mexBnd.ReaderQuotas.MaxNameTableCharCount = 2147483647;
							mexBnd.ReaderQuotas.MaxStringContentLength = 2147483647;

							MetadataExchangeClient mexClient = new MetadataExchangeClient(mexBnd);

							List<ContractDescription> contracts = new List<ContractDescription>();
							contracts.Add(new ContractDescription("IResourceManagement", "urn:xi/contracts"));

							//ServiceEndpointCollection endpoints = MetadataResolver.Resolve(contracts,
							//                    MexEndpointAddress, MetadataExchangeClientMode.MetadataExchange, mexClient );
							// result is empty for unknown reasons
							// Alternate approach:
							MetadataSet metadataSet = mexClient.GetMetadata(new EndpointAddress(mexEP.Url));
							WsdlImporter importer = new WsdlImporter(metadataSet);
							Collection<ContractDescription> __contractDescriptions = importer.ImportAllContracts();

							//Resolve wsdl into ServiceEndpointCollection
							UriBuilder mexUriBuilder = new UriBuilder(mexEP.Url);
							ServiceEndpointCollection serviceEndpointCollection = MetadataResolver.Resolve(
								__contractDescriptions, mexUriBuilder.Uri,
								MetadataExchangeClientMode.MetadataExchange, mexClient);

							break;
						}
						catch //(Exception e)
						{
						    //Logger.Verbose(e);
						    // do nothing if this mex ep doesn't work
						}
					}
				}
			}
		}

		private void ConnectToServerToInitializeServerData()
		{
			System.Threading.Thread.Sleep(_xiInitiateDelay); // wait to give the COM servers time to start

			string contextId = null;
			IResourceManagement resourceManagement = null;

			while (_numCOMserverRetries > -1)
			{
				_numCOMserverRetries--;

				uint contextOptions = (uint)ContextOptions.NoOptions;
				try
				{
					// Connect to the server to allow it to finish initialization
					ServiceEndpoint endpoint = XiOPCWrapper.ServiceHost.Description.Endpoints.Find(typeof(IResourceManagement));
					ChannelFactory<IResourceManagement> sdFac = new ChannelFactory<IResourceManagement>(endpoint);
					resourceManagement = sdFac.CreateChannel();
					uint lcid = (uint)System.Globalization.CultureInfo.CurrentCulture.LCID;
					uint contextTimeout = 0;
					string reInitiateKey = null;
					XiOPCWrapper.ServerInitializing = true;
					contextId = resourceManagement.Initiate(
						XiOPCWrapper.ThisServerEntry.ServerDescription.ServerName, System.Environment.MachineName,
						ref lcid, ref contextTimeout, ref contextOptions, out reInitiateKey);
				}
				catch //(Exception e)
				{
				    //Logger.Verbose(e);
					return;
				}
				finally
				{
					if (null != resourceManagement && null != contextId)
					{
						try
						{
							resourceManagement.Conclude(contextId);
                            // supress klocwork error reporting
                            var channel = resourceManagement as IChannel;
                            if (null != channel)
                                channel.Close();
							resourceManagement = null;
							contextId = null;
						}
						catch (Exception ex)
						{
							// TODO: Replace "OPC .NET" with the name of the server in the server Conclude message below 
							WriteLine("DeltaV OPC .NET Server Conclude() failed: " + ex.Message);
							throw (ex);
						}
					}
				}
				if (   (_xiInitiateDelay == 0)
					||
					   (   ((contextOptions & (uint)ContextOptions.EnableDataAccess) > 0)
						&& ((contextOptions & (uint)ContextOptions.EnableAlarmsAndEventsAccess) > 0)
					   )
				   )
				{
					break; // break out of the while loop if there is no delay or if both the DA and A&E servers started
				}
				else if (_xiInitiateDelay > 0) // Wait for the COM servers to start up.
				{
					System.Threading.Thread.Sleep(_xiInitiateDelay);
				}
			}
		}

	}
}
