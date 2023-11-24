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
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xi.Common.Support;
using Xi.Common.Support.Extensions;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.OPC.COM.API;
using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// The Data List class is used to represent a list of current process data values.  
	/// The data values held by this list represents current process values with a status 
	/// and a time stamp.
	/// </summary>
	public class DataList
		: DataListBase
	{
		/// <summary>
		/// This constructor is used to create a Data List and creates a corresponding OPC DA Group.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="clientId"></param>
		/// <param name="updateRate"></param>
		/// <param name="bufferingRate"></param>
		/// <param name="listType"></param>
		/// <param name="listKey"></param>
		internal DataList(ContextImpl context, uint clientId, uint updateRate, uint bufferingRate,
			uint listType, uint listKey, FilterSet filterSet, StandardMib mib)
			: base(context, clientId, updateRate, bufferingRate, listType, listKey, mib)
		{
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			StringBuilder sb = new StringBuilder("ContextId = ");
			sb.Append(context.Id);
			sb.Append(" & ListId = ");
			sb.Append(listKey);
			string grpName = sb.ToString();

			Nullable<float> fPercentDeadband = null;
			if (filterSet != null && filterSet.Filters != null && filterSet.Filters.Count > 0)
			{
				ParseDataFilters(filterSet, out fPercentDeadband);
			}

			uint dwLCID = (uint)context.LocaleId;
			uint revisedUpdateRate = 0;
			IOPCItemMgtCli iOPCItemMgt = null;
			cliHRESULT HR = context.IOPCServer.AddGroup(
				grpName, false, updateRate, listKey, null, fPercentDeadband, dwLCID,
				out revisedUpdateRate, out iOPCItemMgt);
			if (HR.Succeeded)
			{
				UpdateRate = revisedUpdateRate;
				IOPCItemMgt = iOPCItemMgt;
			}
			else
			{
				(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(HR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
				// The next line will not be executed if the call above throws
				throw FaultHelpers.Create((uint)HR.hResult, "OPC DA Add Group Failed!");
			}
		}

		private IOPCItemMgtCli IOPCItemMgt { get; set; }
		private IOPCGroupStateMgtCli IOPCGroupStateMgt
		{
			get { return IOPCItemMgt as IOPCGroupStateMgtCli; }
		}
		private IOPCSyncIOCli IOPCSyncIO
		{
			get { return IOPCItemMgt as IOPCSyncIOCli; }
		}
		private IOPCAsyncIO2Cli IOPCAsyncIO2
		{
			get { return IOPCItemMgt as IOPCAsyncIO2Cli; }
		}

		private List<ORedFilters> AdditionalNonDAFilters; // Non OPC DA Filters supported by this server


		protected override uint OnNegotiateBufferingRate(uint requestedBufferingRate)
		{
			uint negotiatedBufferingRate = 0;
			// TODO: Negotiate Buffering Rate if Buffering Rate is supported
			//       Also add code to implement buffering rate.
			return negotiatedBufferingRate;
		}

		/// <summary>
		/// Invoke the Dispose to make sure all clean up is done.
		/// </summary>
		/// <param name="isDisposing"></param>
		/// <returns></returns>
		protected override bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			if (isDisposing)
			{
				lock (_QueueOfChangedValuesLock)
				{
					_queueOfChangedValues.Clear();
				}
				if (0 < this.Count)
				{
					lock (_DictionaryIntegrityLock)
					{
						this.Clear();
					}
				}
			}

			lock (_ListTransactionLock)
			{
				if (isDisposing)
				{
					(IOPCItemMgt as IDisposable).Dispose();
				}
				_queueOfChangedValues = null;
				IOPCItemMgt = null;

				_hasBeenDisposed = true;
			}
			return true;
		}

		/// <summary>
		/// This method is invoked to Add Items to the OPC DA group 
		/// associated with this Xi Data List.
		/// </summary>
		/// <param name="dataListEntries"></param>
		/// <returns></returns>
		protected override List<AddDataObjectResult> OnAddDataObjectsToList(
			List<ValueRoot> dataListEntries)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			lock (_ListTransactionLock)
			{
				List<AddDataObjectResult> resultsList = new List<AddDataObjectResult>();
				List<OPCITEMDEF> listOpcItemDef = new List<OPCITEMDEF>();
				foreach (var dle in dataListEntries)
				{
					OPCITEMDEF itemDef = new OPCITEMDEF()
					{
						bActive = false,
						hClient = dle.ServerAlias,
						sAccessPath = null,
						sItemID = dle.InstanceId.LocalId,
						vtRequestedDataType = ((dle.ListElementOptions == ListElementOptions.AccessAsString) 
											? (ushort)cliVARENUM.VT_BSTR 
											: (ushort)cliVARENUM.VT_EMPTY)
					};
					listOpcItemDef.Add(itemDef);
				}
				List<OPCITEMRESULT> itemResults = null;
				cliHRESULT HR = IOPCItemMgt.AddItems(listOpcItemDef, out itemResults);
				if (HR.Succeeded)
				{
					IEnumerator<ValueRoot> iDLV = dataListEntries.GetEnumerator();
					foreach (OPCITEMRESULT ir in itemResults)
					{
						iDLV.MoveNext();
						DataListValue dlv = iDLV.Current as DataListValue;
						dlv.hServer = ir.hServer;
						dlv.OpcDataType = ir.vtCanonicalDataType;
						dlv.OpcAccessRights = ir.dwAccessRights;
						AddDataObjectResult result = new AddDataObjectResult(
							(uint)ir.hResult, dlv.ClientAlias, dlv.ServerAlias,
							new TypeId(cliVARIANT.CliTypeFrom(ir.vtCanonicalDataType)),
                            true, true // Unisim returns wrong flags, so this check is disabled
                            );
                            //((ir.dwAccessRights & (int)OpcDaAccessRights.Readable) != 0),
                            //((ir.dwAccessRights & (int)OpcDaAccessRights.Writable) != 0));
						resultsList.Add(result);
					}
				}
				else
				{
					(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(HR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)HR.hResult, "OPC DA Add Items Failed!");
				}
				return resultsList;
			}
		}

		/// <summary>
		/// This method is used to obtain an instance of a DataListValue which 
		/// is the class used by the OPC DA wrapper to cache a data value.
		/// </summary>
		/// <param name="clientAlias"></param>
		/// <param name="serverAlias"></param>
		/// <param name="instanceId"></param>
		/// <returns></returns>
		protected override ValueRoot OnNewDataListValue(
			uint clientAlias, uint serverAlias, InstanceId instanceId)
		{
			if (   (clientAlias == 0)
				|| (serverAlias == 0)
				|| (instanceId == null)
				|| (instanceId.IsValid() == false)
			   )
			{
				return null;
			}
			else
			{
				return new DataListValue(clientAlias, serverAlias)
				{
					InstanceId = instanceId,
				};
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="listAliasResult">List of errors passed in by calling method, 
		/// and that are to be included in the list returned by this method.</param>
		/// <param name="dataListEntries">The list of data objects to be removed from the list. 
		/// When this value is null all elements of the list are to be removed.</param>
		/// <returns>List of errors that includes those passed in by calling method</returns>
		protected override List<AliasResult> OnRemoveDataObjectsFromList(
			List<AliasResult> listAliasResult, List<ValueRoot> dataListEntries)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			lock (_ListTransactionLock)
			{
				List<uint> listHServer = new List<uint>();
				foreach (var dle in dataListEntries)
				{
					if (dle is DataListValue)
					{
						DataListValue dlv = dle as DataListValue;
						listHServer.Add(dlv.hServer);
					}
					else
					{
						listAliasResult.Add(new AliasResult(
							XiFaultCodes.E_INCONSISTENTUSEAGE, dle.ClientAlias, dle.ServerAlias));
					}
				}
				List<HandleAndHRESULT> errList = null;
				cliHRESULT HR = IOPCItemMgt.RemoveItems(listHServer, out errList);
				if (HR.Succeeded)
				{
					if ((HR.IsS_FALSE) && (errList != null))
					{
						foreach (HandleAndHRESULT hhr in errList)
						{
							if (hhr.hResult.Failed)
							{
								ValueRoot vr = dataListEntries.Find(dle => dle.ClientAlias == hhr.Handle);
								AliasResult aliasResult = (vr == null)
														? new AliasResult((uint)hhr.hResult, hhr.Handle, 0)
														: new AliasResult((uint)hhr.hResult, hhr.Handle, vr.ServerAlias);

								if (listAliasResult == null)
									listAliasResult = new List<AliasResult>();
								listAliasResult.Add(aliasResult);
							}
						}
					}
				}
				else
				{
					(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(HR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)HR.hResult, "OPC DA Remove Items Failed!");
				}
				return listAliasResult;
			}
		}

		/// <summary>
		/// This method is used to change the filters of a list.  The 
		/// new filters replace the old filters if they exist.
		/// </summary>
		/// <param name="updateRate">
		/// List update or scan rate.  The server will negotiate this rate to one 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the update rate is not to be updated.  
		/// </param>
		/// <param name="bufferingRate">
		/// List buffering rate.  The server will negotiate this rate to one 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the buffering rate is not to be updated.
		/// </param>
		/// <param name="filterSet">
		/// The new set of filters.  The server will negotiate these filters to those 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the filters are not to be updated.
		/// </param>
		/// <returns>
		/// The revised update rate, buffering rate, and filter set.  Attributes 
		/// that were not updated will be null in this response.
		/// </returns>
		public override ModifyListAttrsResult OnModifyListAttributes(
			Nullable<uint> updateRate, Nullable<uint> bufferingRate, FilterSet filterSet)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			lock (_ListTransactionLock)
			{
				// Call IOPCGroupStateMgt.GetState to get the parameters below that have to be passed back in 
				// when setting the state to modify the update rate for the Group
				uint dwUpdateRate = 0;
				bool bActive = false;
				string sName = null;
				int dwTimeBias = 0;
				float fPercentDeadband = 0;
				uint dwLCID = 0;
				uint hClientGroup = 0;
				uint hServerGroup = 0;
				uint dwRevUpdateRate = 0;
				cliHRESULT getHR = IOPCGroupStateMgt.GetState(out dwUpdateRate, out bActive, out sName,
					out dwTimeBias, out fPercentDeadband, out dwLCID, out hClientGroup, out hServerGroup);
				if (getHR.Succeeded)
				{
					dwRevUpdateRate = dwUpdateRate;
					// At this time only the update rate may be modified 
					// so make sure a new value is present and only do
					// the change if the new update rate is present different
					if (null != updateRate && dwUpdateRate != (uint)updateRate)
					{
						dwUpdateRate = (uint)updateRate;
						// now call SetState to enable/disable the Group
						cliHRESULT setHR = IOPCGroupStateMgt.SetState(dwUpdateRate, out dwRevUpdateRate,
							bActive, dwTimeBias, fPercentDeadband, dwLCID, hClientGroup);
						if (setHR.Failed)
						{
							(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(setHR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
							// The next line will not be executed if the call above throws
							FaultHelpers.Create((uint)setHR, "Failed to set OPC Group State!");
						}
					}
				}
				else
				{
					(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(getHR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)getHR, "Failed to get OPC Group State");
				}

				UpdateRate = dwRevUpdateRate;

				if (bufferingRate != null)
				{
					BufferingRate = NegotiateBufferingRate(XiOPCWrapper.StandardMib, (uint)bufferingRate);
					bufferingRate = BufferingRate;
				}

				ModifyListAttrsResult result = new ModifyListAttrsResult()
				{
					RevisedUpdateRate = dwRevUpdateRate,
					RevisedFilterSet = null,
				};
				return result;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="enableUpdating"></param>
		public override ListAttributes OnEnableListUpdating(bool enableUpdating)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			lock (_ListTransactionLock)
			{
				// Call IOPCGroupStateMgt.GetState to get the parameters below that 
				// have to be passed back in when setting the state to change the 
				// disable/enable state of the Group
				uint dwUpdateRate = 0;
				bool bActive = !enableUpdating;
				string sName = null;
				int dwTimeBias = 0;
				float fPercentDeadband = 0;
				uint dwLCID = 0;
				uint hClientGroup = 0;
				uint hServerGroup = 0;
				cliHRESULT getHR = IOPCGroupStateMgt.GetState(out dwUpdateRate, out bActive, out sName,
					out dwTimeBias, out fPercentDeadband, out dwLCID, out hClientGroup, out hServerGroup);
				// Only change the state if the state was obtained 
				// and the new active state is different
				if (getHR.Succeeded)
				{
					if (bActive != enableUpdating)
					{
						// now call SetState to enable/disable the Group
						uint dwRevUpdateRate = 0;
						cliHRESULT setHR = IOPCGroupStateMgt.SetState(dwUpdateRate, out dwRevUpdateRate,
							enableUpdating, dwTimeBias, fPercentDeadband, dwLCID, hClientGroup);
						if (setHR.Failed)
						{
							(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(setHR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
							// The next line will not be executed if the call above throws
							FaultHelpers.Create((uint)setHR, "Failed to set OPC Group State!");
						}
					}
				}
				else
				{
					(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(getHR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)getHR, "Failed to get OPC Group State");
				}

				Enabled = enableUpdating;
				return ListAttributes;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="enableUpdating"></param>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public override List<AliasResult> OnEnableListElementUpdating(
			bool enableUpdating, List<uint> serverAliases)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			lock (_ListTransactionLock)
			{
				List<AliasResult> listAliasResult = new List<AliasResult>();

				List<uint> listHServer = null;
				if (serverAliases == null) // use all the elements in the list
					serverAliases = GetServerAliases();
				if (serverAliases != null)
				{
					listHServer = new List<uint>(serverAliases.Count);
					for (int idx = 0; idx < serverAliases.Count; idx++)
					{
						ValueRoot valueRoot = null;
						// make sure each server alias is in the List
						if (serverAliases[idx] == 0)
						{
							// if the server alias is 0, use its index into the serverAliases list as the client index
							listAliasResult.Add(new AliasResult(XiFaultCodes.E_ALIASNOTFOUND, (uint)idx, 0));
						}
						else
						{
							bool bValueFound = false;
							lock (_DictionaryIntegrityLock)
							{
								bValueFound = this.TryGetValue(serverAliases[idx], out valueRoot);
							}
							if ((bValueFound) && (valueRoot != null))
							{
								// make sure each server alias is for a data object
								if (valueRoot is DataListValue)
								{
									// if so, add it to the list to pass to the server
									listHServer.Add((valueRoot as DataListValue).hServer);
									(valueRoot as DataListValue).UpdatingEnabled = enableUpdating;
								}
								else
								{
									listAliasResult.Add(new AliasResult(XiFaultCodes.E_INCONSISTENTUSEAGE,
										valueRoot.ClientAlias, serverAliases[idx]));
								}
							}
							else
							{
								listAliasResult.Add(new AliasResult(XiFaultCodes.E_ALIASNOTFOUND,
									0, serverAliases[idx])); // server alias is not zero, but was not found; so there is no client alias
							}
						}
					}
				}

				if ((listHServer != null) && (listHServer.Count > 0))
				{
					List<HandleAndHRESULT> errList = null;
					cliHRESULT HR = IOPCItemMgt.SetActiveState(listHServer, enableUpdating, out errList);
					if (HR.Succeeded)
					{
						if ((HR.IsS_FALSE) && (errList != null))
						{
							foreach (HandleAndHRESULT hdlAndHr in errList)
							{
								if (XiFaultCodes.S_OK != hdlAndHr.hResult)
								{
									// this should always succeed since this object was obtained in the TryGetValue above
									ValueRoot vr = FindEntryRoot(hdlAndHr.Handle);
									if (vr != null)
									{
										if (vr is DataListValueBase)
										{
											DataListValueBase dataListValue = vr as DataListValueBase;
											listAliasResult.Add(new AliasResult(
												(uint)hdlAndHr.hResult, dataListValue.ClientAlias, hdlAndHr.Handle));
										}
									}
								}
							}
						}
					}
					else
					{
						(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(HR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
						// The next line will not be executed if the call above throws
						FaultHelpers.Create((uint)HR, "Failed to set OPC Active State for Data Values!");
					}
				}
				return listAliasResult;
			}
		}

		private uint dwCancelID_Refresh = 0;
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override uint OnTouchList()
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			lock (_ListTransactionLock)
			{
				uint dwTransactionID = (uint)_rand.Next(0x40000000, 0x7FFFFFFF);
				cliHRESULT HR = IOPCAsyncIO2.Refresh2(OPCDATASOURCE.OPC_DS_DEVICE,
					dwTransactionID, out dwCancelID_Refresh);
				if (HR.Succeeded == false)
				{
					(OwnerContext as ContextImpl).ThrowOnDisconnectedServer(HR.hResult, (OwnerContext as ContextImpl).IOPCServer_ProgId);
				}
				return (uint)HR.hResult;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public override List<AliasResult> OnTouchDataObjects(List<uint> serverAliases)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			lock (_ListTransactionLock)
			{
				return OnAsyncReadData(serverAliases);
			}
		}

		/// <summary>
		/// This method is invoked to perform a OPC DA server synchronous read.
		/// </summary>
		/// <param name="serverAliases">
		/// It is expected that this method will update both the Xi Servers cached 
		/// data value and the data value to be returned for this read request.
		/// </param>
		public override DataValueArraysWithAlias OnReadData(
			List<uint> serverAliases)
		{
			if (null == _iReadEndpointEntry)
				throw FaultHelpers.Create(XiFaultCodes.E_LISTNOTATTACHEDTOENDPOINT, "List not attached to the IRead endpoint.");
			if (!Enabled)
				throw FaultHelpers.Create(XiFaultCodes.E_LISTDISABLED, "List not Enabled.");

			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			DataValueArraysWithAlias valueArrays = null;
			lock (_ListTransactionLock)
			{
				List<ErrorInfo> errors = new List<ErrorInfo>();

				List<uint> listHServer = null;
				if (null == serverAliases)
					serverAliases = this.GetServerAliases();
				if (null != serverAliases)
				{
					listHServer = new List<uint>(serverAliases.Count);
					for (int idx = 0; idx < serverAliases.Count; idx++)
					{
						ValueRoot valueRoot = null;
						// make sure each server alias is in the List
						if (serverAliases[idx] == 0)
						{
							// if the server alias is 0, use its index into the serverAliases list as the client index
							errors.Add(new ErrorInfo(XiFaultCodes.E_ALIASNOTFOUND, (uint)idx, 0));
						}
						else
						{
							bool bValueReturned = false;
							lock (_DictionaryIntegrityLock)
							{
								bValueReturned = this.TryGetValue(serverAliases[idx], out valueRoot);
							}
							if ((bValueReturned) && (valueRoot != null))
							{
								// make sure each server alias is for a data object
								if (valueRoot is DataListValue)
								{
									// if so, add it to the list to pass to the server
									listHServer.Add((valueRoot as DataListValue).hServer);
								}
								else
								{
									errors.Add(new ErrorInfo(XiFaultCodes.E_INCONSISTENTUSEAGE,
										valueRoot.ClientAlias, valueRoot.ServerAlias));
								}
							}
							else
							{
								errors.Add(new ErrorInfo(XiFaultCodes.E_ALIASNOTFOUND,
									0, serverAliases[idx])); // server alias is not zero, but was not found; so there is no client alias
							}
						}
					}
				}

				List<ValueRoot> valuesToQueue = new List<ValueRoot>();

				if ((listHServer != null) && (listHServer.Count > 0))
				{
					cliHRESULT HR = IOPCSyncIO.Read(OPCDATASOURCE.OPC_DS_CACHE, listHServer, out valueArrays);
					if ((HR.Succeeded) && (valueArrays != null))
					{
						if (valueArrays.HasDataValues() && valueArrays.TotalValues() == listHServer.Count)
						{
							if (valueArrays.HasDoubleValues())
							{
								for (int idx = 0; idx < valueArrays.DoubleAlias.Length; idx++)
								{
									ValueRoot entryRoot = null;
									if (this.TryGetValue(
										(valueArrays.DoubleAlias[idx]), out entryRoot))
									{
										DataListValue dataListValue = entryRoot as DataListValue;
										if (null != dataListValue)
										{
											valueArrays.DoubleAlias[idx] = dataListValue.ClientAlias;
											if (PollingActivated)
											{
												cliHRESULT HR1 = dataListValue.OnDataChangeDouble(
													valueArrays.DoubleStatusCodes[idx],
													valueArrays.DoubleTimeStamps[idx],
													valueArrays.DoubleValues[idx]);
												if (HR.IsS_OK)
												{
													HR = HR1;
													valuesToQueue.Add(dataListValue);
												}
											}
										}
									}
								}
							}
							if (valueArrays.HasUintValues())
							{
								for (int idx = 0; idx < valueArrays.UintAlias.Length; idx++)
								{
									ValueRoot entryRoot = null;
									if (this.TryGetValue(
										(valueArrays.UintAlias[idx]), out entryRoot))
									{
										DataListValue dataListValue = entryRoot as DataListValue;
										if (null != dataListValue)
										{
											valueArrays.UintAlias[idx] = dataListValue.ClientAlias;
											if (PollingActivated)
											{
												cliHRESULT HR1 = dataListValue.OnDataChangeUint(
													valueArrays.UintStatusCodes[idx],
													valueArrays.UintTimeStamps[idx],
													valueArrays.UintValues[idx]);
												if (HR.IsS_OK)
												{
													HR = HR1;
													valuesToQueue.Add(dataListValue);
												}
											}
										}
									}
								}
							}
							if (valueArrays.HasObjectValues())
							{
								for (int idx = 0; idx < valueArrays.ObjectAlias.Length; idx++)
								{
									ValueRoot entryRoot = null;
									if (this.TryGetValue(
										(valueArrays.ObjectAlias[idx]), out entryRoot))
									{
										DataListValue dataListValue = entryRoot as DataListValue;
										if (null != dataListValue)
										{
											valueArrays.ObjectAlias[idx] = dataListValue.ClientAlias;
											if (PollingActivated)
											{
												cliHRESULT HR1 = dataListValue.OnDataChangeObject(
													valueArrays.ObjectStatusCodes[idx],
													valueArrays.ObjectTimeStamps[idx],
													valueArrays.ObjectValues[idx]);
												if (HR.IsS_OK)
												{
													HR = HR1;
													valuesToQueue.Add(dataListValue);
												}
											}
										}
									}
								}
							}
							QueueChangedValues(valuesToQueue);
						}
						else
						{
							throw FaultHelpers.Create("OPC DA Sync Read failed to return the requested number of responses");
						}
					}
					else
					{
						context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCServer_ProgId);
						// The next line will not be executed if the call above throws
						throw FaultHelpers.Create((uint)HR.hResult, "OPC DA Sync Read failed");
					}
				}
				if (0 < errors.Count)
				{
					if (null == valueArrays)
						valueArrays = new DataValueArraysWithAlias(0, 0, 0);
					if (null == valueArrays.ErrorInfo || 0 == valueArrays.ErrorInfo.Count)
						valueArrays.ErrorInfo = errors;
					else
						foreach (ErrorInfo errorInfo in errors)
							valueArrays.ErrorInfo.Add(errorInfo);
				}
			}
			return valueArrays;
		}

		private uint dwCancelID_AsyncRead = 0;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public List<AliasResult> OnAsyncReadData(List<uint> serverAliases)
		{
			ContextImpl context = OwnerContext as ContextImpl;

			List<AliasResult> listAliasResult = new List<AliasResult>();

			List<uint> listHServer = null;
			if (serverAliases == null)
			{
				listHServer = new List<uint>(this.Count);
				lock (_DictionaryIntegrityLock)
				{
					foreach (var kvp in this) // kvp = key value pair
					{
						if (kvp.Value is DataListValue)
						{
							listHServer.Add((kvp.Value as DataListValue).hServer);
						}
						else
						{
							listAliasResult.Add(new AliasResult(XiFaultCodes.E_INCONSISTENTUSEAGE,
								kvp.Value.ClientAlias, kvp.Value.ServerAlias));
						}
					}
				}
			}
			else
			{
				listHServer = new List<uint>(serverAliases.Count);
				for (int idx = 0; idx < serverAliases.Count; idx++)
				{
					ValueRoot valueRoot = null;
					// make sure each server alias is in the List
					if (serverAliases[idx] == 0)
					{
						// if the server alias is 0, use its index into the serverAliases list as the client index
						listAliasResult.Add(new AliasResult(XiFaultCodes.E_ALIASNOTFOUND, (uint)idx, 0));
					}
					else
					{
						bool bValueReturned = false;
						lock (_DictionaryIntegrityLock)
						{
							bValueReturned = this.TryGetValue(serverAliases[idx], out valueRoot);
						}
						if ((bValueReturned) && (valueRoot != null))
						{
							// make sure each server alias is for a data object
							if (valueRoot is DataListValue)
							{
								// if so, add it to the list to pass to the server
								listHServer.Add((valueRoot as DataListValue).hServer);
							}
							else
							{
								listAliasResult.Add(new AliasResult(XiFaultCodes.E_INCONSISTENTUSEAGE,
									valueRoot.ClientAlias, serverAliases[idx]));
							}
						}
						else
						{
							// server alias is not zero, but was not found; so there is no client alias
							listAliasResult.Add(new AliasResult(XiFaultCodes.E_ALIASNOTFOUND, 0, serverAliases[idx]));
						}
					}
				}
			}

			if ((listHServer != null) && (listHServer.Count > 0))
			{
				List<HandleAndHRESULT> errList = null;
				uint dwTransactionID = (uint)_rand.Next(0x40000000, 0x7FFFFFFF);
				cliHRESULT HR = IOPCAsyncIO2.Read(listHServer,
					dwTransactionID, out dwCancelID_AsyncRead, out errList);
				if (HR.Succeeded)
				{
					if ((HR.IsS_FALSE) && (errList != null))
					{
						foreach (var HdlAndRes in errList)
						{
							listAliasResult.Add(new AliasResult(
								(uint)HdlAndRes.hResult, HdlAndRes.Handle, 0));
						}
					}
				}
				else
				{
					context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCServer_ProgId);
				}
			}

			return (listAliasResult.Count == 0) ? null : listAliasResult;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writeValueArrays"></param>
		/// <returns></returns>
		public override List<AliasResult> OnWriteValues(WriteValueArrays writeValueArrays)
		{
			if (null == _iWriteEndpointEntry)
				throw FaultHelpers.Create(XiFaultCodes.E_LISTNOTATTACHEDTOENDPOINT, "List not attached to the IWrite endpoint.");

            var context = (ContextImpl)OwnerContext;
			if (context.IsAccessibleDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCServer_ProgId);

			var opcSvrClientHandleToXiServerClientAlias = new Dictionary<uint, DataListValue>();
			var listAliasResult = new List<AliasResult>();

		    var changedValuesListForEvents = new Dictionary<DataListValue, object>();

			lock (_ListTransactionLock)
			{
				bool bValueReturned = false;
				if (null != writeValueArrays.DoubleServerAlias)
				{
					for (int idx = 0; idx < writeValueArrays.DoubleServerAlias.Length; idx++)
					{
						ValueRoot valueRoot = null;
						lock (_DictionaryIntegrityLock)
						{
							bValueReturned = this.TryGetValue(writeValueArrays.DoubleServerAlias[idx], out valueRoot);
						}
						if ((bValueReturned) && (valueRoot != null))
						{
							Debug.Assert(valueRoot is DataListValue);

						    var dataListValue = valueRoot as DataListValue;

							if (dataListValue != null)
							{
								// if so, add it to the list to pass to the server
                                writeValueArrays.DoubleServerAlias[idx] = dataListValue.hServer;
								opcSvrClientHandleToXiServerClientAlias.Add(
                                    dataListValue.hClient,
                                    dataListValue);

                                changedValuesListForEvents.Add(dataListValue, writeValueArrays.DoubleValues[idx]);
							}
							else
							{
								listAliasResult.Add(new AliasResult(XiFaultCodes.E_INCONSISTENTUSEAGE,
									valueRoot.ClientAlias, writeValueArrays.DoubleServerAlias[idx]));
							}
						}
						else
						{
							listAliasResult.Add(new AliasResult(XiFaultCodes.E_ALIASNOTFOUND,
								0, writeValueArrays.DoubleServerAlias[idx]));
						}
					}
				}
				if (null != writeValueArrays.UintServerAlias)
				{
					for (int idx = 0; idx < writeValueArrays.UintServerAlias.Length; idx++)
					{
						ValueRoot valueRoot = null;
						lock (_DictionaryIntegrityLock)
						{
							bValueReturned = this.TryGetValue(writeValueArrays.UintServerAlias[idx], out valueRoot);
						}
						if ((bValueReturned) && (valueRoot != null))
						{
							Debug.Assert(valueRoot is DataListValue);

                            var dataListValue = valueRoot as DataListValue;
                            if (dataListValue != null)
							{
								// if so, add it to the list to pass to the server
                                writeValueArrays.UintServerAlias[idx] = dataListValue.hServer;
								opcSvrClientHandleToXiServerClientAlias.Add(
                                    dataListValue.hClient,
                                    dataListValue);
                            
                                changedValuesListForEvents.Add(dataListValue, writeValueArrays.UintValues[idx]);
                            }
							else
							{
								listAliasResult.Add(new AliasResult(XiFaultCodes.E_INCONSISTENTUSEAGE,
									valueRoot.ClientAlias, writeValueArrays.UintServerAlias[idx]));
							}
						}
						else
						{
							listAliasResult.Add(new AliasResult(XiFaultCodes.E_ALIASNOTFOUND,
								0, writeValueArrays.UintServerAlias[idx]));
						}
					}
				}
				if (null != writeValueArrays.ObjectServerAlias)
				{
					for (int idx = 0; idx < writeValueArrays.ObjectServerAlias.Length; idx++)
					{
						ValueRoot valueRoot = null;
						lock (_DictionaryIntegrityLock)
						{
							bValueReturned = this.TryGetValue(writeValueArrays.ObjectServerAlias[idx], out valueRoot);
						}
						if ((bValueReturned) && (valueRoot != null))
						{
							Debug.Assert(valueRoot is DataListValue);

                            var dataListValue = valueRoot as DataListValue;

                            if (dataListValue != null)
							{
								// if so, add it to the list to pass to the server
								writeValueArrays.ObjectServerAlias[idx] = (valueRoot as DataListValue).hServer;
								opcSvrClientHandleToXiServerClientAlias.Add(
                                    dataListValue.hClient,
                                    dataListValue);

                                changedValuesListForEvents.Add(dataListValue, writeValueArrays.ObjectValues[idx]);

							}
							else
							{
								listAliasResult.Add(new AliasResult(XiFaultCodes.E_INCONSISTENTUSEAGE,
									valueRoot.ClientAlias, writeValueArrays.ObjectServerAlias[idx]));
							}
						}
						else
						{
							listAliasResult.Add(new AliasResult(XiFaultCodes.E_ALIASNOTFOUND,
								0, writeValueArrays.ObjectServerAlias[idx]));
						}
					}
				}

				List<HandleAndHRESULT> listHdlAndHR = null;
				cliHRESULT HR = IOPCSyncIO.Write(writeValueArrays, out listHdlAndHR);
				if (HR.Succeeded)
				{
                    foreach (var item in opcSvrClientHandleToXiServerClientAlias)
                    {
                        object changedValue;
                        if (changedValuesListForEvents.TryGetValue(item.Value, out changedValue))
                        {
                            lock (_usoGlobalsLock)
                            {
#if USO
                                // We can use UserID from Identity
                                // context.Identity.Name
                                // Or can take identification data from context
                                // context.ComputerAndUserId
                                // Or can tage separatly 
                                // context.WorkstationName + context.UserId
                                UsoClient.LogOperatorAction(context.SimExec, context.ComputerAndUserId, item.Value.InstanceId.LocalId, changedValue);
#endif
								((ContextImpl)this.OwnerContext).NotifyClientsAboutChanges(
                                    context.ComputerAndUserId,
                                    item.Value.InstanceId, changedValue);
                            }
                        }
                    }

				    if (HR.IsS_FALSE && null != listHdlAndHR)
					{
						foreach (var hdlHR in listHdlAndHR)
						{
							DataListValue dataListValue = null;
							opcSvrClientHandleToXiServerClientAlias.TryGetValue((uint)hdlHR.Handle, out dataListValue);
							AliasResult aliasResult = new AliasResult((uint)hdlHR.hResult,
								dataListValue.ClientAlias, dataListValue.ServerAlias);
							listAliasResult.Add(aliasResult);
						}
					}
				}
				else
				{
					context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCServer_ProgId);
				}

				opcSvrClientHandleToXiServerClientAlias.Clear();
				return listAliasResult;
			}
		}

        private static object _usoGlobalsLock = new object();

	    public override List<AliasResult> OnWriteVST(DataValueArraysWithAlias readValueArrays)
	    {
	        var arrays = new WriteValueArrays(
                readValueArrays.DoubleValues != null ? readValueArrays.DoubleValues.Length : 0,
                readValueArrays.UintValues != null ? readValueArrays.UintValues.Length : 0,
                readValueArrays.ObjectValues != null ? readValueArrays.ObjectValues.Length : 0);

            if (readValueArrays.DoubleValues != null)
                for (var idx = 0; idx < readValueArrays.DoubleValues.Length; ++idx)
                {
                    arrays.DoubleValues[idx] = readValueArrays.DoubleValues[idx];
                    arrays.DoubleServerAlias[idx] = readValueArrays.DoubleAlias[idx];
                }

	        if (readValueArrays.UintValues != null)
	            for (var idx = 0; idx < readValueArrays.UintValues.Length; ++idx)
	            {
	                arrays.UintValues[idx] = readValueArrays.UintValues[idx];
                    arrays.UintServerAlias[idx] = readValueArrays.UintAlias[idx];
                }

	        if (readValueArrays.ObjectValues != null)
	            for (var idx = 0; idx < readValueArrays.ObjectValues.Length; ++idx)
	            {
	                arrays.ObjectValues[idx] = readValueArrays.ObjectValues[idx];
	                arrays.ObjectServerAlias[idx] = readValueArrays.ObjectAlias[idx];
	            }

	        return OnWriteValues(arrays);
	    }

	    /// <summary>
		/// This method will be invoked as a result of the OPC DA COM
		/// server issuing a call back to update data.
		/// </summary>
		/// <param name="hrMasterquality"></param>
		/// <param name="hrMastererror"></param>
		/// <param name="valueArrays"></param>
		/// <returns></returns>
		public cliHRESULT OnDataChange(cliHRESULT hrMasterquality, cliHRESULT hrMastererror,
			DataValueArraysWithAlias valueArrays)
		{
			cliHRESULT HR = new cliHRESULT(XiFaultCodes.S_OK);
			if ((Enabled) && (BeingDeleted == false))
			{
				if (valueArrays.HasDataValues())
				{
					// TODO: If BufferingRate is supported and callbacks are used, queue the callback data and issue the callbacks from the queue below at the buffering rate
					List<ValueRoot> valuesToQueue = new List<ValueRoot>();
					if (PollingActivated || CallbackActivated)
					{
						lock (_DictionaryIntegrityLock)
						{
							if (valueArrays.HasDoubleValues())
							{
								for (int idx = 0; idx < valueArrays.DoubleAlias.Length; idx++)
								{
									ValueRoot entryRoot = null;
									if (this.TryGetValue(
										(valueArrays.DoubleAlias[idx]), out entryRoot))
									{
										DataListValue dataListValue = entryRoot as DataListValue;
										if (null != dataListValue)
										{
											valueArrays.DoubleAlias[idx] = dataListValue.ClientAlias;
											if (PollingActivated)
											{
												cliHRESULT HR1 = dataListValue.OnDataChangeDouble(
													valueArrays.DoubleStatusCodes[idx],
													valueArrays.DoubleTimeStamps[idx],
													valueArrays.DoubleValues[idx]);
												if (HR.IsS_OK)
												{
													HR = HR1;
													valuesToQueue.Add(dataListValue);
												}
											}
										}
									}
								}
							}
							if (valueArrays.HasUintValues())
							{
								for (int idx = 0; idx < valueArrays.UintAlias.Length; idx++)
								{
									ValueRoot entryRoot = null;
									if (this.TryGetValue(
										(valueArrays.UintAlias[idx]), out entryRoot))
									{
										DataListValue dataListValue = entryRoot as DataListValue;
										if (null != dataListValue)
										{
											valueArrays.UintAlias[idx] = dataListValue.ClientAlias;
											if (PollingActivated)
											{
												cliHRESULT HR1 = dataListValue.OnDataChangeUint(
													valueArrays.UintStatusCodes[idx],
													valueArrays.UintTimeStamps[idx],
													valueArrays.UintValues[idx]);
												if (HR.IsS_OK)
												{
													HR = HR1;
													valuesToQueue.Add(dataListValue);
												}
											}
										}
									}
								}
							}
							if (valueArrays.HasObjectValues())
							{
								for (int idx = 0; idx < valueArrays.ObjectAlias.Length; idx++)
								{
									ValueRoot entryRoot = null;
									if (this.TryGetValue(
										(valueArrays.ObjectAlias[idx]), out entryRoot))
									{
										DataListValue dataListValue = entryRoot as DataListValue;
										if (null != dataListValue)
										{
											valueArrays.ObjectAlias[idx] = dataListValue.ClientAlias;
											if (PollingActivated)
											{
												cliHRESULT HR1 = dataListValue.OnDataChangeObject(
													valueArrays.ObjectStatusCodes[idx],
													valueArrays.ObjectTimeStamps[idx],
													valueArrays.ObjectValues[idx]);
												if (HR.IsS_OK)
												{
													HR = HR1;
													valuesToQueue.Add(dataListValue);
												}
											}
										}
									}
								}
							}
						}
					}
					// add a queue marker as the last entry in the queue
					if ((PollingActivated) && (valuesToQueue.Count > 0))
						QueueChangedValues(valuesToQueue);
				}

				if (CallbackActivated)
					OnInformationReport(valueArrays);
			}
			return HR;
		}

		protected void ParseDataFilters(FilterSet filterSet, out Nullable<float> fPercentDeadband)
		{
			fPercentDeadband = null;
			if (   (filterSet == null)
				|| (filterSet.Filters == null)
				|| (filterSet.Filters.Count == 0))
			{
				return;
			}

			int percentDeadbandIdx = -1;  // identifies the OredFilter in the filterSet that contains the EventType filter

			AdditionalNonDAFilters = new List<ORedFilters>();

			for (int idx = 0; idx < filterSet.Filters.Count; idx++)
			{
				ORedFilters oredFilter = filterSet.Filters[idx];
				switch (oredFilter.FilterCriteria[0].OperandName)
				{
					case FilterOperandNames.PercentDeadband:
						if (percentDeadbandIdx != -1)
							throw FaultHelpers.Create("FilterSet contains ANDed PercentDeadbands");
						percentDeadbandIdx = idx;
						ParsePercentDeadbandFilterCriteria(oredFilter.FilterCriteria, ref fPercentDeadband);
						break;

					default:
						AdditionalNonDAFilters.Add(oredFilter);
						break;
				}
			}
			return;
		}

		protected void ParsePercentDeadbandFilterCriteria(List<FilterCriterion> filterCriteria, ref Nullable<float> fPercentDeadband)
		{
			if (filterCriteria.Count > 1)
				throw FaultHelpers.Create("Invalid ORed Percent Deadband in Filter Criteria");
			try
			{
				fPercentDeadband = (float)filterCriteria[0].ComparisonValue;
			}
			catch
			{
				throw FaultHelpers.Create("Invalid Percent Deadband Filter Comparison Value Type = "
					+ filterCriteria[0].ComparisonValue.GetType().ToString());
			}
		}
#if false
        static public void LogOperatorAction(SimExec se, string fieldViewDisplayName, string fullTagname, double newValue)
        {
            UsoGlobals.SimExec = se;

#if true
            ProcessControlEvent processControlEvent = new ProcessControlEvent();
            string parameter;
            string tagName;
            Utilities.SplitTagName(fullTagname, out tagName, out parameter);
            // Worked version of parameter initialization
            processControlEvent.SetAllProcessControlValues(tagName, 0, -1, fullTagname);
            //processControlEvent.SetAllProcessControlValues(fullTagname, 0, -1,"");
            // processControlEvent.SetAllProcessControlValues(fullTagname, 0, -1,""); - bad for ModuleName and good for other param
            processControlEvent.Value = newValue;
            //processControlEvent.DescripName = tagName;
            Console.WriteLine("Tag [" + tagName + "]" + (processControlEvent.ModuleId == 0 ? " not" : "") + " present , module num= " + processControlEvent.ModuleId.ToString());
#else
            ProcessControlEvent processControlEvent = new ProcessControlEvent(fullTagname, newValue, se.GetSimTime() );
            Console.WriteLine("Tag [" + fullTagname + "]" + (processControlEvent.ModuleId == 0?" not":"") + " present , module num= " + processControlEvent.ModuleId.ToString() );
#endif
            // processControlEvent.Description = fullTagname + " Changed" + /*"From " + oldValue.ToString() +  */ " To " + newValue.ToString();


            if (null != se)
            {

                //string fieldViewSourceName = string.Empty;	//The name that uniquely identifies this fieldView display (i.e. RemoteDisplay1)
                processControlEvent.ConsoleName = fieldViewDisplayName;
                processControlEvent.Console = EventConsole.Remote;

                //processControlEvent.Playable = true;                // not necessary
                //processControlEvent.PlayMessage = "Some message";   // not necessary
                //processControlEvent.OldValue = 0.5;                 // not necessary
                //processControlEvent.IsScenarioReplay = true;        // not necessary

                Console.WriteLine("Generate process control event: " + processControlEvent.Description + " from console: " + fieldViewDisplayName + " for session: " + se.Simulation.Name);

#if false
                se.AsyncLogEvent(processControlEvent);
#else
                processControlEvent.Publish(false);
#endif
            }
            else
            {
                Console.WriteLine("Error: SimExe is not defined!");
            }
        }
#endif

    }

}
