using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.DataGrpc.Common;

namespace Ssz.DataGrpc.Client.Managers
{
    public abstract class ClientElementListManagerBase<TDataGrpcListItem, TDataGrpcList>
        where TDataGrpcListItem : ClientElementListItemBase
        where TDataGrpcList : ClientElementListBase<TDataGrpcListItem>
    {
        #region construction and destruction

        protected ClientElementListManagerBase(ILogger<GrpcDataAccessProvider> logger, bool unsubscribeItemsFromServer)
        {
            Logger = logger;
            UnsubscribeItemsFromServer = unsubscribeItemsFromServer;
        }

        #endregion

        #region public functions

        /// <summary>        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="clientObj"></param>
        public void AddItem(string elementId, object clientObj)
        {
            Logger.LogDebug("DataGrpcListItemsManager.AddItem() " + elementId); 

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
                Logger.LogError("DataGrpcListItemsManager.AddItem() error, duplicate clientObj " + elementId);
            }
        }

        /// <summary>
        ///     valueSubscription != null
        ///     If valueSubscription is not subscribed - does nothing. 
        /// </summary>
        /// <param name="clientObj"></param>
        public void RemoveItem(object clientObj)
        {
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
                Logger.LogError("DataGrpcListItemsManager.RemoveItem() error, clientObj did not add earlier");
            }
        }

        public void Unsubscribe()
        {
            foreach (var dataGrpcListItemWrapper in _dataGrpcListItemWrappersDictionary.Values)
            {
                dataGrpcListItemWrapper.DataGrpcListItem = null;
                dataGrpcListItemWrapper.ConnectionError = false;
                dataGrpcListItemWrapper.ItemDoesNotExist = false;
            }            

            DataGrpcList = null;

            _clientObjectInfosToRemove.Clear();

            _dataGrpcItemsMustBeAddedOrRemoved = true;
        }

        public IEnumerable<object> GetAllClientObjs()
        {
            return ModelItemsDictionary.Values.Select(mi => mi.ClientObj).Where(o => o != null).OfType<object>();
        }

        #endregion

        #region protected functions

        protected ILogger<GrpcDataAccessProvider> Logger { get; }

        protected TDataGrpcList? DataGrpcList { get; set; }

        protected bool UnsubscribeItemsFromServer { get; }

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
        protected bool SubscribeInitial()
        {
            bool connectionError = false;
            
            foreach (ClientObjectInfo clientObjectInfo in _clientObjectInfosDictionary.Values)
            {
                if (clientObjectInfo.DataGrpcListItemWrapper == null)
                {
                    var elementId = clientObjectInfo.ElementId;
                    DataGrpcListItemWrapper? dataGrpcListItemWrapper;
                    if (!_dataGrpcListItemWrappersDictionary.TryGetValue(elementId, out dataGrpcListItemWrapper))
                    {
                        dataGrpcListItemWrapper = new DataGrpcListItemWrapper();
                        _dataGrpcListItemWrappersDictionary.Add(elementId, dataGrpcListItemWrapper);
                    }
                    clientObjectInfo.ForceNotifyClientObj = true;
                    clientObjectInfo.DataGrpcListItemWrapper = dataGrpcListItemWrapper;
                    dataGrpcListItemWrapper.ClientObjectInfosCollection.Add(clientObjectInfo);
                }                
            }

            var dataGrpcListItemWrappersToAdd = new List<DataGrpcListItemWrapper>();
            foreach (var kvp in _dataGrpcListItemWrappersDictionary)
            {
                DataGrpcListItemWrapper dataGrpcListItemWrapper = kvp.Value;
                if (dataGrpcListItemWrapper.DataGrpcListItem == null && !dataGrpcListItemWrapper.ItemDoesNotExist)
                {
                    dataGrpcListItemWrappersToAdd.Add(dataGrpcListItemWrapper);
                    TDataGrpcListItem? dataGrpcListItem = null;
                    if (DataGrpcList != null && !DataGrpcList.Disposed)
                    {
                        try
                        {
                            dataGrpcListItem = DataGrpcList.PrepareAddItem(kvp.Key);
                        }
                        catch
                        {
                            connectionError = true;
                        }
                    }
                    else connectionError = true;
                    if (dataGrpcListItem != null)
                    {
                        dataGrpcListItem.Obj = dataGrpcListItemWrapper;
                        dataGrpcListItemWrapper.DataGrpcListItem = dataGrpcListItem;
                    }                    
                }                
            }

            if (dataGrpcListItemWrappersToAdd.Count > 0)
            {
                IEnumerable<TDataGrpcListItem>? notAddedDataGrpcListItems = null;
                if (!connectionError && DataGrpcList != null && !DataGrpcList.Disposed)
                {
                    try
                    {
                        notAddedDataGrpcListItems = DataGrpcList.CommitAddItems();
                    }
                    catch
                    {
                        connectionError = true;
                    }
                }
                else connectionError = true;

                if (notAddedDataGrpcListItems == null) // List doesn't exist or exception when calling to server
                {
                    connectionError = true;

                    foreach (var dataGrpcListItemWrapper in dataGrpcListItemWrappersToAdd)
                    {                        
                        if (!dataGrpcListItemWrapper.ConnectionError)
                        {
                            dataGrpcListItemWrapper.ConnectionError = true;
                            foreach (ClientObjectInfo clientObjectInfo in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                            {
                                clientObjectInfo.ForceNotifyClientObj = true;
                            }
                        }                        
                        dataGrpcListItemWrapper.ItemDoesNotExist = false;
                        dataGrpcListItemWrapper.DataGrpcListItem = null;
                    }
                }
                else
                {
                    foreach (TDataGrpcListItem notAddedDataGrpcListItem in notAddedDataGrpcListItems)
                    {
                        var dataGrpcListItemWrapper = notAddedDataGrpcListItem.Obj as DataGrpcListItemWrapper;
                        if (dataGrpcListItemWrapper == null) throw new InvalidOperationException();
                        dataGrpcListItemWrappersToAdd.Remove(dataGrpcListItemWrapper);
                        if (!dataGrpcListItemWrapper.ItemDoesNotExist)
                        {
                            dataGrpcListItemWrapper.ItemDoesNotExist = true;
                            foreach (ClientObjectInfo clientObjectInfo in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                            {
                                clientObjectInfo.ForceNotifyClientObj = true;
                            }
                        }                        
                        dataGrpcListItemWrapper.ConnectionError = false;
                        dataGrpcListItemWrapper.DataGrpcListItem = null;
                    }
                    foreach (var dataGrpcListItemWrapper in dataGrpcListItemWrappersToAdd)
                    {
                        if (dataGrpcListItemWrapper.DataGrpcListItem == null)
                        {
                            if (!dataGrpcListItemWrapper.ItemDoesNotExist)
                            {
                                dataGrpcListItemWrapper.ItemDoesNotExist = true;
                                foreach (ClientObjectInfo clientObjectInfo in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                                {
                                    clientObjectInfo.ForceNotifyClientObj = true;
                                }
                            }
                            dataGrpcListItemWrapper.ConnectionError = false;
                        }
                        else
                        {
                            dataGrpcListItemWrapper.ItemDoesNotExist = false;
                            dataGrpcListItemWrapper.ConnectionError = false;
                            foreach (ClientObjectInfo clientObjectInfo in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                            {
                                clientObjectInfo.ForceNotifyClientObj = false;
                            }
                        }                        
                    }
                }
            }

            if (_clientObjectInfosToRemove.Count > 0)
            {
                foreach (ClientObjectInfo clientObjectInfo in _clientObjectInfosToRemove)
                {
                    var dataGrpcListItemWrapper = clientObjectInfo.DataGrpcListItemWrapper;
                    if (dataGrpcListItemWrapper != null)
                    {
                        var clientObjectInfos = dataGrpcListItemWrapper.ClientObjectInfosCollection;
                        clientObjectInfos.Remove(clientObjectInfo);
                        clientObjectInfo.DataGrpcListItemWrapper = null;
                        if (UnsubscribeItemsFromServer)
                        {
                            // Remove DataGrpc Item
                            var dataGrpcListItem = dataGrpcListItemWrapper.DataGrpcListItem;
                            if (clientObjectInfos.Count == 0 && dataGrpcListItem != null)
                            {
                                dataGrpcListItem.PrepareForRemove();
                                dataGrpcListItem.Obj = null;
                            }
                            dataGrpcListItemWrapper.DataGrpcListItem = null;
                        }                        
                    }
                }

                if (UnsubscribeItemsFromServer)
                {
                    if (!connectionError && DataGrpcList != null && !DataGrpcList.Disposed)
                    {
                        try
                        {
                            DataGrpcList.CommitRemoveItems();
                        }
                        catch
                        {
                            connectionError = true;
                        }
                    }
                    else connectionError = true;
                }
            }

            return connectionError;
        }        

        protected void SubscribeFinal()
        {
            _clientObjectInfosToRemove.Clear();
        }

        protected bool DataGrpcItemsMustBeAddedOrRemoved
        {
            get { return _dataGrpcItemsMustBeAddedOrRemoved; }
            set { _dataGrpcItemsMustBeAddedOrRemoved = value; }
        }

        protected Dictionary<object, ClientObjectInfo> ModelItemsDictionary
        {
            get { return _clientObjectInfosDictionary; }
        }

        protected CaseInsensitiveDictionary<DataGrpcListItemWrapper> DataGrpcListItemWrappersDictionary
        {
            get { return _dataGrpcListItemWrappersDictionary; }
        }

        protected object DataGrpcListItemsDictionarySyncRoot = new object();

        #endregion

        #region private fields

        private readonly Dictionary<object, ClientObjectInfo> _clientObjectInfosDictionary =
            new Dictionary<object, ClientObjectInfo>(256, ReferenceEqualityComparer.Instance);

        private readonly CaseInsensitiveDictionary<DataGrpcListItemWrapper> _dataGrpcListItemWrappersDictionary =
            new CaseInsensitiveDictionary<DataGrpcListItemWrapper>(256);

        private volatile bool _dataGrpcItemsMustBeAddedOrRemoved;
        private readonly List<ClientObjectInfo> _clientObjectInfosToRemove = new List<ClientObjectInfo>(256);        

        #endregion

        public class DataGrpcListItemWrapper
        {            
            public readonly List<ClientObjectInfo> ClientObjectInfosCollection = new List<ClientObjectInfo>();

            public TDataGrpcListItem? DataGrpcListItem { get; set; }

            public bool ConnectionError;

            public bool ItemDoesNotExist;
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
            
            public DataGrpcListItemWrapper? DataGrpcListItemWrapper { get; set; }

            public object? ClientObj { get; set; }

            public bool ForceNotifyClientObj { get; set; }

            #endregion
        }
    }
}