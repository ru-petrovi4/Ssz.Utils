using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Ssz.Utils;
using Ssz.Xi.Client.Internal.Lists;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Context
{

    #region List Management

    /// <summary>
    ///     This partial class defines the List Management aspects of the XiContext
    /// </summary>
    internal partial class XiContext
    {
        #region public functions

        /// <summary>
        ///     This method is used to create a Xi List of one of the four supported list types.
        ///     Which are:
        ///     1) DataList - used to maintain a list of active process values.
        ///     2) DataJournalList - used to obtain a historical list of process values.
        ///     3) EventList - used to obtain process events as they occur.
        ///     4) EventJournalList - used to obtain a historical list of process events.
        /// </summary>
        /// <param name="xiList"> The list to be created. </param>
        /// <param name="updateRate">
        ///     The requested update rate in milliseconds for the list. The update rate indicates how often
        ///     the server updates the values of elements in the list. A value of 0 indicates that updating is exception-based. The
        ///     server may negotiate this value, up or down as necessary to support its efficient operation.
        /// </param>
        /// <param name="bufferingRate">
        ///     <para>
        ///         An optional-use parameter that indicates that the server is to buffer data updates, rather than overwriting
        ///         them, until either the time span defined by the buffering rate expires or the values are transmitted to the
        ///         client in a callback or poll response. If the time span expires, then the oldest value for a data object is
        ///         discarded when a new value is received from the underlying system.
        ///     </para>
        ///     <para>
        ///         The value of the bufferingRate is set to 0 to indicate that it is not to be used and that new values
        ///         overwrite (replace) existing cached values.
        ///     </para>
        ///     <para>
        ///         When used, this parameter contains the client-requested buffering rate, which the server may negotiate up or
        ///         down, or to 0 if the server does not support the buffering rate.
        ///     </para>
        ///     <para> The FeaturesSupported member of the StandardMib is used to indicate server support for the buffering rate. </para>
        /// </param>
        /// <param name="filterSet"> The set of filters to be used to select the elements of the list. </param>
        /// <returns> The attributes created for the list. </returns>
        public ListAttributes? DefineList(XiListRoot xiList, uint updateRate, uint bufferingRate, FilterSet? filterSet)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            uint clientListId = _lists.Add(xiList);
            ListAttributes? listAttrs = null;
            if (_iResourceManagement is null) throw new InvalidOperationException();
            try
            {
                listAttrs = _iResourceManagement.DefineList(ContextId, clientListId,
                    (uint) xiList.StandardListType,
                    updateRate, bufferingRate, filterSet);
                
            }
            catch (Exception)
            {
                _lists.Remove(clientListId);
                // If AE list error it is not problem
                //ProcessRemoteMethodCallException(ex);
            }

            _listArray = _lists.ToArray();

            return listAttrs;
        }

        /// <summary>
        ///     This method deletes a list from the Xi Server.
        /// </summary>
        /// <param name="xiList"> The list to deleted </param>
        /// <returns> The results of the deletion. </returns>
        public AliasResult? RemoveList(XiListRoot xiList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (xiList.ListAttributes is not null)
            {
                // Only do the delete of this list from the server 
                // if the context dispose is not running and the
                // list has list attributes.
                if (0 != xiList.ServerListId)
                {
                    var listIds = new List<uint>();
                    listIds.Add(xiList.ServerListId);
                    List<AliasResult>? listAliasResult = null;
                    if (_iResourceManagement is null) throw new InvalidOperationException();
                    try
                    {
                        listAliasResult = _iResourceManagement.DeleteLists(ContextId, listIds);
                        
                    }
                    catch (Exception ex)
                    {
                        ProcessRemoteMethodCallException(ex);
                    }
                    _lists.Remove(xiList.ClientListId);

                    _listArray = _lists.ToArray();

                    if (null != listAliasResult) return listAliasResult[0];
                }
            }
            return null;
        }

        /// <summary>
        ///     This method gets the attributes of a Xi List.
        /// </summary>
        /// <param name="serverListId"> The server id for the list </param>
        /// <returns> The requested list attributes </returns>
        public ListAttributes GetListAttributes(uint serverListId)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            var listIds = new List<uint>
            {
                serverListId
            };
            List<ListAttributes>? listListAttrs = null;
            if (_iResourceManagement is null) throw new InvalidOperationException();
            try
            {
                listListAttrs = _iResourceManagement.GetListAttributes(ContextId, listIds);
                
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            if (listListAttrs is null) throw new InvalidOperationException();
            return listListAttrs.First();
        }

        /// <summary>
        ///     <para>
        ///         This method is used to add objects to a list. Objects are added with updating of their values by the server
        ///         disabled. Updating of values by the server can be enabled using the EnableListUpdating() method.
        ///     </para>
        ///     <para>
        ///         For performance reasons, data objects should not be added one at a time by clients. Clients should, instead,
        ///         create a list of data objects and submit them all together to be added to the data list.
        ///     </para>
        /// </summary>
        /// <param name="serverListId"> The server identifier for the list to which data objects are to be added. </param>
        /// <param name="dataObjectsToAdd"> The data objects to add. </param>
        /// <returns>
        ///     The list of results. The size and order of this list matches the size and order of the objectsToAdd
        ///     parameter.
        /// </returns>
        public List<AddDataObjectResult>? AddDataObjectsToList(uint serverListId, List<ListInstanceId> dataObjectsToAdd)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            List<AddDataObjectResult>? results = null;
            if (serverListId != 0)
            {
                try
                {
                    results = _iResourceManagement.AddDataObjectsToList(ContextId, serverListId, dataObjectsToAdd);
                    
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }            
            return results;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to remove members from a list. It does not, however, delete the corresponding data
        ///         object from the server.
        ///     </para>
        /// </summary>
        /// <param name="serverListId"> The server identifier for the list from which data objects are to be removed. </param>
        /// <param name="serverAliasesToRemove"> The server aliases of the data objects to remove. </param>
        /// <returns>
        ///     The list identifiers and result codes for data objects whose removal failed. Returns null if all removals
        ///     succeeded.
        /// </returns>
        public List<AliasResult>? RemoveDataObjectsFromList(uint serverListId, List<uint> serverAliasesToRemove)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            List<AliasResult>? results = null;
            if (serverListId != 0) 
            {
                try
                {
                    results = _iResourceManagement.RemoveDataObjectsFromList(ContextId, serverListId,
                        serverAliasesToRemove);
                    
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }            
            return results;
        }

        /// <summary>
        ///     This method is used to change the update rate, buffering rate, and/or
        ///     filter set of a list.  The new value replace the old values if they exist.
        /// </summary>
        /// <param name="serverListId"> The seerver identifier for the list for which the filters are to be changed. </param>
        /// <param name="updateRate">
        ///     The new update rate of the list. The server will negotiate this rate to one that it can
        ///     support. GetListAttributes can be used to obtain the current value of this parameter. Null if the update rate is
        ///     not to be updated.
        /// </param>
        /// <param name="bufferingRate">
        ///     The new buffering rate of the list. The server will negotiate this rate to one that it can
        ///     support. GetListAttributes can be used to obtain the current value of this parameter. Null if the buffering rate is
        ///     not to be updated.
        /// </param>
        /// <param name="filterSet">
        ///     The new set of filters. The server will negotiate these filters to those that it can support.
        ///     GetListAttributes can be used to obtain the current value of this parameter. Null if the filters are not to be
        ///     updated.
        /// </param>
        /// <returns>
        ///     The revised update rate, buffering rate, and filter set. Attributes that were not updated are set to null in
        ///     this response.
        /// </returns>
        public ModifyListAttrsResult? ModifyListAttributes(uint serverListId, uint? updateRate, uint? bufferingRate,
            FilterSet filterSet)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            ModifyListAttrsResult? result = null;
            try
            {
                result = _iResourceManagement.ModifyListAttributes(ContextId, serverListId, updateRate,
                    bufferingRate,
                    filterSet);
                
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            return result;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to enable or disable updating of an entire list. When this method is called, the enabled
        ///         state of the list is changed, but the enabled state of the individual elements of the list is unchanged.
        ///     </para>
        ///     <para>
        ///         When a list is disabled, the server excludes it from participating in callbacks and polls. However, at the
        ///         option of the server, the server may continue updating its cache for the elements of the list.
        ///     </para>
        /// </summary>
        /// <param name="serverListId"> The identifier for the list for which updating is to be enabled or disabled. </param>
        /// <param name="enableUpdating">
        ///     Indicates, when TRUE, that updating of the list is to be enabled, and when FALSE, that
        ///     updating of the list is to be disabled.
        /// </param>
        /// <returns> The attributes of the list. </returns>
        public ListAttributes? EnableListUpdating(uint serverListId, bool enableUpdating)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            ListAttributes? listAttrs = null;
            if (serverListId != 0)
            {
                try
                {
                    listAttrs = _iResourceManagement.EnableListUpdating(ContextId, serverListId, enableUpdating);
                    
                }
                catch //(CommunicationException)
                {
                    throw new Exception(
                        "The connection to the server has been lost. Attempting to re-connect...");
                }
            }               
            return listAttrs;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to enable or disable updating of individual entries of a list. If the server aliases
        ///         parameter is null, then all entries of the list are enabled/disabled. This call does not change the enabled
        ///         state of the list itself.
        ///     </para>
        ///     <para>
        ///         When an element of the list is disabled, the server excludes it from participating in callbacks and polls.
        ///         However, at the option of the server, the server may continue updating its cache for the element.
        ///     </para>
        /// </summary>
        /// <param name="serverListId"> The identifier for the list for which updating is to be enabled or disabled. </param>
        /// <param name="enableUpdating">
        ///     Indicates, when TRUE, that updating of the list is to be enabled, and when FALSE, that
        ///     updating of the list is to be disabled.
        /// </param>
        /// <param name="serverAliases">
        ///     The list of aliases for data objects of a list for which updating is to be enabled or
        ///     disabled. When this value is null updating all elements of the list are to be enabled/disabled. In this case,
        ///     however, the enable/disable state of the list itself is not changed.
        /// </param>
        /// <returns>
        ///     <para>
        ///         Returns null if the server was able to successfully enable/disable the the specified elements for the
        ///         specified list. If not, returns the client and server aliases and result codes for the data objects that could
        ///         not be enabled/disabled.
        ///     </para>
        ///     <para> Throws an exception if the specified context or list could not be found. </para>
        /// </returns>
        public IEnumerable<AliasResult>? EnableListElementUpdating(uint serverListId, bool enableUpdating,
            List<uint>? serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            List<AliasResult>? listAliasResult = null;
            if (serverListId != 0)
            {
                try
                {
                    listAliasResult = _iResourceManagement.EnableListElementUpdating(ContextId, serverListId,
                        enableUpdating,
                        serverAliases);
                    
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }            
            return listAliasResult;
        }

        /// <summary>
        ///     This method is used to request that category-specific fields be
        ///     included in event messages generated for alarms and events of
        ///     the category for the specified Event/Alarm List.
        /// </summary>
        /// <param name="serverListId"> The server identifier for the list for which event message fields are being added. </param>
        /// <param name="categoryId"> The category for which event message fields are being added. </param>
        /// <param name="fieldObjectTypeIds">
        ///     The list of category-specific fields to be included in the event messages generated
        ///     for alarms and events of the category. Each field is identified by its ObjectType LocalId obtained from the
        ///     EventMessageFields contained in the EventCategoryConfigurations Standard MIB element.
        /// </param>
        /// <returns>
        ///     The ObjectTypeIds and result codes for the fields that could not be added to the event message. Returns null
        ///     if all succeeded.
        /// </returns>
        public List<TypeIdResult>? AddEventMessageFields(uint serverListId, uint categoryId,
            IEnumerable<TypeId> fieldObjectTypeIds)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            List<TypeIdResult>? typeIdResults = null;
            try
            {
                List<TypeId> listFieldObjectTypeIds = fieldObjectTypeIds.ToList();
                typeIdResults = _iResourceManagement.AddEventMessageFields(ContextId, serverListId, categoryId,
                    listFieldObjectTypeIds);
                
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            return typeIdResults;
        }

        /// <summary>
        ///     <para>
        ///         This method is used to cause one or more data objects of a list to be "touched". Data objects that are in
        ///         the disabled state (see the EnableListElementUpdating() method) are not affected by this method. This method
        ///         cannot be used with event lists.
        ///     </para>
        ///     <para>
        ///         Touching an enabled data object causes the server to update the data object, mark it as changed (even if
        ///         their values did not change), and then return it to the client in the next callback or poll.
        ///     </para>
        /// </summary>
        /// <param name="serverListId"> The identifier for the lists whose data objects are to be touched. </param>
        /// <param name="serverAliases"> The aliases for the data objects to touch. </param>
        /// <returns>
        ///     The list of error codes for the data objects that could not be touched. See XiFaultCodes claass for
        ///     standardized result codes. Data objects that were successfully touched are not included in this list.
        /// </returns>
        public IEnumerable<AliasResult>? TouchDataObjects(uint serverListId, List<uint> serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            List<AliasResult>? listAliasResult = null;
            try
            {
                listAliasResult = _iResourceManagement.TouchDataObjects(ContextId, serverListId, serverAliases);
                
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
            }
            return listAliasResult;
        }

        /// <summary>
        ///     <para> This method is used to cause a list to be "touched". </para>
        ///     <para>
        ///         For lists that contain data objects, this method causes the server to update all data objects in the list
        ///         that are currently enabled (see the EnableListElementUpdating() method), mark them as changed (even if their
        ///         values did not change), and then return them all to the client in the next callback or poll.
        ///     </para>
        ///     <para>
        ///         For lists that contain events, this method causes the server to mark all alarms/event in the list as
        ///         changed, and then return them all to the client in the next callback.
        ///     </para>
        /// </summary>
        /// <param name="serverListId"> The identifier for the list to be touched. </param>
        /// <returns> The result code for the operation. See XiFaultCodes class for standardized result codes. </returns>
        public uint TouchList(uint serverListId)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_iResourceManagement is null) throw new InvalidOperationException();
            uint uintResult = 0xFFFFFFFFu;
            if (serverListId != 0)
            {
                try
                {
                    uintResult = _iResourceManagement.TouchList(ContextId, serverListId);
                    
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }            
            return uintResult;
        }

        public XiListRoot[] ListArray
        {
            get { return _listArray ?? new XiListRoot[0]; }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     This method returns the list with the specified Client List Id
        /// </summary>
        /// <param name="clientListId"> The client list id </param>
        /// <returns> The specified list </returns>
        private XiDataList? GetDataList(uint clientListId)
        {
            XiListRoot? xiListRoot;
            _lists.TryGetValue(clientListId, out xiListRoot);            
            return xiListRoot as XiDataList;
        }

        /// <summary>
        ///     This method returns the list with the specified Client List Id
        /// </summary>
        /// <param name="clientListId"> The client list id </param>
        /// <returns> The specified list </returns>
        private XiDataJournalList? GetDataListJournal(uint clientListId)
        {
            XiListRoot? xiListRoot;
            _lists.TryGetValue(clientListId, out xiListRoot);            
            return xiListRoot as XiDataJournalList;
        }

        /// <summary>
        ///     This method returns the list with the specified Client List Id
        /// </summary>
        /// <param name="clientListId"> The client list id </param>
        /// <returns> The specified list </returns>
        private XiEventList? GetEventList(uint clientListId)
        {
            XiListRoot? xiListRoot;
            _lists.TryGetValue(clientListId, out xiListRoot);                        
            return xiListRoot as XiEventList;
        }

        #endregion

        #region private fields

        private XiListRoot[]? _listArray;

        /// <summary>
        /// </summary>
        private readonly ObjectManager<XiListRoot> _lists = new ObjectManager<XiListRoot>(16);

        #endregion
    }

    #endregion // List Management
}