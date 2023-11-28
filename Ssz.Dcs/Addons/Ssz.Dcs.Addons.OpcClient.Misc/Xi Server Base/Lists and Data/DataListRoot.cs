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

namespace Xi.Server.Base
{
	/// <summary>
	/// This is the root or base class for all lists the report data values either current or historical.
	/// </summary>
	public abstract class DataListRoot
		: ListRoot
	{
		public DataListRoot(ContextBase<ListRoot> context, uint clientId, uint updateRate, uint bufferingRate,
							uint listType, uint listKey)
			: base(context, clientId, updateRate, bufferingRate, listType, listKey)
		{
		}

		/// <summary>
		/// This method is invoked from Context Base (List Management) 
		/// to Add Data objects To this List.
		/// </summary>
		/// <param name="dataObjectsToAdd"></param>
		/// <returns></returns>
		public override List<AddDataObjectResult> OnAddDataObjectsToList(
			List<ListInstanceId> dataObjectsToAdd)
		{
			if (dataObjectsToAdd == null)
				throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "dataObjectsToAdd parameter cannot be null.");

			List<AddDataObjectResult> resultsList = null;
			List<ValueRoot> listDataListEntry = new List<ValueRoot>(dataObjectsToAdd.Count);
			lock (_DictionaryIntegrityLock)
			{
				foreach (var id in dataObjectsToAdd)
				{
					uint serverAlias = NewUniqueServerAlias();
					ValueRoot dataListEntry = OnNewDataListValue(id.ClientAlias,
						serverAlias, id.ObjectElementId);
					if (dataListEntry != null)
					{
						if ((uint)ListElementOptions.AccessAsString == id.ListElementOptions)
						{
							dataListEntry.ListElementOptions = ListElementOptions.AccessAsString;
						}
						AddAValue(dataListEntry); // This method locks the dictionary
						listDataListEntry.Add(dataListEntry);
					}
					else
					{
						AddDataObjectResult addDataObjectResult
							= new AddDataObjectResult(XiFaultCodes.E_BADARGUMENT, id.ClientAlias, serverAlias, null, false, false);
                        // Fix null reference - we will create list if it not created yet
                        if (null == resultsList)
                            resultsList = new List<AddDataObjectResult>();
                        resultsList.Add(addDataObjectResult);
					}
				}
			}

			if (listDataListEntry.Count > 0)
			{
				List<AddDataObjectResult> comResultsList = OnAddDataObjectsToList(listDataListEntry);
				foreach (var ir in comResultsList)
				{
					if (FaultHelpers.Failed(ir.Result))
					{
						RemoveAValue(FindEntryRoot(ir.ServerAlias)); // This method locks the dictionary
						ir.ServerAlias = 0;
					}
				}

				if ((resultsList == null) || (resultsList.Count == 0))
					resultsList = comResultsList;
				else
				{
					// because there are likely to be only a few resultsList members
					// add them to the comResultsList, and then set resultsList to comResultsList
					// then return resultsList
					foreach (var result in resultsList)
					{
						comResultsList.Add(result);
					}
					resultsList = comResultsList;
				}
			}
			return resultsList;
		}

		/// <summary>
		/// Normally an override will be provided in the implementation 
		/// subclass to add the Data List Value Base 
		/// instance to the Data List.
		/// </summary>
		/// <param name="listDataListEntry"></param>
		/// <returns></returns>
		protected virtual List<AddDataObjectResult> OnAddDataObjectsToList(
			List<ValueRoot> listDataListEntry)
		{
			List<AddDataObjectResult> resultsList = new List<AddDataObjectResult>(listDataListEntry.Count);
			foreach (var dle in listDataListEntry)
			{
				AddDataObjectResult result = new AddDataObjectResult(
					XiFaultCodes.S_OK, dle.ClientAlias, dle.ServerAlias,
					((dle.ListElementOptions == ListElementOptions.AccessAsString) ? new TypeId(typeof(string)) : (null)),
					false, false);
				resultsList.Add(result);
			}
			return resultsList;
		}

