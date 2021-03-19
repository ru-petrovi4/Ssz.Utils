using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Core.Lists;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Common;

namespace Ssz.DataGrpc.Client.Core.Context
{

    #region List Management

    /// <summary>
    ///     This partial class defines the List Management aspects of the DataGrpcContext
    /// </summary>
    public partial class DataGrpcContext
    {
        #region public functions

        /// <summary>
        ///     This method is used to create a DataGrpc List of one of the four supported list types.
        ///     Which are:
        ///     1) ElementValueList - used to maintain a list of active process values.
        ///     2) ElementValueJournalList - used to obtain a historical list of process values.
        ///     3) EventList - used to obtain process events as they occur.
        ///     4) EventJournalList - used to obtain a historical list of process events.
        /// </summary>
        /// <param name="dataGrpcList"></param>
        /// <param name="listParams"></param>
        public void DefineList(DataGrpcListRoot dataGrpcList, CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            uint listClientAlias = _lists.Add(dataGrpcList);
            
            try
            {
                var request = new DefineListRequest
                {
                    ContextId = this.ContextId,
                    ListClientAlias = listClientAlias,
                    ListType = dataGrpcList.ListType
                };
                if (listParams != null) request.ListParams.Add(listParams);
                var reply = _resourceManagementClient.DefineList(request);
                SetResourceManagementLastCallUtc();
                if (reply.Result.ResultCode == DataGrpcFaultCodes.S_OK)
                {
                    dataGrpcList.ListClientAlias = listClientAlias;
                    dataGrpcList.ListServerAlias = reply.Result.ServerAlias;
                    dataGrpcList.IsInServerContext = true;
                }                
            }
            catch (Exception ex)
            {
                _lists.Remove(listClientAlias);
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        /// <summary>
        ///     This method deletes a list from the DataGrpc Server.
        /// </summary>
        /// <param name="dataGrpcList"> The list to deleted </param>
        /// <returns> The results of the deletion. </returns>
        public AliasResult? RemoveList(DataGrpcListRoot dataGrpcList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            // Only do the delete of this list from the server 
            // if the context dispose is not running and the
            // list has list attributes.
            if (dataGrpcList.IsInServerContext)
            {                
                try
                {
                    var request = new DeleteListsRequest
                    {
                        ContextId = _contextId                        
                    };
                    request.ListServerAliases.Add(dataGrpcList.ListServerAlias);
                    DeleteListsReply reply = _resourceManagementClient.DeleteLists(request);
                    SetResourceManagementLastCallUtc();
                    _lists.Remove(dataGrpcList.ListClientAlias);
                    dataGrpcList.IsInServerContext = false;
                    return reply.Results.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                    throw;
                }                
            }
            return null;
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
        /// <param name="listServerAlias"> The server identifier for the list to which data objects are to be added. </param>
        /// <param name="dataObjectsToAdd"> The data objects to add. </param>
        /// <returns>
        ///     The list of results. The size and order of this list matches the size and order of the objectsToAdd
        ///     parameter.
        /// </returns>
        public List<AddDataObjectToListResult> AddDataObjectsToList(uint listServerAlias, List<ListInstanceId> dataObjectsToAdd)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();
            
            try
            {
                var request = new AddDataObjectsToListRequest
                {
                    ContextId = this.ContextId,
                    ListServerAlias = listServerAlias
                };
                request.DataObjectsToAdd.Add(dataObjectsToAdd);
                var reply = _resourceManagementClient.AddDataObjectsToList(request);
                SetResourceManagementLastCallUtc();
                return reply.Results.ToList();
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        /// <summary>
        ///     <para>
        ///         This method is used to remove members from a list. It does not, however, delete the corresponding data
        ///         object from the server.
        ///     </para>
        /// </summary>
        /// <param name="listServerAlias"> The server identifier for the list from which data objects are to be removed. </param>
        /// <param name="serverAliasesToRemove"> The server aliases of the data objects to remove. </param>
        /// <returns>
        ///     The list identifiers and result codes for data objects whose removal failed. Returns null if all removals
        ///     succeeded.
        /// </returns>
        public List<AliasResult> RemoveDataObjectsFromList(uint listServerAlias, List<uint> serverAliasesToRemove)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new RemoveDataObjectsFromListRequest
                {
                    ContextId = this.ContextId,
                    ListServerAlias = listServerAlias
                };
                request.ServerAliasesToRemove.Add(serverAliasesToRemove);
                var reply = _resourceManagementClient.RemoveDataObjectsFromList(request);
                SetResourceManagementLastCallUtc();
                return reply.Results.ToList();
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
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
        /// <param name="listServerAlias"> The identifier for the list for which updating is to be enabled or disabled. </param>
        /// <param name="enable">
        ///     Indicates, when TRUE, that updating of the list is to be enabled, and when FALSE, that
        ///     updating of the list is to be disabled.
        /// </param>
        /// <returns> The attributes of the list. </returns>
        public bool EnableListCallback(uint listServerAlias, bool enable)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new EnableListCallbackRequest
                {
                    ContextId = this.ContextId,
                    ListServerAlias = listServerAlias,
                    Enable = enable
                };
                var reply = _resourceManagementClient.EnableListCallback(request);
                SetResourceManagementLastCallUtc();
                return reply.Enabled;
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
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
        /// <param name="listServerAlias"> The identifier for the list to be touched. </param>
        /// <returns> The result code for the operation. See DataGrpcFaultCodes class for standardized result codes. </returns>
        public uint TouchList(uint listServerAlias)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new TouchListRequest
                {
                    ContextId = this.ContextId,
                    ListServerAlias = listServerAlias
                };
                var touchLisReply = _resourceManagementClient.TouchList(request);
                SetResourceManagementLastCallUtc();
                return touchLisReply.ResultCode;
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     This method returns the list with the specified Client List Id
        /// </summary>
        /// <param name="clientListId"> The client list id </param>
        /// <returns> The specified list </returns>
        private DataGrpcElementValueList GetElementValueList(uint clientListId)
        {
            DataGrpcListRoot? dataGrpcListRoot;
            _lists.TryGetValue(clientListId, out dataGrpcListRoot);
            var result = dataGrpcListRoot as DataGrpcElementValueList;
            if (result == null) throw new InvalidOperationException();
            return result;
        }

        /// <summary>
        ///     This method returns the list with the specified Client List Id
        /// </summary>
        /// <param name="clientListId"> The client list id </param>
        /// <returns> The specified list </returns>
        private DataGrpcElementValueJournalList GetElementValueJournalList(uint clientListId)
        {
            DataGrpcListRoot? dataGrpcListRoot;
            _lists.TryGetValue(clientListId, out dataGrpcListRoot);
            var result = dataGrpcListRoot as DataGrpcElementValueJournalList;
            if (result == null) throw new InvalidOperationException();
            return result;
        }

        /// <summary>
        ///     This method returns the list with the specified Client List Id
        /// </summary>
        /// <param name="clientListId"> The client list id </param>
        /// <returns> The specified list </returns>
        private DataGrpcEventList GetEventList(uint clientListId)
        {
            DataGrpcListRoot? dataGrpcListRoot;
            _lists.TryGetValue(clientListId, out dataGrpcListRoot);            
            var result = dataGrpcListRoot as DataGrpcEventList;
            if (result == null) throw new InvalidOperationException();
            return result;
        }

        #endregion

        #region private fields

        /// <summary>
        /// </summary>
        private readonly ObjectManager<DataGrpcListRoot> _lists = new ObjectManager<DataGrpcListRoot>(16);

        #endregion
    }

    #endregion // List Management
}