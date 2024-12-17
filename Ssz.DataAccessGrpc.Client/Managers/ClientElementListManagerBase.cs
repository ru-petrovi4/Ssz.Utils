using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using System.Threading.Tasks;

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
            foreach (var dataAccessGrpcListItemWrapper in _dataAccessGrpcListItemWrappersDictionary.Values)
            {
                dataAccessGrpcListItemWrapper.DataAccessGrpcListItem = null;
                dataAccessGrpcListItemWrapper.ConnectionError = false;
                dataAccessGrpcListItemWrapper.FailedAddItemResultInfo = null;
                if (clearClientSubscriptions) dataAccessGrpcListItemWrapper.ClientObjectInfosCollection.Clear();
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
        protected async Task<bool> SubscribeInitialAsync(bool unsubscribeItemsFromServer)
        {
            bool connectionError = false;
            
            foreach (ClientObjectInfo clientObjectInfo in _clientObjectInfosDictionary.Values)
            {
                if (clientObjectInfo.DataAccessGrpcListItemWrapper is null)
                {
                    var elementId = clientObjectInfo.ElementId;
                    DataAccessGrpcListItemWrapper? dataAccessGrpcListItemWrapper;
                    if (!_dataAccessGrpcListItemWrappersDictionary.TryGetValue(elementId, out dataAccessGrpcListItemWrapper))
                    {
                        dataAccessGrpcListItemWrapper = new DataAccessGrpcListItemWrapper();
                        _dataAccessGrpcListItemWrappersDictionary.Add(elementId, dataAccessGrpcListItemWrapper);
                    }
                    else
                    {
                        clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = true;
                    }                    
                    clientObjectInfo.DataAccessGrpcListItemWrapper = dataAccessGrpcListItemWrapper;
                    dataAccessGrpcListItemWrapper.ClientObjectInfosCollection.Add(clientObjectInfo);
                }                
            }

            var itemsToAdd_DataGrpcListItemWrappers = new List<DataAccessGrpcListItemWrapper>();
            foreach (var kvp in _dataAccessGrpcListItemWrappersDictionary)
            { 
                DataAccessGrpcListItemWrapper dataAccessGrpcListItemWrapper = kvp.Value;
                if (dataAccessGrpcListItemWrapper.DataAccessGrpcListItem is null && dataAccessGrpcListItemWrapper.FailedAddItemResultInfo is null)
                {
                    itemsToAdd_DataGrpcListItemWrappers.Add(dataAccessGrpcListItemWrapper);
                    TDataAccessGrpcListItem? dataAccessGrpcListItem = null;
                    if (DataAccessGrpcList is not null && !DataAccessGrpcList.Disposed)
                    {
                        try
                        {
                            dataAccessGrpcListItem = DataAccessGrpcList.PrepareAddItem(kvp.Key);
                        }
                        catch
                        {
                            connectionError = true;
                        }
                    }
                    else
                    {
                        connectionError = true;
                    }
                    if (dataAccessGrpcListItem is not null)
                    {
                        dataAccessGrpcListItem.Obj = dataAccessGrpcListItemWrapper;
                        dataAccessGrpcListItemWrapper.DataAccessGrpcListItem = dataAccessGrpcListItem;
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
                        failedItems = await DataAccessGrpcList.CommitAddItemsAsync();
                    }
                    catch
                    {
                        connectionError = true;
                    }
                }
                else
                {
                    connectionError = true;
                }

                if (failedItems is null) // List doesn't exist or exception when calling to server
                {
                    connectionError = true;

                    foreach (var dataAccessGrpcListItemWrapper in itemsToAdd_DataGrpcListItemWrappers)
                    {                        
                        if (!dataAccessGrpcListItemWrapper.ConnectionError)
                        {
                            dataAccessGrpcListItemWrapper.ConnectionError = true;
                            foreach (ClientObjectInfo clientObjectInfo in dataAccessGrpcListItemWrapper.ClientObjectInfosCollection)
                            {                                
                                clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = true;
                            }
                        }                        
                        dataAccessGrpcListItemWrapper.FailedAddItemResultInfo = null;
                        dataAccessGrpcListItemWrapper.DataAccessGrpcListItem = null;
                    }
                }
                else
                {
                    foreach (TDataAccessGrpcListItem failedItem in failedItems)
                    {
                        var dataAccessGrpcListItemWrapper = failedItem.Obj as DataAccessGrpcListItemWrapper;
                        if (dataAccessGrpcListItemWrapper is null) throw new InvalidOperationException();
                        itemsToAdd_DataGrpcListItemWrappers.Remove(dataAccessGrpcListItemWrapper);
                        if (dataAccessGrpcListItemWrapper.FailedAddItemResultInfo is null)
                        {
                            dataAccessGrpcListItemWrapper.FailedAddItemResultInfo = failedItem.AddItemResultInfo!;
                            foreach (ClientObjectInfo clientObjectInfo in dataAccessGrpcListItemWrapper.ClientObjectInfosCollection)
                            {                                
                                clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = true;
                            }
                        }                        
                        dataAccessGrpcListItemWrapper.ConnectionError = false;
                        dataAccessGrpcListItemWrapper.DataAccessGrpcListItem = null;
                    }
                    foreach (var dataAccessGrpcListItemWrapper in itemsToAdd_DataGrpcListItemWrappers)
                    {
                        if (dataAccessGrpcListItemWrapper.DataAccessGrpcListItem is null)
                        {
                            if (dataAccessGrpcListItemWrapper.FailedAddItemResultInfo is null)
                            {
                                dataAccessGrpcListItemWrapper.FailedAddItemResultInfo = ResultInfo.UncertainResultInfo;
                                foreach (ClientObjectInfo clientObjectInfo in dataAccessGrpcListItemWrapper.ClientObjectInfosCollection)
                                {                                    
                                    clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = true;
                                }
                            }
                            dataAccessGrpcListItemWrapper.ConnectionError = false;
                        }
                        else
                        {
                            dataAccessGrpcListItemWrapper.FailedAddItemResultInfo = null;
                            dataAccessGrpcListItemWrapper.ConnectionError = false;
                            foreach (ClientObjectInfo clientObjectInfo in dataAccessGrpcListItemWrapper.ClientObjectInfosCollection)
                            {                                
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
                    var dataAccessGrpcListItemWrapper = clientObjectInfo.DataAccessGrpcListItemWrapper;
                    if (dataAccessGrpcListItemWrapper is not null)
                    {
                        var clientObjectInfos = dataAccessGrpcListItemWrapper.ClientObjectInfosCollection;
                        clientObjectInfos.Remove(clientObjectInfo);
                        clientObjectInfo.DataAccessGrpcListItemWrapper = null;
                        if (unsubscribeItemsFromServer && clientObjectInfos.Count == 0)
                        {
                            // Remove DataAccessGrpc Item
                            _dataAccessGrpcListItemWrappersDictionary.Remove(clientObjectInfo.ElementId);
                            var dataAccessGrpcListItem = dataAccessGrpcListItemWrapper.DataAccessGrpcListItem;
                            if (dataAccessGrpcListItem is not null)
                            {
                                dataAccessGrpcListItem.PrepareForRemove();
                                dataAccessGrpcListItem.Obj = null;
                                dataAccessGrpcListItemWrapper.DataAccessGrpcListItem = null;
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
                            await DataAccessGrpcList.CommitRemoveItemsAsync();
                        }
                        catch
                        {
                            connectionError = true;
                        }
                    }
                    else
                    {
                        connectionError = true;
                    }
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

        protected Dictionary<string, DataAccessGrpcListItemWrapper> DataAccessGrpcListItemWrappersDictionary
        {
            get { return _dataAccessGrpcListItemWrappersDictionary; }
        }

        protected object DataAccessGrpcListItemsDictionarySyncRoot = new object();

        #endregion

        #region private fields

        private readonly Dictionary<object, ClientObjectInfo> _clientObjectInfosDictionary = new(256, ReferenceEqualityComparer<object>.Default);

        private readonly Dictionary<string, DataAccessGrpcListItemWrapper> _dataAccessGrpcListItemWrappersDictionary = new(256);

        private volatile bool _dataGrpcItemsMustBeAddedOrRemoved;
        private readonly List<ClientObjectInfo> _clientObjectInfosToRemove = new List<ClientObjectInfo>(256);        

        #endregion

        public class DataAccessGrpcListItemWrapper
        {            
            public readonly List<ClientObjectInfo> ClientObjectInfosCollection = new List<ClientObjectInfo>();

            public TDataAccessGrpcListItem? DataAccessGrpcListItem 
            { 
                get; 
                set; 
            }

            public bool ConnectionError;

            public ResultInfo? FailedAddItemResultInfo;
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

            public bool NotifyClientObj_ValueStatusTimestamp { get; set; }

            #endregion
        }
    }
}