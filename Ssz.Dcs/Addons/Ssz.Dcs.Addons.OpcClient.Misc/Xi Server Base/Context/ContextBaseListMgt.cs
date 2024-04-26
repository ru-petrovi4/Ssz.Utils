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
	/// This partial class defines the methods to be overridden by the server implementation 
	/// to support the List Management methods of the IResourceManagement interface.
	/// </summary>
	public abstract partial class ContextBase<TList>
		where TList : ListRoot
	{
		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="clientId">
		/// The Client LocalId for this list.  Used in callbacks to allow the 
		/// client to identify this list.
		/// </param>
		/// <param name="listType">
		/// Indicates the type of list to be created.
		/// Standard list types as defined by the ListAttributes class 
		/// are: 
		/// 1) Data List, 
		/// 2) History Data List, 
		/// 3) Event List 
		/// 4) History Event List
		/// </param>
		/// <param name="updateRate">
		/// The requested update rate in milliseconds for the list. The  
		/// update rate indicates how often the server updates the 
		/// values of elements in the list.  A value of 0 indicates 
		/// that updating is exception-based. The server may negotiate 
		/// this value, up or down as necessary to support its efficient 
		/// operation.
		/// </param>
		/// <param name="bufferingRate">
		/// <para>An optional-use parameter that indicates that the server is 
		/// to buffer data updates, rather than overwriting them, until either 
		/// the time span defined by the buffering rate expires or the values 
		/// are transmitted to the client in a callback or poll response. If 
		/// the time span expires, then the oldest value for a data object is 
		/// discarded when a new value is received from the underlying system.</para>
		/// <para>The value of the bufferingRate is set to TimeSpan.Zero to indicate 
		/// that it is not to be used and that new values overwrite (replace) existing 
		/// cached values.  </para>
		/// <para>When used, this parameter contains the client-requested buffering 
		/// rate, which the server may negotiate up or down, or to TimeSpan.Zero if the 
		/// server does not support the buffering rate. </para>
		/// <para>The FeaturesSupported member of the StandardMib is used to indicate 
		/// server support for the buffering rate.</para>
		/// </param>
		/// <param name="filterSet">
		/// The set of filters to use to select elements of the list.  
		/// </param>
		/// <returns>
		/// The attributes created for the list.
		/// </returns>
		public abstract ListAttributes OnDefineList(uint clientId,
			uint listType, uint scanRate, uint bufferingRate, FilterSet filterSet);

		/// <summary>
		/// This method is invoked to add a list to the specified context in the 
		/// server implementation
		/// </summary>
		/// <param name="xiList">The Xi List to add to the context.</param>
		/// <returns></returns>
		protected ListAttributes AddXiList(TList xiList)
		{
			lock (ContextLock)
			{
				ListAttributes listAttrs = xiList.ListAttributes;
				_XiLists.Add(xiList.ServerId, xiList);
				return listAttrs;
			}
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listIds">
		/// The identifiers for the lists whose attributes are to be 
		/// retrieved. Null means to get the attributes for all lists.
		/// </param>
		/// <returns>
		/// The list of requested List Attributes. The size and order 
		/// of this list matches the size and order of the listAliases 
		/// parameter.  
		/// </returns>
		internal List<ListAttributes> OnGetListAttributes(List<uint> listIds)
		{
			List<ListAttributes> listListAttrs = null;
			List<TList> listTList = null;
			lock (ContextLock)
			{
				// null means to get all lists, so put all lists into listTList
				if (listIds == null)
				{
					listTList = new List<TList>(_XiLists.Count);
					listListAttrs = new List<ListAttributes>(_XiLists.Count);
					foreach (var list in _XiLists)
					{
						listTList.Add(list as TList);
					}
				}
				else if (_XiLists.Count > 0)
				{
					listTList = new List<TList>(listIds.Count);
					listListAttrs = new List<ListAttributes>(listIds.Count);
					foreach (uint listKey in listIds)
					{
						TList tList = null;
						if (_XiLists.TryGetValue(listKey, out tList))
							listTList.Add(tList);
						else
						{
							listListAttrs.Add(new ListAttributes()
							{
								ClientId = listKey,
								ServerId = listKey,
								ListType = 0,
								Enabled = false,
								UpdateRate = 0,
								CurrentCount = 0,
								HowSorted = 0,
								SortKeys = null,
								FilterSet = null,
								ResultCode = XiFaultCodes.E_BADLISTID,
							});
						}
					}
				}
			}

			if (listTList is not null && 0 < listTList.Count)
			{
				foreach (var tList in listTList)
				{
					listListAttrs.Add(tList.ListAttributes);
				}
			}
			return listListAttrs;
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the list whose aliases are to be updated.
		/// </param>
		/// <param name="newAliases">
		/// The list of current and new alias values.
		/// </param>
		/// <returns>
		/// The list of updated aliases. The size and order of this list matches 
		/// the size and order of the listAliases parameter.  Each AliasResult in 
		/// the list contains the new client alias from the request and its 
		/// corresponding new server alias assigned by the server.
		/// </returns>
		internal List<AliasResult> OnRenewAliases(uint listId, List<AliasUpdate> newAliases)
		{
			if (newAliases == null)
				throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "newAliases parameter cannot be null.");

			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}
			if (null != tList)
			{
				List<AliasResult> results = tList.OnRenewAliases(newAliases);
				if (tList.BeingDeleted == false) // could have changed since the call was made
					return results;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Renew Aliases.");
		}

		/// <summary>
		/// This method removes the specified lists from the context.
		/// It does not dispose of the lists; it only removes them from the context's list of Xi Lists.
		/// </summary>
		/// <param name="listIds">
		/// The identifiers for the lists to be removed.  If this parameter is null,
		/// then all lists for the context is to be removed.
		/// </param>
		/// <param name="removedLists">The lists that were successfully removed.</param>
		/// <returns>
		/// The list identifiers and result codes for the lists whose 
		/// removal failed. Returns null if all removals succeeded.  
		/// </returns>
		public List<AliasResult> RemoveListsFromContext(List<uint> listIds, out List<TList> removedLists)
		{
			removedLists = null;
			if ((_XiLists == null) || (_XiLists.Count == 0)) // no lists to delete
				return null;

			removedLists = new List<TList>();
			List<AliasResult> listAliasResult = null;
			lock (ContextLock)
			{
				// null means to delete all lists, so put all lists into listIds
				if (listIds == null)
				{
					listIds = new List<uint>();
					foreach (var dictionaryEntry in _XiLists)
					{
						TList list = dictionaryEntry.Value;
						list.BeingDeleted = false;
						removedLists.Add(list);						
					}
					_XiLists.Clear();
				}
				else
				{
					// find each list in listIds by its listId in _XiLists, 
					// then (1) remove it from the endpoints to which it is assigned
					//      (2) remove it from _XiLists
					foreach (uint listKey in listIds)
					{
						TList list = null;
						if (_XiLists.TryGetValue(listKey, out list))
						{
							removedLists.Add(list);							
							_XiLists.Remove(listKey);
						}
						else
						{
							if (listAliasResult == null)
								listAliasResult = new List<AliasResult>();
							listAliasResult.Add(
								new AliasResult(XiFaultCodes.E_BADLISTID, 0, listKey));
						}
					}
				}
			} // end context lock
			return listAliasResult;
		}

		/// <summary>
		/// This method may be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listIds">
		/// The lists to be disposed. They should have already been removed from the context before 
		/// this method is called.
		/// </param>
		internal void DisposeLists(List<TList> listsToDispose)
		{
			if ((listsToDispose != null) && (listsToDispose.Count > 0))
			{
				// Dispose of the lists outside of the lock to prevent deadlock.
				foreach (var tList in listsToDispose)
				{
					tList.Dispose();
				}
			}
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the list to which data objects are to be 
		/// added.
		/// </param>
		/// <param name="dataObjectsToAdd">
		/// The data objects to add.
		/// </param>
		/// <returns>
		/// The list of results. The size and order of this list matches 
		/// the size and order of the objectsToAdd parameter.   
		/// </returns>
		internal List<AddDataObjectResult> OnAddDataObjectsToList(
			uint listId, List<ListInstanceId> dataObjectsToAdd)
		{
			if (dataObjectsToAdd == null)
				throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "dataObjectsToAdd parameter cannot be null.");

			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}

			if (null != tList)
			{
				List<AddDataObjectResult> results = tList.OnAddDataObjectsToList(dataObjectsToAdd);
				if (tList.BeingDeleted == false) // could have changed since the call was made
					return results;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Add Data Object To List.");
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the list from which data objects are 
		/// to be removed.
		/// </param>
		/// <param name="serverAliasesToDelete">
		/// The server aliases of the data objects to remove.
		/// </param>
		/// <returns>
		/// The list identifiers and result codes for data objects whose 
		/// removal failed. Returns null if all removals succeeded.  
		/// </returns>
		internal List<AliasResult> OnRemoveDataObjectsFromList(
			uint listId, List<uint> serverAliasesToDelete)
		{
			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}

			if (null != tList)
			{
				if (serverAliasesToDelete == null) // null means to delete all data objects from the list
					serverAliasesToDelete = tList.GetServerAliases();

				List<AliasResult> results = tList.OnRemoveDataObjectsFromList(serverAliasesToDelete);
				if (tList.BeingDeleted == false) // could have changed since the call was made
					return results;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Remove Data Objects From List.");
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the list for which the filters are to be changed.
		/// </param>
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
		internal ModifyListAttrsResult OnModifyListAttributes(
			uint listId, Nullable<uint> updateRate, Nullable<uint> bufferingRate, FilterSet filterSet)
		{
			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}

			if (null != tList)
			{
				ModifyListAttrsResult results = tList.OnModifyListAttributes(updateRate, bufferingRate, filterSet);
				if (tList.BeingDeleted == false) // could have changed since the call was made
					return results;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Change List Filters.");
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the list for which updating is to be 
		/// enabled or disabled.
		///</param>
		/// <param name="enableUpdating">
		/// Indicates, when TRUE, that updating of the list is to be enabled,
		/// and when FALSE, that updating of the list is to be disabled.
		/// </param>
		internal ListAttributes OnEnableListUpdating(uint listId, bool enableUpdating)
		{
			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}

			if (null != tList)
			{
				ListAttributes listAttrs = tList.OnEnableListUpdating(enableUpdating);
				//if (tList.BeingDeleted == false) // could have changed since the call was made
				//{
				//	if (enableUpdating)
				//		tList.OnTouchList();
				//}
				if (tList.BeingDeleted == false) // could have changed since the last tList call was made
					return listAttrs;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Enable List Updating.");
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the list for which updating is to be 
		/// enabled or disabled.
		///</param>
		/// <param name="enableUpdating">
		/// Indicates, when TRUE, that updating of the list is to be enabled,
		/// and when FALSE, that updating of the list is to be disabled.
		/// </param>
		/// <param name="serverAliases">
		/// The list of aliases for data objects of a list for 
		/// which updating is to be enabled or disabled.
		/// When this value is null updating all elements of the list are to be 
		/// enabled/disabled. In this case, however, the enable/disable state 
		/// of the list itself is not changed.
		/// </param>
		/// <returns>
		/// <para>If the serverAliases parameter was null, returns 
		/// null if the server was able to successfully enable/disable 
		/// the list and all its elements.  If not, throws an exception 
		/// for event lists and for data lists, returns the client and server 
		/// aliases and result codes for the data objects that could not be 
		/// enabled/disabled.  </para> 
		/// <para>If the serverAliases parameter was not null, returns null 
		/// if the server was able to successfully enable/disable the data 
		/// objects identified by the serverAliases.  If not, returns the 
		/// client and server aliases and result codes for the data objects 
		/// that could not be enabled/disabled.</para> 
		/// </returns>
		internal List<AliasResult> OnEnableListElementUpdating(
			uint listId, bool enableUpdating, List<uint> serverAliases)
		{
			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}

			if (null != tList)
			{
				if (serverAliases == null) // null means to enable/disable all elements in the list
					serverAliases = tList.GetServerAliases();

				List<AliasResult> listAliasResult
					= tList.OnEnableListElementUpdating(enableUpdating, serverAliases);
				//if (tList.BeingDeleted == false) // could have changed since the call was made
				//{
				//	if (enableUpdating)
				//		tList.OnTouchList();
				//}
				if (tList.BeingDeleted == false) // could have changed since the last tList call was made
					return listAliasResult;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Enable List Updating.");
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the list for which event message fields are being added. 
		///</param>
		/// <param name="categoryId">
		/// The category for which event message fields are being added.
		/// </param>
		/// <param name="fieldObjectTypeIds">
		/// The list of category-specific fields to be included in the event 
		/// messages generated for alarms and events of the category.  Each field 
		/// is identified by its ObjectType LocalId obtained from the EventMessageFields 
		/// contained in the EventCategoryConfigurations Standard MIB element.
		/// </param>
		/// <returns>
		/// The ObjectTypeIds and result codes for the fields that could not be  
		/// added to the event message. Returns null if all succeeded.  
		/// </returns>
		internal List<TypeIdResult> OnAddEventMessageFields(
			uint listId, uint categoryId, List<TypeId> fieldObjectTypeIds)
		{
			if (fieldObjectTypeIds == null)
				throw FaultHelpers.Create(XiFaultCodes.E_BADARGUMENT, "fieldObjectTypeIds parameter cannot be null.");

			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}

			if (null != tList)
			{
				List<TypeIdResult> results = tList.OnAddEventMessageFields(categoryId, fieldObjectTypeIds);
				if (tList.BeingDeleted == false) // could have changed since the call was made
					return results;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Add Event Message Fields.");
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the lists whose data objects are to be touched.
		/// </param>
		/// <param name="serverAliases">
		/// The aliases for the data objects to touch.
		/// </param>
		/// <returns>
		/// The list of aliases whose touch failed and the result code that 
		/// indicates why it failed.  Null if all succeeded.
		/// </returns>
		internal List<AliasResult> OnTouchDataObjects(uint listId, List<uint> serverAliases)
		{
			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}

			if (null != tList)
			{
				if (serverAliases == null) // null means to touch all data objects in the list
					serverAliases = tList.GetServerAliases();

				List<AliasResult> results = tList.OnTouchDataObjects(serverAliases);
				if (tList.BeingDeleted == false) // could have changed since the call was made
					return results;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Touch.");
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier for the list whose data objects are to be touched.
		/// </param>
		internal uint OnTouchList(uint listId)
		{
			TList tList = null;
			lock (ContextLock)
			{
				_XiLists.TryGetValue(listId, out tList);
			}

			if (null != tList)
			{
				uint result = tList.OnTouchList();
				if (tList.BeingDeleted == false) // could have changed since the call was made
					return result;
				else
					throw FaultHelpers.Create(XiFaultCodes.E_LISTDELETED, "List has been deleted.");
			}
			throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Touch All.");
		}

	}
}
