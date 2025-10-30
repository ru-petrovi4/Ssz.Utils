using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Ssz.Xi.Client.Internal.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    public abstract class XiListItemsManager<TXiListItem, TXiListProxy>
        where TXiListItem : class, IXiListItem
        where TXiListProxy : class, IXiListProxy<TXiListItem>
    {
        #region public functions

        public ILogger? Logger { get; set; }

        /// <summary>
        ///     id is not null, valueSubscription is not null
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientObj"></param>
        public void AddItem(string id, object clientObj)
        {
            Logger?.LogDebug("XiListItemsManager.AddItem() " + id);

            ClientObjectInfo? modelItem;
            if (!_clientObjectInfosDictionary.TryGetValue(clientObj, out modelItem))
            {
                modelItem = new ClientObjectInfo(id);
                _clientObjectInfosDictionary.Add(clientObj, modelItem);
                modelItem.ClientObj = clientObj;

                _xiItemsMustBeAddedOrRemoved = true;
            }
        }

        /// <summary>
        ///     valueSubscription is not null
        ///     If valueSubscription is not subscribed - does nothing. 
        /// </summary>
        /// <param name="clientObj"></param>
        public void RemoveItem(object clientObj)
        {
            ClientObjectInfo? modelItem;
            if (_clientObjectInfosDictionary.TryGetValue(clientObj, out modelItem))
            {
                _clientObjectInfosToRemove.Add(modelItem);
                _clientObjectInfosDictionary.Remove(clientObj);
                modelItem.ClientObj = null;

                _xiItemsMustBeAddedOrRemoved = true;
            }
        }

        public void Unsubscribe(bool clearClientSubscriptions)
        {
            foreach (var dataListItemWrapper in _xiListItemWrappersDictionary.Values)
            {
                dataListItemWrapper.XiListItem = null;
                dataListItemWrapper.ConnectionError = false;
                dataListItemWrapper.ItemDoesNotExist = false;
                if (clearClientSubscriptions) dataListItemWrapper.ClientObjectInfosCollection.Clear();
            }

            if (clearClientSubscriptions) _clientObjectInfosDictionary.Clear();

            XiList = null;

            _clientObjectInfosToRemove.Clear();

            _xiItemsMustBeAddedOrRemoved = true;
        }

        public abstract InstanceId GetInstanceId(string id);

        public ClientObjectInfo? GetClientObjectInfo(object clientObj)
        {
            ClientObjectInfo? clientObjectInfo;
            _clientObjectInfosDictionary.TryGetValue(clientObj, out clientObjectInfo);
            return clientObjectInfo;
        }

        public IEnumerable<object> GetAllClientObjs()
        {
            return ClientObjectInfosDictionary.Values.Select(mi => mi.ClientObj).Where(o => o is not null).OfType<object>();
        }

        /// <summary>
        ///     Xi Alias
        /// </summary>
        public string XiSystem { get; set; } = "";

        public TXiListProxy? XiList { get; set; }

        #endregion

        #region protected functions

        /// <summary>
        ///     Returns whether connection errors occur.
        /// </summary>
        /// <returns></returns>
        protected bool SubscribeInitial(bool unsubscribeItemsFromServer)
        {
            bool connectionError = false;

            foreach (ClientObjectInfo modelItem in _clientObjectInfosDictionary.Values)
            {
                if (modelItem.XiListItemWrapper is null)
                {
                    var id = modelItem.ElementId;
                    XiListItemWrapper? xiListItemWrapper;
                    if (!_xiListItemWrappersDictionary.TryGetValue(id, out xiListItemWrapper))
                    {
                        xiListItemWrapper = new XiListItemWrapper();
                        _xiListItemWrappersDictionary.Add(id, xiListItemWrapper);
                    }
                    modelItem.ForceNotifyClientObj = true;
                    modelItem.XiListItemWrapper = xiListItemWrapper;
                    xiListItemWrapper.ClientObjectInfosCollection.Add(modelItem);
                }
            }

            var xiListItemWrappersToAdd = new List<XiListItemWrapper>();
            foreach (var kvp in _xiListItemWrappersDictionary)
            {
                XiListItemWrapper xiListItemWrapper = kvp.Value;
                if (xiListItemWrapper.XiListItem is null && !xiListItemWrapper.ItemDoesNotExist)
                {
                    xiListItemWrappersToAdd.Add(xiListItemWrapper);
                    TXiListItem? xiListItem = null;
                    if (XiList is not null && !XiList.Disposed)
                    {
                        try
                        {
                            xiListItem = XiList.PrepareAddItem(GetInstanceId(kvp.Key));
                        }
                        catch
                        {
                            connectionError = true;
                        }
                    }
                    else connectionError = true;
                    if (xiListItem is not null)
                    {
                        xiListItem.Obj = xiListItemWrapper;
                        xiListItemWrapper.XiListItem = xiListItem;
                    }
                }
            }

            if (xiListItemWrappersToAdd.Count > 0)
            {
                IEnumerable<TXiListItem>? notAddedXiListItems = null;
                if (!connectionError && XiList is not null && !XiList.Disposed)
                {
                    try
                    {
                        notAddedXiListItems = XiList.CommitAddItems();
                    }
                    catch
                    {
                        connectionError = true;
                    }
                }
                else connectionError = true;

                if (notAddedXiListItems is null) // List doesn't exist or exception when calling to server
                {
                    connectionError = true;

                    foreach (var xiListItemWrapper in xiListItemWrappersToAdd)
                    {
                        if (!xiListItemWrapper.ConnectionError)
                        {
                            xiListItemWrapper.ConnectionError = true;
                            foreach (ClientObjectInfo modelItem in xiListItemWrapper.ClientObjectInfosCollection)
                            {
                                modelItem.ForceNotifyClientObj = true;
                            }
                        }
                        xiListItemWrapper.ItemDoesNotExist = false;
                        xiListItemWrapper.XiListItem = null;
                    }
                }
                else
                {
                    foreach (TXiListItem notAddedXiListItem in notAddedXiListItems)
                    {
                        var xiListItemWrapper = notAddedXiListItem.Obj as XiListItemWrapper;
                        if (xiListItemWrapper is null) throw new InvalidOperationException();
                        xiListItemWrappersToAdd.Remove(xiListItemWrapper);
                        if (!xiListItemWrapper.ItemDoesNotExist)
                        {
                            xiListItemWrapper.ItemDoesNotExist = true;
                            foreach (ClientObjectInfo modelItem in xiListItemWrapper.ClientObjectInfosCollection)
                            {
                                modelItem.ForceNotifyClientObj = true;
                            }
                        }
                        xiListItemWrapper.ConnectionError = false;
                        xiListItemWrapper.XiListItem = null;
                    }
                    foreach (var xiListItemWrapper in xiListItemWrappersToAdd)
                    {
                        if (xiListItemWrapper.XiListItem is null)
                        {
                            if (!xiListItemWrapper.ItemDoesNotExist)
                            {
                                xiListItemWrapper.ItemDoesNotExist = true;
                                foreach (ClientObjectInfo modelItem in xiListItemWrapper.ClientObjectInfosCollection)
                                {
                                    modelItem.ForceNotifyClientObj = true;
                                }
                            }
                            xiListItemWrapper.ConnectionError = false;
                        }
                        else
                        {
                            xiListItemWrapper.ItemDoesNotExist = false;
                            xiListItemWrapper.ConnectionError = false;
                            foreach (ClientObjectInfo modelItem in xiListItemWrapper.ClientObjectInfosCollection)
                            {
                                modelItem.ForceNotifyClientObj = false;
                            }
                        }
                    }
                }
            }

            if (_clientObjectInfosToRemove.Count > 0)
            {
                foreach (ClientObjectInfo clientObjectInfo in _clientObjectInfosToRemove)
                {
                    var xiListItemWrapper = clientObjectInfo.XiListItemWrapper;
                    if (xiListItemWrapper is not null)
                    {
                        var modelItems = xiListItemWrapper.ClientObjectInfosCollection;
                        modelItems.Remove(clientObjectInfo);
                        clientObjectInfo.XiListItemWrapper = null;
                        if (unsubscribeItemsFromServer)
                        {
                            // Remove Xi Item                        
                            if (modelItems.Count == 0)
                            {
                                _xiListItemWrappersDictionary.Remove(clientObjectInfo.ElementId);
                                var xiListItem = xiListItemWrapper.XiListItem;
                                if (xiListItem is not null)
                                {
                                    xiListItem.PrepareForRemove();
                                    xiListItem.Obj = null;
                                }
                            }
                            xiListItemWrapper.XiListItem = null;
                        }
                    }
                }
                if (unsubscribeItemsFromServer)
                {
                    if (!connectionError && XiList is not null && !XiList.Disposed)
                    {
                        try
                        {
                            XiList.CommitRemoveItems();
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

        protected bool XiItemsMustBeAddedOrRemoved
        {
            get { return _xiItemsMustBeAddedOrRemoved; }
            set { _xiItemsMustBeAddedOrRemoved = value; }
        }

        protected Dictionary<object, ClientObjectInfo> ClientObjectInfosDictionary
        {
            get { return _clientObjectInfosDictionary; }
        }

        protected CaseInsensitiveOrderedDictionary<XiListItemWrapper> XiListItemWrappersDictionary
        {
            get { return _xiListItemWrappersDictionary; }
        }

        protected object XiListItemsDictionarySyncRoot = new object();

        #endregion

        #region private fields

        private readonly Dictionary<object, ClientObjectInfo> _clientObjectInfosDictionary =
            new Dictionary<object, ClientObjectInfo>(256, ReferenceEqualityComparer<object>.Default);

        private readonly CaseInsensitiveOrderedDictionary<XiListItemWrapper> _xiListItemWrappersDictionary =
            new CaseInsensitiveOrderedDictionary<XiListItemWrapper>(256);

        private volatile bool _xiItemsMustBeAddedOrRemoved;
        private readonly List<ClientObjectInfo> _clientObjectInfosToRemove = new List<ClientObjectInfo>(256);

        #endregion        

        public class XiListItemWrapper
        {
            public readonly List<ClientObjectInfo> ClientObjectInfosCollection = new List<ClientObjectInfo>();

            public TXiListItem? XiListItem { get; set; }

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

            public XiListItemWrapper? XiListItemWrapper { get; set; }

            public object? ClientObj { get; set; }

            public bool ForceNotifyClientObj { get; set; }

            #endregion
        }
    }
}