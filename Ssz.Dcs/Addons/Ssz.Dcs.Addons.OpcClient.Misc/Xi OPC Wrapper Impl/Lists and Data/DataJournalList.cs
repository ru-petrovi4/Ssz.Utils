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

using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.OPC.COM.API;
using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// This internal class is used to take a Xi Filter Criterion 
	/// and convert it into an OPC HDA Time.
	/// </summary>
	internal static class OpcHdaTime
	{
		public static OPCHDA_TIME OpcHdaTimeFromTimeFilter(FilterCriterion timestampFilter)
		{
			if (0 == string.Compare(timestampFilter.OperandName, FilterOperandNames.Timestamp, true))
			{
				if (timestampFilter.ComparisonValue is DateTime)
				{
					return new OPCHDA_TIME((DateTime)timestampFilter.ComparisonValue);
				}
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Time Filter");
			}
			else if (0 == string.Compare(timestampFilter.OperandName, FilterOperandNames.OpcHdaTimestampExpression, true))
			{
				if (timestampFilter.ComparisonValue is string)
				{
					return new OPCHDA_TIME(timestampFilter.ComparisonValue as string);
				}
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Time Filter");
			}
			else
				throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "Invalid Time Filter");
		}
	}

	/// <summary>
	/// This implementation of a Data Journal List is used to maintain 
	/// a collection of historical data value collections.  Each value 
	/// maintained by a Data Journal List consists of collection of 
	/// data values where each value is associated with a specific time.
	/// </summary>
	public class DataJournalList
		: DataJournalListBase
	{
		internal DataJournalList(ContextImpl context, uint clientId, uint updateRate, uint bufferingRate,
			uint listType, uint listKey, FilterSet filterSet)
			: base(context, clientId, updateRate, bufferingRate, listType, listKey)
		{
		}

		protected override uint OnNegotiateBufferingRate(uint requestedBufferingRate)
		{
			uint negotiatedBufferingRate = 0;
			// TODO:  Negotiate Buffering Rate if Buffering Rate is supported.
			//        Also add code to implement buffering rate.
			return negotiatedBufferingRate;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="isDisposing"></param>
		/// <returns></returns>
		protected override bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			List<uint> listHServer = null;
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
						listHServer = new List<uint>();
						foreach (var kvp in this)
						{
							DataJournalListValue dlv = kvp.Value as DataJournalListValue;
							listHServer.Add(dlv.hServer);
						}
						this.Clear();
					}
				}
			}

			lock (_ListTransactionLock)
			{
				if (isDisposing)
				{
					if (listHServer != null)
					{
						List<HandleAndHRESULT> errList = null;
						ContextImpl context = OwnerContext as ContextImpl;
						if (context.IsAccessibleJournalDataAccess)
						{
							cliHRESULT HR = context.IOPCHDA_Server.ReleaseItemHandles(listHServer, out errList);
						}
					}
				}
				_queueOfChangedValues = null;

				_hasBeenDisposed = true;
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataListEntries"></param>
		/// <returns></returns>
		protected override List<AddDataObjectResult> OnAddDataObjectsToList(
			List<ValueRoot> dataListEntries)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleJournalDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCHDAServer_ProgId);

			lock (_ListTransactionLock)
			{
				List<AddDataObjectResult> resultsList = new List<AddDataObjectResult>();
				List<OPCHDA_ITEMDEF> hClientAndItemID = new List<OPCHDA_ITEMDEF>();
				foreach (var dle in dataListEntries)
				{
					OPCHDA_ITEMDEF itemDef = new OPCHDA_ITEMDEF()
					{
						hClient = dle.ServerAlias,
						sItemID = dle.InstanceId.LocalId,
					};
					hClientAndItemID.Add(itemDef);
				}

				List<OPCHDAITEMRESULT> hServerAndHResult = null;
				cliHRESULT HR = context.IOPCHDA_Server.GetItemHandles(
					hClientAndItemID, out hServerAndHResult);

				if (HR.Succeeded)
				{
					OPCHDA_TIME hdaTimeNow = new OPCHDA_TIME() { bString = true, sTime = "NOW" };
					OPCHDA_TIME hdaTimeEnd = new OPCHDA_TIME() { bString = true, sTime = null };
					List<uint> attrIDs = new List<uint>() { (uint)OPCHDA_ATTRIBUTES.OPCHDA_DATA_TYPE };
					IEnumerator<ValueRoot> iDLV = dataListEntries.GetEnumerator();
					foreach (OPCHDAITEMRESULT ir in hServerAndHResult)
					{
						iDLV.MoveNext();
						DataJournalListValue dljv = iDLV.Current as DataJournalListValue;
						JournalDataPropertyValue[] attrValues = null;
						cliHRESULT HR1 = context.IOPCHDA_SyncRead.ReadAttribute(ref hdaTimeNow, ref hdaTimeEnd,
							ir.hServer, attrIDs, out attrValues);
						TypeId typeId = null;
						if (HR1.Succeeded)
						{
							ushort dt = unchecked((ushort)attrValues[0].PropertyValues.UintValues[0]);
							typeId = new TypeId(cliVARIANT.CliTypeFrom(dt));
						}
						else
							context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);

						dljv.hServer = ir.hServer;
						dljv.StatusCode = (uint)ir.HResult;
						AddDataObjectResult ar = new AddDataObjectResult(
							(uint)ir.HResult, dljv.ClientAlias, dljv.ServerAlias, typeId, true, false);
						resultsList.Add(ar);
					}
				}
				else
				{
					context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)HR.hResult, "Failed HR returned from OPC HDA GetItemHandles!");
				}
				return resultsList;
			}
		}

		/// <summary>
		/// This method is used to obtain an instance of a DataListValue which 
		/// is the class used by the OPC HDA wrapper as its data value.
		/// </summary>
		/// <param name="clientAlias"></param>
		/// <param name="serverAlias"></param>
		/// <param name="instanceId"></param>
		/// <returns></returns>
		protected override ValueRoot OnNewDataListValue(
			uint clientAlias, uint serverAlias, InstanceId instanceId)
		{
			return new DataJournalListValue(clientAlias, serverAlias)
			{
				InstanceId = instanceId,
			};
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="listAliasResult"></param>
		/// <param name="dataListEntries"></param>
		/// <returns></returns>
		protected override List<AliasResult> OnRemoveDataObjectsFromList(
			List<AliasResult> listAliasResult, List<ValueRoot> dataListEntries)
		{
			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleJournalDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCHDAServer_ProgId);

			lock (_ListTransactionLock)
			{
				List<uint> listHServer = new List<uint>();
				foreach (var dle in dataListEntries)
				{
					DataJournalListValue dlv = dle as DataJournalListValue;
					listHServer.Add(dlv.hServer);
				}

				List<HandleAndHRESULT> errList = null;
				cliHRESULT HR = context.IOPCHDA_Server.ReleaseItemHandles(listHServer, out errList);
				if (HR.Succeeded)
				{
					if ((HR.IsS_FALSE) && (errList != null))
					{
						foreach (HandleAndHRESULT hhr in errList)
						{
							if (hhr.hResult.Failed)
							{
								AliasResult aliasResult = new AliasResult((uint)hhr.hResult, hhr.Handle, 0);
								listAliasResult.Add(aliasResult);
							}
						}
					}
				}
				else
				{
					context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)HR.hResult, "OPC HDA Remove Items Failed!");
				}
				return listAliasResult;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="numValuesPerAlias"></param>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public override JournalDataValues[] OnReadJournalDataForTimeInterval(
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases)
		{			
			if (!Enabled)
				throw FaultHelpers.Create(XiFaultCodes.E_LISTDISABLED, "List not Enabled.");

			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleJournalDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCHDAServer_ProgId);

			lock (_ListTransactionLock)
			{
				OPCHDA_TIME firstTime = OpcHdaTime.OpcHdaTimeFromTimeFilter(firstTimeStamp);
				OPCHDA_TIME secondTime = OpcHdaTime.OpcHdaTimeFromTimeFilter(secondTimeStamp);

				List<ErrorInfo> errors = new List<ErrorInfo>();
				List<uint> listHServer = null;
				List<DataJournalListValue> validItems = new List<DataJournalListValue>();
				if (null == serverAliases)
				{
					listHServer = new List<uint>(this.Count);
					lock (_DictionaryIntegrityLock)
					{
						foreach (KeyValuePair<uint, ValueRoot> kvp in this)
						{
							DataJournalListValue dataValue = kvp.Value as DataJournalListValue;
							if (null != dataValue)
							{
								listHServer.Add(dataValue.hServer);
								validItems.Add(dataValue);
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
								if (valueRoot is DataJournalListValue)
								{
									listHServer.Add((valueRoot as DataJournalListValue).hServer);
									validItems.Add(valueRoot as DataJournalListValue);
								}
								else
								{
									errors.Add(new ErrorInfo(XiFaultCodes.E_INCONSISTENTUSEAGE,
										valueRoot.ClientAlias, serverAliases[idx]));
								}
							}
							else
							{
								// server alias is not zero, but was not found; so there is no client alias
								errors.Add(new ErrorInfo(XiFaultCodes.E_ALIASNOTFOUND, 0, serverAliases[idx]));
							}
						}
					}
				}

				bool bounds = (FilterOperator.Equal == firstTimeStamp.Operator) ? true : false;
				JournalDataValues[] itemValues = null;

				cliHRESULT HR = context.IOPCHDA_SyncRead.ReadRaw(ref firstTime, ref secondTime,
					numValuesPerAlias, bounds, listHServer, out itemValues);
				if (HR.Failed)
				{
					context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)HR.hResult, "OPC HDA ReadRaw Failed.");
				}
				{
					int idx = 0;
					foreach (var dataJournalValue in validItems)
					{
						dataJournalValue.StatusCode = itemValues[idx].ResultCode;
						itemValues[idx].ClientAlias = dataJournalValue.ClientAlias;
						if (CallbackActivated || PollingActivated)
						{
							dataJournalValue.UpdateDictionary(itemValues[idx]);
						}
						idx += 1;
					}
				}
				listHServer.Clear();
				validItems.Clear();
				if (0 < errors.Count)
				{
					int newSize = itemValues.Length + errors.Count;
                    int idx = itemValues.Length;
                    Array.Resize(ref itemValues, newSize);
					foreach (ErrorInfo errorInfo in errors)
					{
						JournalDataValues jdv = new JournalDataValues()
						{
							ResultCode = XiFaultCodes.E_SEEERRORINFO,
							ClientAlias = errorInfo.ClientAlias,
							Calculation = null,
							StartTime = DateTime.MinValue,
							EndTime = DateTime.MaxValue,
							ErrorInfo = errorInfo,
							HistoricalValues = null,
						};
						itemValues[idx++] = jdv;
					}
				}
				return itemValues;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timestamps"></param>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public override JournalDataValues[] OnReadJournalDataAtSpecificTimes(
			List<DateTime> timestamps, List<uint> serverAliases)
		{			
			if (!Enabled)
				throw FaultHelpers.Create(XiFaultCodes.E_LISTDISABLED, "List not Enabled.");

			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleJournalDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCHDAServer_ProgId);

			lock (_ListTransactionLock)
			{
				List<ErrorInfo> errors = new List<ErrorInfo>();
				List<uint> listHServer = new List<uint>();
				List<DataJournalListValue> validItems = new List<DataJournalListValue>();
				int idx = 0;

				if (null == serverAliases)
				{
					listHServer = new List<uint>(this.Count);
					lock (_DictionaryIntegrityLock)
					{
						foreach (KeyValuePair<uint, ValueRoot> kvp in this)
						{
							DataJournalListValue dataValue = kvp.Value as DataJournalListValue;
							if (null != dataValue)
							{
								listHServer.Add(dataValue.hServer);
								validItems.Add(dataValue);
							}
						}
					}
				}
				else
				{
					listHServer = new List<uint>(serverAliases.Count);
					for (idx = 0; idx < serverAliases.Count; idx++)
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
								if (valueRoot is DataJournalListValue)
								{
									listHServer.Add((valueRoot as DataJournalListValue).hServer);
									validItems.Add(valueRoot as DataJournalListValue);
								}
								else
								{
									errors.Add(new ErrorInfo(XiFaultCodes.E_INCONSISTENTUSEAGE,
										valueRoot.ClientAlias, serverAliases[idx]));
								}
							}
							else
							{
								// server alias is not zero, but was not found; so there is no client alias
								errors.Add(new ErrorInfo(XiFaultCodes.E_ALIASNOTFOUND, 0, serverAliases[idx]));
							}
						}
					}
				}

				JournalDataValues[] itemValues = null;

				cliHRESULT HR = context.IOPCHDA_SyncRead.ReadAtTime(timestamps, listHServer, out itemValues);
				if (HR.Failed)
				{
					context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)HR.hResult, "OPC HDA ReadAtTime Failed.");
				}

				foreach (var dataJournalValue in validItems)
				{
					dataJournalValue.StatusCode = itemValues[idx].ResultCode;
					itemValues[idx].ClientAlias = dataJournalValue.ClientAlias;
					idx += 1;
				}

				listHServer.Clear();
				validItems.Clear();
				if (0 < errors.Count)
				{
					int newSize = itemValues.Length + errors.Count;
					Array.Resize(ref itemValues, newSize);
					idx = itemValues.Length;
					foreach (ErrorInfo errorInfo in errors)
					{
						JournalDataValues jdv = new JournalDataValues()
						{
							ResultCode = XiFaultCodes.E_SEEERRORINFO,
							ClientAlias = errorInfo.ClientAlias,
							Calculation = null,
							StartTime = DateTime.MinValue,
							EndTime = DateTime.MaxValue,
							ErrorInfo = errorInfo,
							HistoricalValues = null,
						};
						itemValues[idx++] = jdv;
					}
				}
				return itemValues;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="numValuesPerAlias"></param>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		public override JournalDataChangedValues[] OnReadJournalDataChanges(
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases)
		{			
			if (!Enabled)
				throw FaultHelpers.Create(XiFaultCodes.E_LISTDISABLED, "List not Enabled.");

			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleJournalDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCHDAServer_ProgId);

			lock (_ListTransactionLock)
			{
				OPCHDA_TIME firstTime = OpcHdaTime.OpcHdaTimeFromTimeFilter(firstTimeStamp);
				OPCHDA_TIME secondTime = OpcHdaTime.OpcHdaTimeFromTimeFilter(secondTimeStamp);

				List<ErrorInfo> errors = new List<ErrorInfo>();
				List<uint> listHServer = new List<uint>();
				List<DataJournalListValue> validItems = new List<DataJournalListValue>();
				int idx = 0;

				if (null == serverAliases)
				{
					listHServer = new List<uint>(this.Count);
					lock (_DictionaryIntegrityLock)
					{
						foreach (KeyValuePair<uint, ValueRoot> kvp in this)
						{
							DataJournalListValue dataValue = kvp.Value as DataJournalListValue;
							if (null != dataValue)
							{
								listHServer.Add(dataValue.hServer);
								validItems.Add(dataValue);
							}
						}
					}
				}
				else
				{
					listHServer = new List<uint>(serverAliases.Count);
					for (idx = 0; idx < serverAliases.Count; idx++)
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
								if (valueRoot is DataJournalListValue)
								{
									listHServer.Add((valueRoot as DataJournalListValue).hServer);
									validItems.Add(valueRoot as DataJournalListValue);
								}
								else
								{
									errors.Add(new ErrorInfo(XiFaultCodes.E_INCONSISTENTUSEAGE,
										valueRoot.ClientAlias, serverAliases[idx]));
								}
							}
							else
							{
								// server alias is not zero, but was not found; so there is no client alias
								errors.Add(new ErrorInfo(XiFaultCodes.E_ALIASNOTFOUND, 0, serverAliases[idx]));
							}
						}
					}
				}

				JournalDataChangedValues[] itemValues = null;

				cliHRESULT HR = context.IOPCHDA_SyncRead.ReadModified(ref firstTime, ref secondTime,
					numValuesPerAlias, listHServer, out itemValues);
				if (HR.Failed)
				{
					context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)HR.hResult, "OPC HDA ReadModified Failed.");
				}

				foreach (var dataJournalValue in validItems)
				{
					dataJournalValue.StatusCode = itemValues[idx].ResultCode;
					itemValues[idx].ClientAlias = dataJournalValue.ClientAlias;
					idx += 1;
				}

				listHServer.Clear();
				validItems.Clear();
				if (0 < errors.Count)
				{
					int newSize = itemValues.Length + errors.Count;
					Array.Resize(ref itemValues, newSize);
					idx = itemValues.Length;
					foreach (ErrorInfo errorInfo in errors)
					{
						JournalDataChangedValues jdv = new JournalDataChangedValues()
						{
							ResultCode = XiFaultCodes.E_SEEERRORINFO,
							ClientAlias = errorInfo.ClientAlias,
							Calculation = null,
							StartTime = DateTime.MinValue,
							EndTime = DateTime.MaxValue,
							ErrorInfo = errorInfo,
							ModificationAttributes = null,
						};
						itemValues[idx++] = jdv;
					}
				}
				return itemValues;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="calculationPeriod"></param>
		/// <param name="serverAliasesAndCalculations"></param>
		/// <returns></returns>
		public override JournalDataValues[] OnReadCalculatedJournalData(
				FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, TimeSpan calculationPeriod,
				List<AliasAndCalculation> serverAliasesAndCalculations)
		{			
			if (!Enabled)
				throw FaultHelpers.Create(XiFaultCodes.E_LISTDISABLED, "List not Enabled.");

			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleJournalDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCHDAServer_ProgId);

			lock (_ListTransactionLock)
			{
				OPCHDA_TIME firstTime = OpcHdaTime.OpcHdaTimeFromTimeFilter(firstTimeStamp);
				OPCHDA_TIME secondTime = OpcHdaTime.OpcHdaTimeFromTimeFilter(secondTimeStamp);

				List<ErrorInfo> errors = new List<ErrorInfo>();
				List<OPCHDA_HANDLEAGGREGATE> listSvrHdlAndAgger = new List<OPCHDA_HANDLEAGGREGATE>();
				List<DataJournalListValue> validItems = new List<DataJournalListValue>();
				foreach (var aliasAndCalc in serverAliasesAndCalculations)
				{
					ValueRoot dataValue = null;
					if (this.TryGetValue(aliasAndCalc.ServerAlias, out dataValue))
					{
						if (dataValue is DataJournalListValue)
						{
							listSvrHdlAndAgger.Add(new OPCHDA_HANDLEAGGREGATE()
							{
								hServer = ((dataValue as DataJournalListValue).hServer),
								haAggregate = Convert.ToUInt32(aliasAndCalc.Calculation.LocalId),
							});
							validItems.Add(dataValue as DataJournalListValue);
						}
						else
						{
							errors.Add(new ErrorInfo(XiFaultCodes.E_INCONSISTENTUSEAGE,
								dataValue.ClientAlias, aliasAndCalc.ServerAlias));
						}
					}
					else
					{
						errors.Add(new ErrorInfo(XiFaultCodes.E_ALIASNOTFOUND, 0, aliasAndCalc.ServerAlias));
					}
				}
				JournalDataValues[] itemValues = null;

				cliHRESULT HR = context.IOPCHDA_SyncRead.ReadProcessed(ref firstTime, ref secondTime,
					calculationPeriod, listSvrHdlAndAgger, out itemValues);
				if (HR.Failed)
				{
					context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
					// The next line will not be executed if the call above throws
					throw FaultHelpers.Create((uint)HR.hResult, "OPC HDA ReadProcessed Failed.");
				}

				int idx = 0;
				foreach (var dataJournalValue in validItems)
				{
					dataJournalValue.StatusCode = itemValues[idx].ResultCode;
					itemValues[idx].ClientAlias = dataJournalValue.ClientAlias;
					if (CallbackActivated || PollingActivated)
					{
						dataJournalValue.UpdateDictionary(itemValues[idx]);
					}
					idx += 1;
				}

				listSvrHdlAndAgger.Clear();
				validItems.Clear();
				if (0 < errors.Count)
				{
					int newSize = itemValues.Length + errors.Count;
					Array.Resize(ref itemValues, newSize);
					idx = itemValues.Length;
					foreach (ErrorInfo errorInfo in errors)
					{
						JournalDataValues jdv = new JournalDataValues()
						{
							ResultCode = XiFaultCodes.E_SEEERRORINFO,
							ClientAlias = errorInfo.ClientAlias,
							Calculation = null,
							StartTime = DateTime.MinValue,
							EndTime = DateTime.MaxValue,
							ErrorInfo = errorInfo,
							HistoricalValues = null,
						};
						itemValues[idx++] = jdv;
					}
				}
				return itemValues;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="serverAlias"></param>
		/// <param name="propertiesToRead"></param>
		/// <returns></returns>
		public override JournalDataPropertyValue[] OnReadJournalDataProperties(
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, uint serverAlias,
			List<TypeId> propertiesToRead)
		{			
			if (!Enabled)
				throw FaultHelpers.Create(XiFaultCodes.E_LISTDISABLED, "List not Enabled.");

			ContextImpl context = OwnerContext as ContextImpl;
			if (context.IsAccessibleJournalDataAccess == false)
				context.ThrowDisconnectedServerException(context.IOPCHDAServer_ProgId);

			lock (_ListTransactionLock)
			{
				OPCHDA_TIME firstTime = OpcHdaTime.OpcHdaTimeFromTimeFilter(firstTimeStamp);
				OPCHDA_TIME secondTime = OpcHdaTime.OpcHdaTimeFromTimeFilter(secondTimeStamp);

				JournalDataPropertyValue[] journalDataPropertyValues = null;
				ValueRoot dataListValue = null;
				if (this.TryGetValue(serverAlias, out dataListValue))
				{
					List<uint> attributeIds = new List<uint>();
					foreach (var typeId in propertiesToRead)
					{
						attributeIds.Add(Convert.ToUInt32(typeId.LocalId));
					}
					if (dataListValue is DataJournalListValue)
					{
						cliHRESULT HR = context.IOPCHDA_SyncRead.ReadAttribute(ref firstTime, ref secondTime,
							(dataListValue as DataJournalListValue).hServer, attributeIds,
							out journalDataPropertyValues);
						if (HR.Failed)
						{
							context.ThrowOnDisconnectedServer(HR.hResult, context.IOPCHDAServer_ProgId);
							// The next line will not be executed if the call above throws
							throw FaultHelpers.Create((uint)HR.hResult, "OPC HDA ReadAttribute Failed.");
						}
						for (int idx = 0; idx < journalDataPropertyValues.Length; idx++)
							journalDataPropertyValues[idx].ClientAlias = dataListValue.ClientAlias;
					}
					return journalDataPropertyValues;
				}
				throw FaultHelpers.Create(
					XiFaultCodes.E_ALIASNOTFOUND, "Bad Server Handle in ReadJournalDataProperties");
			}
		}
	}
}