		/// <summary>
		/// The implementation subclass provides the implementation of this abstract method 
		/// to create / construct an instance of a subclass of Data List Value Base.
		/// </summary>
		/// <param name="clientAlias"></param>
		/// <param name="serverAlias"></param>
		/// <param name="instanceId"></param>
		/// <returns></returns>
		protected abstract ValueRoot OnNewDataListValue(
			uint clientAlias, uint serverAlias, InstanceId instanceId);

		/// <summary>
		/// This method is used to Remove Data Objects From this List.  
		/// It is invoked from Context Base {List Management} Remove Data Object From List.
		/// </summary>
		/// <param name="serverAliases"></param>
		/// <returns>Return null if all were successfully removed. Otherwise, an AliasResult 
		/// is returned for each whose removal failed.</returns>
		public override List<AliasResult> OnRemoveDataObjectsFromList(List<uint> serverAliases)
		{
			lock (_ListTransactionLock)
			{
			    if (serverAliases == null) // null means to enable/disable all elements in the list
			        serverAliases = GetServerAliases();

			    if (serverAliases == null)
			        return new List<AliasResult>();
			   
                var listAliasResult = new List<AliasResult>(serverAliases.Count);
			    var dataListEntries = new List<ValueRoot>(serverAliases.Count);

			    for (var idx = 0; idx < serverAliases.Count; idx++)
			    {
			        // make sure each server alias is in the List
			        if (serverAliases[idx] == 0)
			        {
			            // if the server alias is 0, use its index into the serverAliases list as the client index
			            listAliasResult.Add(new AliasResult(XiFaultCodes.E_ALIASNOTFOUND, (uint)idx, 0));
			        }
			        else
			        {
			            bool bValueFound;
			            ValueRoot valueRoot;
			            lock (_DictionaryIntegrityLock)
			            {
			                bValueFound = TryGetValue(serverAliases[idx], out valueRoot);
			            }
			            if ((bValueFound) && (valueRoot != null))
			            {
			                // make sure each server alias is for a data object and then
			                // take it out of the server's list, and then, below, make the call to 
			                // OnRemoveDataObjectsFromList(listAliasResult, dataListEntries) 
			                // to remove it from the OPC DA Server
			                if (valueRoot is DataListValueBase)
			                {
			                    dataListEntries.Add(valueRoot as DataListValueBase);
			                    RemoveAValue(valueRoot);
			                }
			                else if (valueRoot is DataJournalListValueBase)
			                {
			                    dataListEntries.Add(valueRoot as DataJournalListValueBase);
			                    RemoveAValue(valueRoot);
			                }
			                else
			                {
			                    var aliasResult
			                        = new AliasResult(XiFaultCodes.E_INCONSISTENTUSEAGE, valueRoot.ClientAlias,
			                            serverAliases[idx]);
			                    listAliasResult.Add(aliasResult);
			                }
			            }
			            else
			            {
			                var aliasResult
			                    = new AliasResult(XiFaultCodes.E_ALIASNOTFOUND, 0, serverAliases[idx]);
			                    // server alias is not zero, but was not found; so there is no client alias
			                listAliasResult.Add(aliasResult);
			            }
			        }
			    }
			    if (0 < dataListEntries.Count)
			    {
			        listAliasResult = OnRemoveDataObjectsFromList(listAliasResult, dataListEntries);
			        dataListEntries.Clear();
			    }
			    return listAliasResult;
			}
		}

		/// <summary>
		/// This method should be overridden in the implementation 
		/// base class to take any actions needed to remove the 
		/// specified Data List Value Base instances from the list.
		/// </summary>
		/// <param name="listUintIdRes"></param>
		/// <param name="dataListEntries"></param>
		/// <returns></returns>
		protected virtual List<AliasResult> OnRemoveDataObjectsFromList(
			List<AliasResult> listAliasResult, List<ValueRoot> dataListEntries)
		{
			// Note: _ListTransactionLock has been locked
			return listAliasResult;
		}
	}
}
