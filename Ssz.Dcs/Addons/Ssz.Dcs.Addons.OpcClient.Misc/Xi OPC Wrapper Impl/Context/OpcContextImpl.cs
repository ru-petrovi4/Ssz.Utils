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
using Xi.Contracts.Data;
using Xi.Contracts.Constants;

using Xi.OPC.COM.API;
using Xi.OPC.COM.Impl;

using Xi.Server.Base;
using Xi.Common.Support;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// Managed OPC Servers.
	/// </summary>
	public partial class ContextImpl : ContextBase<ListRoot>
	{
		/// <summary>
		/// Managed version of the OPC DA Server.
		/// </summary>
		public IOPCServerCli IOPCServer { get; private set; }

		/// <summary>
		/// The ProgId of the OPC DA Server.
		/// </summary>
		public string IOPCServer_ProgId { get; private set; }

		/// <summary>
		/// Managed version of the OPC HDA Server.
		/// </summary>
		public IOPCHDA_ServerCli IOPCHDA_Server { get; private set; }

		/// <summary>
		/// The ProgId of the OPC HDA Server.
		/// </summary>
		public string IOPCHDAServer_ProgId { get; private set; }

		/// <summary>
		/// Managed version of the OPC A&E Server.
		/// </summary>
		public IOPCEventServerCli IOPCEventServer { get; private set; }

		/// <summary>
		/// The ProgId of the OPC A&E Server.
		/// </summary>
		public string IOPCEventServer_ProgId { get; private set; }

		/// <summary>
		/// Managed version of the OPC Common interface for the DA Server.
		/// </summary>
		public IOPCCommonCli IOPCCommonDA
		{
			get { return IOPCServer as IOPCCommonCli; }
		}
		/// <summary>
		/// Managed version of the OPC Common interface for the HDA Server.
		/// </summary>
		public IOPCCommonCli IOPCCommonHDA
		{
			get { return IOPCHDA_Server as IOPCCommonCli; }
		}
		/// <summary>
		/// Managed version of the OPC Common interface for the A&E Server.
		/// </summary>
		public IOPCCommonCli IOPCCommonAE
		{
			get { return IOPCEventServer as IOPCCommonCli; }
		}

		/// <summary>
		/// Managed interface for the OPC DA Server browser.
		/// </summary>
		public IOPCBrowseServerAddressSpaceCli IOPCBrowseServerAddressSpace
		{
			get { return IOPCServer as IOPCBrowseServerAddressSpaceCli; }
		}

		/// <summary>
		/// Managed interface for the OPC DA Server properties.
		/// </summary>
		public IOPCItemPropertiesCli IOPCItemProperties
		{
			get { return IOPCServer as IOPCItemPropertiesCli; }
		}

		/// <summary>
		/// Managed interface for the OPC HDA Server synchronous read.
		/// </summary>
		public IOPCHDA_SyncReadCli IOPCHDA_SyncRead
		{
			get { return IOPCHDA_Server as IOPCHDA_SyncReadCli; }
		}

		/// <summary>
		/// The wrapped servers that are currently accessible.
		/// The server bits in ContextOptions are used as its values.
		/// TODO: if the wrapper wraps more than one server of a given type (e.g. DA)
		/// then the values for this data member will have to be changed, along with 
		/// its associated comparisons.
		/// </summary>
		private uint AccessibleWrappedServers
		{
			get { return _accessibleWrappedServers; }
		}
		private uint _accessibleWrappedServers = 0;

		public override bool IsAccessibleDataAccess
		{
			get { return ((_accessibleWrappedServers & (uint)Contracts.Constants.ContextOptions.EnableDataAccess) > 0); }
		}
		public override bool IsAccessibleAlarmsAndEvents
		{
			get { return ((_accessibleWrappedServers & (uint)Contracts.Constants.ContextOptions.EnableAlarmsAndEventsAccess) > 0); }
		}
		public override bool IsAccessibleJournalDataAccess
		{
			get { return ((_accessibleWrappedServers & (uint)Contracts.Constants.ContextOptions.EnableJournalDataAccess) > 0); }
		}
		public override bool IsAccessibleJournalAlarmsAndEvents
		{
			get { return ((_accessibleWrappedServers & (uint)Contracts.Constants.ContextOptions.EnableJournalAlarmsAndEventsAccess) > 0); }
		}

	    public string SessionId { get; private set; }
        
	    internal void ClearAccessibleServer(ContextOptions server)
		{
			_accessibleWrappedServers &= ~(uint)server;
		}		

		public void ThrowOnDisconnectedServer(cliHRESULT hr, string progId)
		{
			if (   ((uint)hr.hResult == 0x800706ba) // "The RPC server is unavailable"
				|| ((uint)hr.hResult == 0x800706bf)) // The remote procedure called failed and did not execute
			{
				OpcServerInfo server = XiOPCWrapperServer.OpcWrappedServers.Find(ws => string.Compare(ws.ProgId, progId) == 0);
				switch (server.ServerType)
				{
					case ServerType.OPC_DA205_Wrapper:
                        ClearAccessibleServer(Contracts.Constants.ContextOptions.EnableDataAccess);
						break;
					case ServerType.OPC_AE11_Wrapper:
                        ClearAccessibleServer(Contracts.Constants.ContextOptions.EnableAlarmsAndEventsAccess);
						break;
					case ServerType.OPC_HDA12_Wrapper:
                        ClearAccessibleServer(Contracts.Constants.ContextOptions.EnableJournalDataAccess);
						break;
					default:
						break;
				}
				ThrowDisconnectedServerException(progId);
			}
		}

		public void ThrowDisconnectedServerException(string progId)
		{
			throw FaultHelpers.Create(XiFaultCodes.E_WRAPPEDSERVER_NOT_ACCESSIBLE,
			"The " + progId + " wrapped server is not currently accessible.");
		}

		/// <summary>
		/// This method is invoked to create the underlying COM servers.
		/// Each of these servers implements its coresponding methods as
		/// defined in XiOPCWrapper.Interfaces.
		/// </summary>
		/// <returns></returns>
		public uint OpcCreateInstance(ref uint lcid, ServerDescription serverDescription)
		{
			uint daLCID = 0;
			uint hdaLCID = 0;
			uint aeLCID = 0;
			uint rc = XiFaultCodes.S_OK;            

			uint numNegotiatedServers = 0;

			uint daConnectRC = XiFaultCodes.S_OK;
			uint daCallbackRC = XiFaultCodes.S_OK;
			uint daShutdownRC = XiFaultCodes.S_OK;

			uint hdaConnectRC = XiFaultCodes.S_OK;
			uint hdaShutdownRC = XiFaultCodes.S_OK;

			uint aeConnectRC = 0;
			uint aeShutdownRC = 0;

			foreach (var server in XiOPCWrapperServer.OpcWrappedServers)
			{
				switch (server.ServerType)
				{
					case ServerType.OPC_DA205_Wrapper:
						if ((ContextOptions & (uint)Contracts.Constants.ContextOptions.EnableDataAccess) > 0)
						{
                            IOPCServerCli iOPCServer = null;
#if USO
						    cliHRESULT hr1 = SimExec != null
						        ? CCreateInstance.CreateInstanceDA_Clsid(server.HostName, SimExec.OpcDAClsId, ref iOPCServer,
						            serverDescription)
						        : CCreateInstance.CreateInstanceDA(server.HostName,
						            makeUnisimServerProgIdWithModelName(server.ProgId), ref iOPCServer, serverDescription);
#else
                            cliHRESULT hr1 = CCreateInstance.CreateInstanceDA(server.HostName,
									server.ProgId, ref iOPCServer, serverDescription);
#endif
							if (hr1.Succeeded == false)
							{
								rc = daConnectRC = (uint)hr1.hResult;
							}
							else
							{
                                // Set the on data change callback 
                                IAdviseOPCDataCallbackCli iRegOPCDataCallbacks = iOPCServer as IAdviseOPCDataCallbackCli;
								if (null != iRegOPCDataCallbacks)
								{
                                    cliHRESULT hr1A = iRegOPCDataCallbacks.AdviseOnDataChange(OnDataChange);
									if (hr1A.Succeeded == false)
									{
										rc = daCallbackRC = (uint)hr1A.hResult;
									}
									else
									{
                                        // Set the DA Server Shutdown callback
                                        IAdviseOPCShutdownCli iRegOPCShutdownCallback = iOPCServer as IAdviseOPCShutdownCli;
										if (null != iRegOPCShutdownCallback)
										{
                                            cliHRESULT hr1B = iRegOPCShutdownCallback.AdviseShutdownRequest(OnDAShutdown);
											if (hr1B.Succeeded == false)
											{
												rc = daShutdownRC = (uint)hr1B.hResult;
											}
											else
											{
												iOPCServer.SetLocaleID(lcid);
												iOPCServer.GetLocaleID(out daLCID);
                                                _accessibleWrappedServers |= (uint)Contracts.Constants.ContextOptions.EnableDataAccess;
												numNegotiatedServers++;
                                                IOPCServer = iOPCServer;
                                                IOPCServer_ProgId = server.ProgId;
											}
										}
									}
								}
							}
						}
						break;

					case ServerType.OPC_HDA12_Wrapper:
						if ((ContextOptions & (uint)Contracts.Constants.ContextOptions.EnableJournalDataAccess) > 0)
						{
                            IOPCHDA_ServerCli iOPCHDA_Server = null;
#if USO
						    cliHRESULT hr2 = SimExec != null
                                ? CCreateInstance.CreateInstanceHDA_Clsid(server.HostName, SimExec.OpcHDAClsId, ref iOPCHDA_Server,
						            serverDescription)
						        : CCreateInstance.CreateInstanceHDA(server.HostName, makeUnisimServerProgIdWithModelName(server.ProgId),
						            ref iOPCHDA_Server, serverDescription);
#else
                            cliHRESULT hr2 = CCreateInstance.CreateInstanceHDA(server.HostName, 
								server.ProgId,
									ref iOPCHDA_Server, serverDescription);
#endif
							if (hr2.Succeeded == false)
							{
								rc = hdaConnectRC = (uint)hr2.hResult;
							}
							else
							{
                                // Set the HDA Server Shutdown callback
                                IAdviseOPCShutdownCli iRegOPCShutdownCallback = iOPCHDA_Server as IAdviseOPCShutdownCli;
								if (null != iRegOPCShutdownCallback)
								{
                                    cliHRESULT hr2A = iRegOPCShutdownCallback.AdviseShutdownRequest(OnHDAShutdown);
									if (hr2A.Succeeded == false)
									{
										rc = hdaShutdownRC = (uint)hr2A.hResult;
									}
									else
									{
										iOPCHDA_Server.SetLocaleID(lcid);
										iOPCHDA_Server.GetLocaleID(out hdaLCID);
                                        _accessibleWrappedServers |= (uint)Contracts.Constants.ContextOptions.EnableJournalDataAccess;
										numNegotiatedServers++;
                                        IOPCHDA_Server = iOPCHDA_Server;
                                        IOPCHDAServer_ProgId = server.ProgId;
									}
								}
							}
						}
						break;

					case ServerType.OPC_AE11_Wrapper:
						if ((ContextOptions & (uint)Contracts.Constants.ContextOptions.EnableAlarmsAndEventsAccess) > 0)
						{
                            IOPCEventServerCli iOPCEventServer = null;
#if USO
						    cliHRESULT hr3 = CCreateInstance.CreateInstanceAE(
						        server.HostName,
						        SimExec != null ? SimExec.OpcAEProgId : makeUnisimServerProgIdWithModelName(server.ProgId),
						        ref iOPCEventServer, serverDescription);
#else
                            cliHRESULT hr3 = CCreateInstance.CreateInstanceAE(
								server.HostName,
								server.ProgId,
								ref iOPCEventServer, serverDescription);
#endif

							if (hr3.Succeeded == false)
							{
								rc = aeConnectRC = (uint)hr3.hResult;
							}
							else
							{
                                // Set the A&E Server Shutdown callback
                                IAdviseOPCShutdownCli iRegOPCShutdownCallback = iOPCEventServer as IAdviseOPCShutdownCli;
								if (null != iRegOPCShutdownCallback)
								{
                                    cliHRESULT hr3A = iRegOPCShutdownCallback.AdviseShutdownRequest(OnAEShutdown);
									if (hr3A.Succeeded == false)
									{
										rc = aeShutdownRC = (uint)hr3A.hResult;
									}
									else
									{
										iOPCEventServer.SetLocaleID(lcid);
										iOPCEventServer.GetLocaleID(out aeLCID);
                                        _accessibleWrappedServers |= (uint)Contracts.Constants.ContextOptions.EnableAlarmsAndEventsAccess;
										numNegotiatedServers++;
                                        IOPCEventServer = iOPCEventServer;
                                        IOPCEventServer_ProgId = server.ProgId;
									}
								}
							}
						}
						break;                    

					default:
						rc = XiFaultCodes.E_FAIL;
						Debug.Assert(server.ServerType == ServerType.OPC_DA205_Wrapper ||
									 server.ServerType == ServerType.OPC_HDA12_Wrapper ||
									 server.ServerType == ServerType.OPC_AE11_Wrapper);
						break;
				}
			}
			// set the number of wrapped servers that were negotiated 			
			NumberOfWrappedServersForThisContext = numNegotiatedServers;

			if (numNegotiatedServers == 0)
			{
				string msg = "";

				if (daConnectRC != XiFaultCodes.S_OK)
				{
					msg += "Main Connection to DA server failed, HResult=" + daConnectRC.ToString("X") + "; ";
				}
				if (daCallbackRC != XiFaultCodes.S_OK)
				{
					msg += "Data Callback connection to DA server failed, HResult=" + daCallbackRC.ToString("X") + "; ";
				}
				if (daShutdownRC != XiFaultCodes.S_OK)
				{
					msg += "Shutdown Callback Connection to DA server failed, HResult=" + daShutdownRC.ToString("X") + "; ";
				}

				if (hdaConnectRC != XiFaultCodes.S_OK)
				{
					msg += "Main Connection to HDA server failed, HResult=" + hdaConnectRC.ToString("X") + "; ";
				}
				if (hdaShutdownRC != XiFaultCodes.S_OK)
				{
					msg += "Shutdown Callback Connection to HDA server failed, HResult=" + hdaShutdownRC.ToString("X") + "; ";
				}

				if (aeConnectRC != XiFaultCodes.S_OK)
				{
					msg += "Main Connection to A&E server failed, HResult=" + aeConnectRC.ToString("X") + "; ";
				}
				if (aeShutdownRC != XiFaultCodes.S_OK)
				{
					msg += "Shutdown Callback Connection to A&E server failed, HResult=" + aeShutdownRC.ToString("X") + "; ";
				}

				throw FaultHelpers.Create(msg);
			}
			else
				rc = XiFaultCodes.S_OK; // if at least one wrapped server opened, then set the result code to success
			if ((lcid == daLCID) || (lcid == hdaLCID) || (lcid == aeLCID))
			{
				// do nothing - the requested lcid was set
			}
			else
			{
				// TO DO - Select the appropriate locale id for your server
				if (daLCID != 0)
					lcid = daLCID;
				else if (hdaLCID != 0)
					lcid = hdaLCID;
				else if (aeLCID != 0)
					lcid = aeLCID;
				else // no lcids were set, so fail the request
				{
					throw FaultHelpers.Create("No LocaleIDs were available for any wrapped server");
				}
			}
			return rc;
		}

	    private string makeUnisimServerProgIdWithModelName(string progId)
	    {
	        if (string.IsNullOrEmpty(SessionId))
	            return progId;

	        return progId + "." + SessionId;
	    }

	    /// <summary>
		/// This override releases the OPC resources for this context implementation
		/// </summary>
		public override void OnReleaseResources()
		{
			OpcRelease();
		}

		/// <summary>
		/// By setting the refereces to null the C++ destructor 
		/// runs which then Release the COM server.
		/// </summary>
		/// <returns></returns>
		public uint OpcRelease()
		{
			uint rcda = OpcReleaseDA();
			uint rchda = OpcReleaseHDA();
			uint rcae = OpcReleaseAE();
			return (XiFaultCodes.S_OK != rcda) ? rcda : (XiFaultCodes.S_OK != rchda) ? rchda : rcae;
		}

		/// <summary>
		/// This method is used to release the OPC DA COM Server
		/// </summary>
		/// <returns></returns>
		public uint OpcReleaseDA()
		{
			uint rc = XiFaultCodes.S_OK;
			try
			{
				if (IOPCServer != null)
				{
					IAdviseOPCDataCallbackCli iRegOPCDataCallbacks = IOPCServer as IAdviseOPCDataCallbackCli;
					if (null != iRegOPCDataCallbacks)
					{
						iRegOPCDataCallbacks.UnadviseOnDataChange(OnDataChange);
					}

					IAdviseOPCShutdownCli iDaOPCShutdownCallback = IOPCServer as IAdviseOPCShutdownCli;
					if (null != iDaOPCShutdownCallback)
					{
						iDaOPCShutdownCallback.UnadviseShutdownRequest(OnDAShutdown);
					}
				}

				// Release any browsers that may be around prior to releasing the servers
				if (null != OpcDaBrowser)
				{
					OpcDaBrowser.Dispose();
					OpcDaBrowser = null;
				}

				if (null != OpcBrowser)
				{
					OpcBrowser.Dispose();
					OpcBrowser = null;
				}

				// After releasing the browsers the servers may be released
				if (null != IOPCServer)
				{
					IOPCServer.Dispose();
					IOPCServer = null;
				}
			}
			catch
			{
				rc = XiFaultCodes.E_WRAPPEDSERVER_EXCEPTION;
			}
            ClearAccessibleServer(Contracts.Constants.ContextOptions.EnableDataAccess);
			return rc;
		}

		/// <summary>
		/// This method is used to release the OPC HDA COM Server.
		/// </summary>
		/// <returns></returns>
		public uint OpcReleaseHDA()
		{
			uint rc = XiFaultCodes.S_OK;
			try
			{
				if (IOPCHDA_Server != null)
				{
					IAdviseOPCShutdownCli iHdaOPCShutdownCallback = IOPCHDA_Server as IAdviseOPCShutdownCli;
					if (null != iHdaOPCShutdownCallback)
					{
						iHdaOPCShutdownCallback.UnadviseShutdownRequest(OnDAShutdown);
					}
				}

				if (null != OpcHdaBrowser)
				{
					OpcHdaBrowser.Dispose();
					OpcHdaBrowser = null;
				}

				if (null != OpcBrowser)
				{
					OpcBrowser.Dispose();
					OpcBrowser = null;
				}

				// After releasing the browsers the servers may be released
				if (null != IOPCHDA_Server)
				{
					IOPCHDA_Server.Dispose();
					IOPCHDA_Server = null;
				}
			}
			catch
			{
				rc = XiFaultCodes.E_WRAPPEDSERVER_EXCEPTION;
			}
            ClearAccessibleServer(Contracts.Constants.ContextOptions.EnableJournalDataAccess);
			return rc;
		}

		/// <summary>
		/// This method is used to release the OPC A&E COM Server
		/// </summary>
		/// <returns></returns>
		public uint OpcReleaseAE()
		{
			uint rc = XiFaultCodes.S_OK;
			try
			{
				if (IOPCEventServer != null)
				{
					IAdviseOPCShutdownCli iAEOPCShutdownCallback = IOPCEventServer as IAdviseOPCShutdownCli;
					if (null != iAEOPCShutdownCallback)
					{
						iAEOPCShutdownCallback.UnadviseShutdownRequest(OnDAShutdown);
					}
				}

				// Release any browsers that may be around prior to releasing the servers
				if (null != OpcAeBrowser)
				{
					OpcAeBrowser.Dispose();
					OpcAeBrowser = null;
				}

				if (null != OpcBrowser)
				{
					OpcBrowser.Dispose();
					OpcBrowser = null;
				}

				// After releasing the browsers the servers may be released
				if (null != IOPCEventServer)
				{
					IOPCEventServer.Dispose();
					IOPCEventServer = null;
				}
			}
			catch
			{
				rc = XiFaultCodes.E_WRAPPEDSERVER_EXCEPTION;
			}
            ClearAccessibleServer(Contracts.Constants.ContextOptions.EnableAlarmsAndEventsAccess);
			return rc;
		}

	}
}
