using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Client.Core.Lists;
using Ssz.DataGrpc.Common;

namespace Ssz.DataGrpc.Client.Managers
{
    public abstract class ClientElementListManagerBase<TDataGrpcListItem, TDataGrpcList>
        where TDataGrpcListItem : ClientElementListItemBase
        where TDataGrpcList : ClientElementListBase<TDataGrpcListItem>
    {
        #region construction and destruction

        protected ClientElementListManagerBase(ILogger<DataGrpcProvider> logger, bool unsubscribeItemsFromServer)
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

            ModelItem? modelItem;
            if (!_modelItemsDictionary.TryGetValue(clientObj, out modelItem))
            {                
                modelItem = new ModelItem(elementId);
                _modelItemsDictionary.Add(clientObj, modelItem);
                modelItem.ClientObj = clientObj;

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
            ModelItem? modelItem;
            if (_modelItemsDictionary.TryGetValue(clientObj, out modelItem))
            {
                _modelItemsToRemove.Add(modelItem);
                _modelItemsDictionary.Remove(clientObj);
                modelItem.ClientObj = null;

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
                dataGrpcListItemWrapper.InvalidId = false;
            }            

            DataGrpcList = null;

            _modelItemsToRemove.Clear();

            _dataGrpcItemsMustBeAddedOrRemoved = true;
        }

        public IEnumerable<object> GetAllClientObjs()
        {
            return ModelItemsDictionary.Values.Select(mi => mi.ClientObj).Where(o => o != null).OfType<object>();
        }

        #endregion

        #region protected functions

        protected ILogger<DataGrpcProvider> Logger { get; }

        protected TDataGrpcList? DataGrpcList { get; set; }

        protected bool UnsubscribeItemsFromServer { get; }

        protected ModelItem? GetModelItem(object clientObj)
        {
            ModelItem? modelItem;
            _modelItemsDictionary.TryGetValue(clientObj, out modelItem);
            return modelItem;
        }

        protected IEnumerable<ModelItem> GetAllModelItems()
        {
            return ModelItemsDictionary.Values;
        }

        /// <summary>
        ///     Returns whether connection errors occur.
        /// </summary>
        /// <returns></returns>
        protected bool SubscribeInitial()
        {
            bool connectionError = false;
            
            foreach (ModelItem modelItem in _modelItemsDictionary.Values)
            {
                if (modelItem.DataGrpcListItemWrapper == null)
                {
                    var elementId = modelItem.ElementId;
                    DataGrpcListItemWrapper? dataGrpcListItemWrapper;
                    if (!_dataGrpcListItemWrappersDictionary.TryGetValue(elementId, out dataGrpcListItemWrapper))
                    {
                        dataGrpcListItemWrapper = new DataGrpcListItemWrapper();
                        _dataGrpcListItemWrappersDictionary.Add(elementId, dataGrpcListItemWrapper);
                    }
                    modelItem.ForceNotifyClientObj = true;
                    modelItem.DataGrpcListItemWrapper = dataGrpcListItemWrapper;
                    dataGrpcListItemWrapper.ModelItems.Add(modelItem);
                }                
            }

            var dataGrpcListItemWrappersToAdd = new List<DataGrpcListItemWrapper>();
            foreach (var kvp in _dataGrpcListItemWrappersDictionary)
            {
                DataGrpcListItemWrapper dataGrpcListItemWrapper = kvp.Value;
                if (dataGrpcListItemWrapper.DataGrpcListItem == null && !dataGrpcListItemWrapper.InvalidId)
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
                            foreach (ModelItem modelItem in dataGrpcListItemWrapper.ModelItems)
                            {
                                modelItem.ForceNotifyClientObj = true;
                            }
                        }                        
                        dataGrpcListItemWrapper.InvalidId = false;
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
                        if (!dataGrpcListItemWrapper.InvalidId)
                        {
                            dataGrpcListItemWrapper.InvalidId = true;
                            foreach (ModelItem modelItem in dataGrpcListItemWrapper.ModelItems)
                            {
                                modelItem.ForceNotifyClientObj = true;
                            }
                        }                        
                        dataGrpcListItemWrapper.ConnectionError = false;
                        dataGrpcListItemWrapper.DataGrpcListItem = null;
                    }
                    foreach (var dataGrpcListItemWrapper in dataGrpcListItemWrappersToAdd)
                    {
                        if (dataGrpcListItemWrapper.DataGrpcListItem == null)
                        {
                            if (!dataGrpcListItemWrapper.InvalidId)
                            {
                                dataGrpcListItemWrapper.InvalidId = true;
                                foreach (ModelItem modelItem in dataGrpcListItemWrapper.ModelItems)
                                {
                                    modelItem.ForceNotifyClientObj = true;
                                }
                            }
                            dataGrpcListItemWrapper.ConnectionError = false;
                        }
                        else
                        {
                            dataGrpcListItemWrapper.InvalidId = false;
                            dataGrpcListItemWrapper.ConnectionError = false;
                            foreach (ModelItem modelItem in dataGrpcListItemWrapper.ModelItems)
                            {
                                modelItem.ForceNotifyClientObj = false;
                            }
                        }                        
                    }
                }
            }

            if (_modelItemsToRemove.Count > 0)
            {
                foreach (ModelItem modelItem in _modelItemsToRemove)
                {
                    var dataGrpcListItemWrapper = modelItem.DataGrpcListItemWrapper;
                    if (dataGrpcListItemWrapper != null)
                    {
                        var modelItems = dataGrpcListItemWrapper.ModelItems;
                        modelItems.Remove(modelItem);
                        modelItem.DataGrpcListItemWrapper = null;
                        if (UnsubscribeItemsFromServer)
                        {
                            // Remove DataGrpc Item
                            var dataGrpcListItem = dataGrpcListItemWrapper.DataGrpcListItem;
                            if (modelItems.Count == 0 && dataGrpcListItem != null)
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
            _modelItemsToRemove.Clear();
        }

        protected bool DataGrpcItemsMustBeAddedOrRemoved
        {
            get { return _dataGrpcItemsMustBeAddedOrRemoved; }
            set { _dataGrpcItemsMustBeAddedOrRemoved = value; }
        }

        protected Dictionary<object, ModelItem> ModelItemsDictionary
        {
            get { return _modelItemsDictionary; }
        }

        protected CaseInsensitiveDictionary<DataGrpcListItemWrapper> DataGrpcListItemWrappersDictionary
        {
            get { return _dataGrpcListItemWrappersDictionary; }
        }

        protected object DataGrpcListItemsDictionarySyncRoot = new object();

        #endregion

        #region private fields

        private readonly Dictionary<object, ModelItem> _modelItemsDictionary =
            new Dictionary<object, ModelItem>(256, ReferenceEqualityComparer<object>.Default);

        private readonly CaseInsensitiveDictionary<DataGrpcListItemWrapper> _dataGrpcListItemWrappersDictionary =
            new CaseInsensitiveDictionary<DataGrpcListItemWrapper>(256);

        private volatile bool _dataGrpcItemsMustBeAddedOrRemoved;
        private readonly List<ModelItem> _modelItemsToRemove = new List<ModelItem>(256);        

        #endregion

        public class DataGrpcListItemWrapper
        {            
            public readonly List<ModelItem> ModelItems = new List<ModelItem>();

            public TDataGrpcListItem? DataGrpcListItem { get; set; }

            public bool ConnectionError;

            public bool InvalidId;
        }

        public class ModelItem
        {
            #region construction and destruction
            
            /// <summary>            
            /// </summary>
            /// <param name="elementId"></param>
            public ModelItem(string elementId)
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