using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;

namespace Ssz.DataAccessGrpc.Client.Managers
{
    internal abstract class ClientElementListManagerBase<TDataAccessGrpcListItem, TDataAccessGrpcList>
        where TDataAccessGrpcListItem : ClientElementListItemBase
        where TDataAccessGrpcList : ClientElementListBase<TDataAccessGrpcListItem>
    {
        #region construction and destruction

        protected ClientElementListManagerBase(ILogger<GrpcDataAccessProvider> logger)
        {
            Logger = logger;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Can be added several times with same elementId
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="clientObj"></param>
        public void AddItem(string elementId, object clientObj)
        {
            Logger.LogDebug("DataAccessGrpcListItemsManager.AddItem() " + elementId); 

            ClientObjectInfo? clientObjectInfo;
            if (!_clientObjectInfosDictionary.TryGetValue(clientObj, out clientObjectInfo))
            {                
                clientObjectInfo = new ClientObjectInfo(elementId);
                _clientObjectInfosDictionary.Add(clientObj, clientObjectInfo);
                clientObjectInfo.ClientObj = clientObj;

                _dataGrpcItemsMustBeAddedOrRemoved = true;
            }
            else
            {
                if (clientObjectInfo.ElementId != elementId)
                    Logger.LogError("DataAccessGrpcListItemsManager.AddItem() error, duplicate clientObj " + elementId);
            }
        }

        /// <summary>
        ///     valueSubscription is not null
        ///     If valueSubscription is not subscribed - does nothing. 
        /// </summary>
        /// <param name="clientObj"></param>
        public void RemoveItem(object clientObj)
        {
            if (_clientObjectInfosDictionary.Count == 0)
                return;
#if NET5_0_OR_GREATER
            ClientObjectInfo? clientObjectInfo;
            if (_clientObjectInfosDictionary.Remove(clientObj, out clientObjectInfo))
            {
                _clientObjectInfosToRemove.Add(clientObjectInfo);                
                clientObjectInfo.ClientObj = null;

                _dataGrpcItemsMustBeAddedOrRemoved = true;
            }
            else
            {
                Logger.LogError("DataAccessGrpcListItemsManager.RemoveItem() error, clientObj was not added earlier");
            }            
#else
            ClientObjectInfo? clientObjectInfo;
            if (_clientObjectInfosDictionary.TryGetValue(clientObj, out clientObjectInfo))
            {
                _clientObjectInfosToRemove.Add(clientObjectInfo);
                _clientObjectInfosDictionary.Remove(clientObj);
                clientObjectInfo.ClientObj = null;

                _dataGrpcItemsMustBeAddedOrRemoved = true;
            }
            else
            {
                Logger.LogError("DataAccessGrpcListItemsManager.RemoveItem() error, clientObj was not added earlier");
            }
#endif

        }

        public void Unsubscribe(bool clearClientSubscriptions)
        {
            foreach (var dataGrpcListItemWrapper in _dataGrpcListItemWrappersDictionary.Values)
            {
                dataGrpcListItemWrapper.DataAccessGrpcListItem = null;
                dataGrpcListItemWrapper.ConnectionError = false;
                dataGrpcListItemWrapper.FailedAddItemResult = null;
                if (clearClientSubscriptions) dataGrpcListItemWrapper.ClientObjectInfosCollection.Clear();
            }

            if (clearClientSubscriptions) 
                _clientObjectInfosDictionary.Clear();

            DataAccessGrpcList = null;

            _clientObjectInfosToRemove.Clear();

            _dataGrpcItemsMustBeAddedOrRemoved = true;
        }

        public IEnumerable<object> GetAllClientObjs()
        {
            return _clientObjectInfosDictionary.Values.Select(mi => mi.ClientObj).Where(o => o is not null).OfType<object>();
        }

        #endregion

        #region protected functions

        protected ILogger<GrpcDataAccessProvider> Logger { get; }        

        protected TDataAccessGrpcList? DataAccessGrpcList { get; set; }        

        protected ClientObjectInfo? GetClientObjectInfo(object clientObj)
        {
            ClientObjectInfo? clientObjectInfo;
            _clientObjectInfosDictionary.TryGetValue(clientObj, out clientObjectInfo);
            return clientObjectInfo;
        }        

        /// <summary>
        ///     Returns whether connection errors occur.
        /// </summary>
        /// <returns></returns>
        protected bool SubscribeInitial(bool unsubscribeItemsFromServer)
        {
            bool connectionError = false;
            
            foreach (ClientObjectInfo clientObjectInfo in _clientObjectInfosDictionary.Values)
            {
                if (clientObjectInfo.DataAccessGrpcListItemWrapper is null)
                {
                    var elementId = clientObjectInfo.ElementId;
                    DataAccessGrpcListItemWrapper? dataGrpcListItemWrapper;
                    if (!_dataGrpcListItemWrappersDictionary.TryGetValue(elementId, out dataGrpcListItemWrapper))
                    {
                        dataGrpcListItemWrapper = new DataAccessGrpcListItemWrapper();
                        _dataGrpcListItemWrappersDictionary.Add(elementId, dataGrpcListItemWrapper);
                    }
                    else
                    {
                        clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = true;
                    }
                    clientObjectInfo.NotifyClientObj_AddItemResult = true;
                    clientObjectInfo.DataAccessGrpcListItemWrapper = dataGrpcListItemWrapper;
                    dataGrpcListItemWrapper.ClientObjectInfosCollection.Add(clientObjectInfo);
                }                
            }

            var itemsToAdd_DataGrpcListItemWrappers = new List<DataAccessGrpcListItemWrapper>();
            foreach (var kvp in _dataGrpcListItemWrappersDictionary)
            {
                DataAccessGrpcListItemWrapper dataGrpcListItemWrapper = kvp.Value;
                if (dataGrpcListItemWrapper.DataAccessGrpcListItem is null && dataGrpcListItemWrapper.FailedAddItemResult is null)
                {
                    itemsToAdd_DataGrpcListItemWrappers.Add(dataGrpcListItemWrapper);
                    TDataAccessGrpcListItem? dataGrpcListItem = null;
                    if (DataAccessGrpcList is not null && !DataAccessGrpcList.Disposed)
                    {
                        try
                        {
                            dataGrpcListItem = DataAccessGrpcList.PrepareAddItem(kvp.Key);
                        }
                        catch
                        {
                            connectionError = true;
                        }
                    }
                    else 
                        connectionError = true;
                    if (dataGrpcListItem is not null)
                    {
                        dataGrpcListItem.Obj = dataGrpcListItemWrapper;
                        dataGrpcListItemWrapper.DataAccessGrpcListItem = dataGrpcListItem;
                    }                    
                }                
            }

            if (itemsToAdd_DataGrpcListItemWrappers.Count > 0)
            {
                IEnumerable<TDataAccessGrpcListItem>? failedItems = null;
                if (!connectionError && DataAccessGrpcList is not null && !DataAccessGrpcList.Disposed)
                {
                    try
                    {
                        failedItems = DataAccessGrpcList.CommitAddItems();
                    }
                    catch
                    {
                        connectionError = true;
                    }
                }
                else 
                    connectionError = true;

                if (failedItems is null) // List doesn't exist or exception when calling to server
                {
                    connectionError = true;

                    foreach (var dataGrpcListItemWrapper in itemsToAdd_DataGrpcListItemWrappers)
                    {                        
                        if (!dataGrpcListItemWrapper.ConnectionError)
                        {
                            dataGrpcListItemWrapper.ConnectionError = true;
                            foreach (ClientObjectInfo clientObjectInfo in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                            {
                                clientObjectInfo.NotifyClientObj_AddItemResult = true;
                                clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = true;
                            }
                        }                        
                        dataGrpcListItemWrapper.FailedAddItemResult = null;
                        dataGrpcListItemWrapper.DataAccessGrpcListItem = null;
                    }
                }
                else
                {
                    foreach (TDataAccessGrpcListItem failedItem in failedItems)
                    {
                        var dataGrpcListItemWrapper = failedItem.Obj as DataAccessGrpcListItemWrapper;
                        if (dataGrpcListItemWrapper is null) throw new InvalidOperationException();
                        itemsToAdd_DataGrpcListItemWrappers.Remove(dataGrpcListItemWrapper);
                        if (dataGrpcListItemWrapper.FailedAddItemResult is null)
                        {
                            dataGrpcListItemWrapper.FailedAddItemResult = failedItem.AddItemResult!;
                            foreach (ClientObjectInfo clientObjectInfo in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                            {
                                clientObjectInfo.NotifyClientObj_AddItemResult = true;
                                clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = true;
                            }
                        }                        
                        dataGrpcListItemWrapper.ConnectionError = false;
                        dataGrpcListItemWrapper.DataAccessGrpcListItem = null;
                    }
                    foreach (var dataGrpcListItemWrapper in itemsToAdd_DataGrpcListItemWrappers)
                    {
                        if (dataGrpcListItemWrapper.DataAccessGrpcListItem is null)
                        {
                            if (dataGrpcListItemWrapper.FailedAddItemResult is null)
                            {
                                dataGrpcListItemWrapper.FailedAddItemResult = AddItemResult.UnknownAddItemResult;
                                foreach (ClientObjectInfo clientObjectInfo in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                                {
                                    clientObjectInfo.NotifyClientObj_AddItemResult = true;
                                    clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = true;
                                }
                            }
                            dataGrpcListItemWrapper.ConnectionError = false;
                        }
                        else
                        {
                            dataGrpcListItemWrapper.FailedAddItemResult = null;
                            dataGrpcListItemWrapper.ConnectionError = false;
                            foreach (ClientObjectInfo clientObjectInfo in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                            {
                                clientObjectInfo.NotifyClientObj_AddItemResult = true;
                                clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = false;
                            }
                        }                        
                    }
                }
            }

            if (_clientObjectInfosToRemove.Count > 0)
            {
                foreach (ClientObjectInfo clientObjectInfo in _clientObjectInfosToRemove)
                {
                    var dataGrpcListItemWrapper = clientObjectInfo.DataAccessGrpcListItemWrapper;
                    if (dataGrpcListItemWrapper is not null)
                    {
                        var clientObjectInfos = dataGrpcListItemWrapper.ClientObjectInfosCollection;
                        clientObjectInfos.Remove(clientObjectInfo);
                        clientObjectInfo.DataAccessGrpcListItemWrapper = null;
                        if (unsubscribeItemsFromServer)
                        {
                            // Remove DataAccessGrpc Item
                            var dataGrpcListItem = dataGrpcListItemWrapper.DataAccessGrpcListItem;
                            if (clientObjectInfos.Count == 0 && dataGrpcListItem is not null)
                            {
                                dataGrpcListItem.PrepareForRemove();
                                dataGrpcListItem.Obj = null;
                                dataGrpcListItemWrapper.DataAccessGrpcListItem = null;
                            }                            
                        }                        
                    }
                }

                if (unsubscribeItemsFromServer)
                {
                    if (!connectionError && DataAccessGrpcList is not null && !DataAccessGrpcList.Disposed)
                    {
                        try
                        {
                            DataAccessGrpcList.CommitRemoveItems();
                        }
                        catch
                        {
                            connectionError = true;
                        }
                    }
                    else 
                        connectionError = true;
                }
            }

            return connectionError;
        }        

        protected void SubscribeFinal()
        {
            _clientObjectInfosToRemove.Clear();
        }

        protected bool DataAccessGrpcItemsMustBeAddedOrRemoved
        {
            get { return _dataGrpcItemsMustBeAddedOrRemoved; }
            set { _dataGrpcItemsMustBeAddedOrRemoved = value; }
        }

        protected Dictionary<object, ClientObjectInfo> ClientObjectInfosDictionary
        {
            get { return _clientObjectInfosDictionary; }
        }

        protected CaseInsensitiveDictionary<DataAccessGrpcListItemWrapper> DataAccessGrpcListItemWrappersDictionary
        {
            get { return _dataGrpcListItemWrappersDictionary; }
        }

        protected object DataAccessGrpcListItemsDictionarySyncRoot = new object();

        #endregion

        #region private fields

        private readonly Dictionary<object, ClientObjectInfo> _clientObjectInfosDictionary =
            new Dictionary<object, ClientObjectInfo>(256, ReferenceEqualityComparer<object>.Default);

        private readonly CaseInsensitiveDictionary<DataAccessGrpcListItemWrapper> _dataGrpcListItemWrappersDictionary =
            new CaseInsensitiveDictionary<DataAccessGrpcListItemWrapper>(256);

        private volatile bool _dataGrpcItemsMustBeAddedOrRemoved;
        private readonly List<ClientObjectInfo> _clientObjectInfosToRemove = new List<ClientObjectInfo>(256);        

        #endregion

        public class DataAccessGrpcListItemWrapper
        {            
            public readonly List<ClientObjectInfo> ClientObjectInfosCollection = new List<ClientObjectInfo>();

            public TDataAccessGrpcListItem? DataAccessGrpcListItem { get; set; }

            public bool ConnectionError;

            public AddItemResult? FailedAddItemResult;
        }

        public class ClientObjectInfo
        {
            #region construction and destruction
            
            /// <summary>            
            /// </summary>
            /// <param name="elementId"></param>
            public ClientObjectInfo(string elementId)
            {
                ElementId = elementId;                
            }

            #endregion

            #region public functions
            
            public string ElementId { get; private set; }            
            
            /// <summary>
            ///     Has value after initialization till the end
            /// </summary>
            public DataAccessGrpcListItemWrapper? DataAccessGrpcListItemWrapper { get; set; }

            public object? ClientObj { get; set; }

            public bool NotifyClientObj_AddItemResult { get; set; }

            public bool NotifyClientObj_ValueStatusTimestamp { get; set; }

            #endregion
        }
    }
}