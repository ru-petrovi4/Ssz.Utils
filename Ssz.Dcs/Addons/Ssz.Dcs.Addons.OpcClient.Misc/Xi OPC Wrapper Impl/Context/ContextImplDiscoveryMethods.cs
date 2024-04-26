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

using Xi.Contracts.Data;
using Xi.Contracts.Constants;
using Xi.Server.Base;
using Xi.OPC.COM.API;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// This partial class defines the Discovery Methods of the server 
	/// implementation that override the virtual methods defined in the 
	/// Context folder of the ServerBase project.
	/// </summary>
	public partial class ContextImpl
		: ContextBase<ListRoot>
	{

		/// <summary>
		/// This method implements the server-specific behavior of the corresponding 
		/// Xi interface method.  It overrides its virtual method in the ContextBase 
		/// class of the ServerBase project.
		/// </summary>
		/// <returns>
		/// The status of the Xi server and the status of wrapped servers. 
		/// </returns>
		public override List<ServerStatus> OnStatus()
		{
			List<ServerStatus> serverStatusList = new List<ServerStatus>();
			lock (ContextServerInfoLock)
			{
				foreach (OpcServerInfo wrappedServer in XiOPCWrapperServer.ConfiguredOpcServerInfos)
				{
					var serverStatus = new ServerStatus();
					serverStatus.ServerType = wrappedServer.ServerType;
					serverStatus.ServerName = wrappedServer.ProgId;
					switch (wrappedServer.ServerType)
					{
						case ServerType.OPC_DA205_Wrapper:
							{
								if (IsAccessibleDataAccess == false)
								{
									serverStatus.ServerState = ServerState.NotOperational;
								}
								else
								{
									OPCSERVERSTATUS opcServerStatus = null;
									cliHRESULT HR = IOPCServer.GetStatus(out opcServerStatus);
									if (HR.Succeeded)
									{
										serverStatus.CurrentTime = opcServerStatus.dtCurrentTime;
										switch (opcServerStatus.dwServerState)
										{
											case OPCSERVERSTATE.OPC_STATUS_FAILED:
												serverStatus.ServerState = ServerState.Faulted;
												break;
											case OPCSERVERSTATE.OPC_STATUS_NOCONFIG:
												serverStatus.ServerState = ServerState.NeedsConfiguration;
												break;
											case OPCSERVERSTATE.OPC_STATUS_RUNNING:
												serverStatus.ServerState = ServerState.Operational;
												break;
											case OPCSERVERSTATE.OPC_STATUS_SUSPENDED:
												serverStatus.ServerState = ServerState.OutOfService;
												break;
											case OPCSERVERSTATE.OPC_STATUS_TEST:
												serverStatus.ServerState = ServerState.Diagnostics;
												break;
											default:
												break;
										}
									}
									else
									{
                                        ClearAccessibleServerTypes(Contracts.Constants.AccessibleServerTypes.DataAccess);
										serverStatus.ServerState = ServerState.NotConnected;
										serverStatus.CurrentTime = DateTime.UtcNow;
									}
								}
							}
							break;

						case ServerType.OPC_DA30_Wrapper:
							{
								// TODO: Fill this in for DA 3.0 wrappers
							}
							break;						

						case ServerType.OPC_AE11_Wrapper:
							{
								if (IsAccessibleAlarmsAndEvents == false)
								{
									serverStatus.ServerState = ServerState.NotOperational;
								}
								else
								{
									cliOPCEVENTSERVERSTATUS opcEventServerStatus = null;
									cliHRESULT HR = IOPCEventServer.GetStatus(out opcEventServerStatus);
									if (HR.Succeeded)
									{
										serverStatus.CurrentTime = opcEventServerStatus.dtCurrentTime;
										switch (opcEventServerStatus.dwServerState)
										{
											case OPCEVENTSERVERSTATE.OPCAE_STATUS_FAILED:
												serverStatus.ServerState = ServerState.Faulted;
												break;
											case OPCEVENTSERVERSTATE.OPCAE_STATUS_NOCONFIG:
												serverStatus.ServerState = ServerState.NeedsConfiguration;
												break;
											case OPCEVENTSERVERSTATE.OPCAE_STATUS_RUNNING:
												serverStatus.ServerState = ServerState.Operational;
												break;
											case OPCEVENTSERVERSTATE.OPCAE_STATUS_SUSPENDED:
												serverStatus.ServerState = ServerState.OutOfService;
												break;
											case OPCEVENTSERVERSTATE.OPCAE_STATUS_TEST:
												serverStatus.ServerState = ServerState.Diagnostics;
												break;
											default:
												break;
										}
									}
									else
									{
                                        ClearAccessibleServerTypes(Contracts.Constants.AccessibleServerTypes.AlarmsAndEventsAccess);
										serverStatus.ServerState = ServerState.NotConnected;
										serverStatus.CurrentTime = DateTime.UtcNow;
									}
								}
							}
							break;

						case ServerType.OPC_HDA12_Wrapper:
							{
								if (IsAccessibleJournalDataAccess == false)
								{
									serverStatus.ServerState = ServerState.NotOperational;
								}
								else
								{
									OPCHDA_SERVERSTATUS opcHdaServerStatus;
									DateTime dtCurrentTime;
									DateTime dtStartTime;
									ushort wMajorVersion;
									ushort wMinorVersion;
									ushort wBuildNumber;
									uint dwMaxReturnValues;
									string sStatusString;
									string sVendorInfo;

									cliHRESULT HR = IOPCHDA_Server.GetHistorianStatus(out opcHdaServerStatus,
																					  out dtCurrentTime,
																					  out dtStartTime,
																					  out wMajorVersion,
																					  out wMinorVersion,
																					  out wBuildNumber,
																					  out dwMaxReturnValues,
																					  out sStatusString,
																					  out sVendorInfo);
									if (HR.Succeeded)
									{
										serverStatus.CurrentTime = dtCurrentTime;
										switch (opcHdaServerStatus)
										{
											case OPCHDA_SERVERSTATUS.OPCHDA_DOWN:
												serverStatus.ServerState = ServerState.Faulted;
												break;
											case OPCHDA_SERVERSTATUS.OPCHDA_UP:
												serverStatus.ServerState = ServerState.Operational;
												break;
											case OPCHDA_SERVERSTATUS.OPCHDA_INDETERMINATE:
												serverStatus.ServerState = ServerState.NotOperational;
												break;
											default:
												break;
										}
									}
									else
									{
                                        ClearAccessibleServerTypes(Contracts.Constants.AccessibleServerTypes.JournalDataAccess);
										serverStatus.ServerState = ServerState.NotConnected;
										serverStatus.CurrentTime = DateTime.UtcNow;
									}
								}
							}
							break;						

						default:
							break;
					}
					serverStatusList.Add(serverStatus);
				}
			}
			return serverStatusList;
		}

		/// <summary>
		/// This method implements the server-specific behavior of the corresponding 
		/// Xi interface method.  It overrides its virtual method in the ContextBase 
		/// class of the ServerBase project.
		/// </summary>
		/// <param name="resultCodes">
		/// The result codes for which text descriptions are being requested.
		/// </param>
		/// <returns>
		/// The list of result codes and if a result code indicates success, 
		/// the requested text descriptions. The size and order of this 
		/// list matches the size and order of the resultCodes parameter.
		/// </returns>
		public override List<RequestedString> OnLookupResultCodes(List<uint> resultCodes)
		{
			List<RequestedString> errorStringList = new List<RequestedString>();
			lock (ContextServerInfoLock)
			{
				foreach (var resultCode in resultCodes)
				{
					RequestedString requestedString = new RequestedString();
					requestedString.ResultCode = XiFaultCodes.OPC_E_NOTFOUND;
					foreach (var wrappedServer in XiOPCWrapperServer.ConfiguredOpcServerInfos)
					{
						cliHRESULT HR = new cliHRESULT();
						switch (wrappedServer.ServerType)
						{
							case ServerType.OPC_DA205_Wrapper:
								if (IsAccessibleDataAccess)
								{
									HR = IOPCCommonDA.GetErrorString(resultCode, out requestedString.String);
									if (HR.Succeeded == false)
                                        ClearAccessibleServerTypes(Contracts.Constants.AccessibleServerTypes.DataAccess);
								}
								break;
							case ServerType.OPC_AE11_Wrapper:
								if (IsAccessibleAlarmsAndEvents)
								{
									HR = IOPCCommonAE.GetErrorString(resultCode, out requestedString.String);
									if (HR.Succeeded == false)
                                        ClearAccessibleServerTypes(Contracts.Constants.AccessibleServerTypes.AlarmsAndEventsAccess);
								}
								break;
							case ServerType.OPC_HDA12_Wrapper:
								if (IsAccessibleJournalDataAccess)
								{
									HR = IOPCCommonHDA.GetErrorString(resultCode, out requestedString.String);
									if (HR.Succeeded == false)
                                        ClearAccessibleServerTypes(Contracts.Constants.AccessibleServerTypes.JournalDataAccess);
								}
								break;
							case ServerType.OPC_DA30_Wrapper:
								break;							
							default:
								break;
						}
						if ((HR.Succeeded) && (string.IsNullOrEmpty(requestedString.String) == false))
						{
							// If a good string was returned, return this to the client. 
							// Otherwise continue looping through the supported server types
							requestedString.ResultCode = XiFaultCodes.S_OK;
							break;  // from the server loop, and get the next resultCode
						}
						else
							requestedString.ResultCode = (uint)HR.hResult;
					}
					// add the requested string to the list to return.
					// if the requested string was not found, the requested string that was 
					// returned by the last OnGetErrorString() call will be used.
					errorStringList.Add(requestedString);
				}
			}
			return errorStringList;
		}

		public override List<ObjectAttributes> OnFindObjects(FindCriteria findCriteria, uint numberToReturn)
		{
			List<ObjectAttributes> listOfObjAttrs = new List<ObjectAttributes>();
			lock (ContextBrowseLock)
			{
				// if the findCriteria is null, this is a continuation call from the previous find
				if (findCriteria == null)
				{
					if (OpcBrowser.CurrentBrowseContext.ListOfObjectAttributes != null)
					{
						if (OpcBrowser.CurrentBrowseContext.ListOfObjectAttributes != null)
							listOfObjAttrs = OpcBrowser.CurrentBrowseContext.CopyDestructiveListOfObjectAttributes((int)numberToReturn);
						else
							listOfObjAttrs = GetEnumeratedBrowseResults(numberToReturn, listOfObjAttrs);
					}
					else
						listOfObjAttrs = GetEnumeratedBrowseResults(numberToReturn, listOfObjAttrs);
				}
				else // if the findCriteria is present, this is a new find call
				{
					// If the starting path is the root, AND If there is more than one server type supported by the server, 
					// => each will have its own root below the top-level root, so  
					// Return the wrapped server roots.
					if ((IsStartingPathTheRoot(findCriteria.StartingPath))
						&& (XiOPCWrapperServer.ConfiguredOpcServerInfos.Count > 1)
					   )
					{
						OpcBrowser = new OpcBrowser();
						OpcBrowser.CurrentBrowseContext.ListOfObjectAttributes = new List<ObjectAttributes>();
						foreach (var root in XiOPCWrapperServer.WrappedServerRoots)
						{
							if (root.Name == XiOPCWrapperServer.DA205_RootName)
							{
								if (IsAccessibleDataAccess)
									root.ObjectFlags &= ~(uint)ObjectAttributeFlags.NotAccessible;
								else
									root.ObjectFlags |= (uint)ObjectAttributeFlags.NotAccessible;
								OpcBrowser.CurrentBrowseContext.ListOfObjectAttributes.Add(root);
							}
							else if (root.Name == XiOPCWrapperServer.AE_RootName)
							{
								if (IsAccessibleAlarmsAndEvents)
									root.ObjectFlags &= ~(uint)ObjectAttributeFlags.NotAccessible;
								else
									root.ObjectFlags |= (uint)ObjectAttributeFlags.NotAccessible;
								OpcBrowser.CurrentBrowseContext.ListOfObjectAttributes.Add(root);
							}
							else if (root.Name == XiOPCWrapperServer.HDA_RootName)
							{
								if (IsAccessibleJournalDataAccess)
									root.ObjectFlags &= ~(uint)ObjectAttributeFlags.NotAccessible;
								else
									root.ObjectFlags |= (uint)ObjectAttributeFlags.NotAccessible;
								OpcBrowser.CurrentBrowseContext.ListOfObjectAttributes.Add(root);
							}
						}
						listOfObjAttrs = OpcBrowser.CurrentBrowseContext.CopyDestructiveListOfObjectAttributes((int)numberToReturn);
					}
					else // starting path is not the root or there is only one wrapped server
					{
						CurrentBrowseContext currentBrowseContext = PrepareToBrowse(findCriteria);
						if (   (currentBrowseContext != null)
							&& (currentBrowseContext.ListOfObjectAttributes != null)
							&& (currentBrowseContext.ListOfObjectAttributes.Count > 0))
						{
							listOfObjAttrs = currentBrowseContext.ListOfObjectAttributes;
						}
						// Skip this next code if the client only asked for the starting object attributes
						if (   (currentBrowseContext != null)
							&& (currentBrowseContext.StartingObjectAttributesFilterOperand != (int)StartingObjectFilterValues.StartingObjectOnly))
						{
							switch (currentBrowseContext.ChangeBrowsePositionResults)
							{
								case CurrentBrowseContext.ChangeBrowsePositionSuccess: // ready to browse
									if ((currentBrowseContext.BrowseTypesRequested & (int)OPCBROWSETYPE.OPC_BRANCH) != 0)
									{
										currentBrowseContext.SomethingToReturn |= OpcBrowser.BrowseBranches();
										// if there were no branches, see if the client also wanted to look for leaves
										// if there were branches, then SomethingToReturn will cause the enumerator for 
										// branches to be processed before looking for leaves
										if (   (currentBrowseContext.SomethingToReturn == false)
											&& ((currentBrowseContext.BrowseTypesRequested & (int)OPCBROWSETYPE.OPC_LEAF) != 0)
										   )
										{
											currentBrowseContext.SomethingToReturn |= OpcBrowser.BrowseLeaves();
										}
									}
									else // branches were not requested so leaves had to be requested
									{
										currentBrowseContext.SomethingToReturn |= OpcBrowser.BrowseLeaves();
									}
									break;

								// couldn't browse to the leaf, so current browse position is its parent
								case CurrentBrowseContext.ChangeBrowsePositionLeafFail:
									if ((currentBrowseContext.BrowseTypesRequested & (int)OPCBROWSETYPE.OPC_LEAF) != 0)
									{
										if (OpcBrowser.BrowserType == ServerType.OPC_DA205_Wrapper)
										{
											// If an OPC DA browse down failed on the leaf of the starting path, 
											// PrepareToBrowse will return ChangeBrowsePositionLeafFail, and the parent 
											// of the leaf of the starting path will be the current browse position.
											// In this case call BrowseLeaves to allow the the custom DA Properties of 
											// the starting object to be returned in the output list of ObjectAttributes. 
											if (currentBrowseContext.SomethingToReturn == false)
												currentBrowseContext.SomethingToReturn = OpcBrowser.BrowseLeaves();
											else
												OpcBrowser.BrowseLeaves();
										}
									}
									break;

								default: // failed to browse to the starting path
									break;
							}
							if (currentBrowseContext.SomethingToReturn == true)
								listOfObjAttrs = GetEnumeratedBrowseResults(numberToReturn, listOfObjAttrs);
						}
					}
				}
			}
			return listOfObjAttrs;
		}

		/// <summary>
		/// This method implements the server-specific behavior of the corresponding 
		/// Xi interface method.  It overrides its virtual method in the ContextBase 
		/// class of the ServerBase project.
		/// </summary>
		/// <param name="findCriteria">
		/// The criteria used by the server to find types.  If this 
		/// parameter is null, then this call is a continuation of the 
		/// previous find.
		/// </param>
		/// <param name="numberToReturn">
		/// The maximum number of objects to return in a single response.
		/// </param>
		/// <returns>
		/// The list of requested type attributes.
		/// </returns>
		public override List<TypeAttributes> OnFindTypes(FindCriteria findCriteria, uint numberToReturn)
		{
			// TODO: Add code if the FindTypes method is supported
			//lock (ContextBrowseLock)
			//{
			//}
			return null;
		}

		/// <summary>
		/// This method implements the server-specific behavior of the corresponding 
		/// Xi interface method.  It overrides its virtual method in the ContextBase 
		/// class of the ServerBase project.
		/// </summary>
		/// <param name="objectPath">
		/// The root path that identifies the object for which alternate 
		/// root paths are being requested. 
		/// </param>
		/// <returns>
		/// The list of additional root paths to the specified object.  
		/// Null if specified objectPath is the only root path to the 
		/// object. An exception is thrown if the specified objectPath is 
		/// invalid.  
		/// </returns>
		public override List<ObjectPath> OnFindRootPaths(ObjectPath objectPath)
		{
			// TODO: Add code if the FindRootPaths method is supported
			//lock (ContextBrowseLock)
			//{
			//}
			return null;
		}
	}
}
