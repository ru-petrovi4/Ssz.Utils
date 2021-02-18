using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        ///     id != null, valueSubscription != null
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clientObj"></param>
        public void AddItem(string id, object clientObj)
        {
            if (id == null) throw new ArgumentNullException(@"id");

            Logger.Verbose("XiListItemsManager.AddItem() " + id); 

            ModelItem? modelItem;
            if (!_modelItemsDictionary.TryGetValue(clientObj, out modelItem))
            {                
                modelItem = new ModelItem(id);
                _modelItemsDictionary.Add(clientObj, modelItem);
                modelItem.ClientObj = clientObj;

                _xiItemsMustBeAddedOrRemoved = true;
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

                _xiItemsMustBeAddedOrRemoved = true;
            }
        }

        public void Unsubscribe()
        {
            foreach (var xiListItemWrapper in _xiListItemWrappersDictionary.Values)
            {
                xiListItemWrapper.XiListItem = null;
                xiListItemWrapper.ConnectionError = false;
                xiListItemWrapper.InvalidId = false;
            }            

            XiList = null;

            _modelItemsToRemove.Clear();

            _xiItemsMustBeAddedOrRemoved = true;
        }

        public abstract InstanceId GetInstanceId(string id);

        public IEnumerable<ModelItem> GetAllModelItems()
        {
            return ModelItemsDictionary.Values;
        }

        public ModelItem? GetModelItem(object clientObj)
        {
            ModelItem? modelItem;
            _modelItemsDictionary.TryGetValue(clientObj, out modelItem);
            return modelItem;
        }

        public IEnumerable<object> GetAllClientObjs()
        {
            return ModelItemsDictionary.Values.Select(mi => mi.ClientObj).Where(o => o != null).OfType<object>();
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
        protected bool SubscribeInitial()
        {
            bool connectionError = false;
            
            foreach (ModelItem modelItem in _modelItemsDictionary.Values)
            {
                if (modelItem.XiListItemWrapper == null)
                {
                    var id = modelItem.Id;
                    XiListItemWrapper? xiListItemWrapper;
                    if (!_xiListItemWrappersDictionary.TryGetValue(id, out xiListItemWrapper))
                    {
                        xiListItemWrapper = new XiListItemWrapper();
                        _xiListItemWrappersDictionary.Add(id, xiListItemWrapper);
                    }
                    modelItem.ForceNotifyClientObj = true;
                    modelItem.XiListItemWrapper = xiListItemWrapper;
                    xiListItemWrapper.ModelItems.Add(modelItem);
                }                
            }

            var xiListItemWrappersToAdd = new List<XiListItemWrapper>();
            foreach (var kvp in _xiListItemWrappersDictionary)
            {
                XiListItemWrapper xiListItemWrapper = kvp.Value;
                if (xiListItemWrapper.XiListItem == null && !xiListItemWrapper.InvalidId)
                {
                    xiListItemWrappersToAdd.Add(xiListItemWrapper);
                    TXiListItem? xiListItem = null;
                    if (XiList != null && !XiList.Disposed)
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
                    if (xiListItem != null)
                    {
                        xiListItem.Obj = xiListItemWrapper;
                        xiListItemWrapper.XiListItem = xiListItem;
                    }                    
                }                
            }

            if (xiListItemWrappersToAdd.Count > 0)
            {
                IEnumerable<TXiListItem>? notAddedXiListItems = null;
                if (!connectionError && XiList != null && !XiList.Disposed)
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

                if (notAddedXiListItems == null) // List doesn't exist or exception when calling to server
                {
                    connectionError = true;

                    foreach (var xiListItemWrapper in xiListItemWrappersToAdd)
                    {                        
                        if (!xiListItemWrapper.ConnectionError)
                        {
                            xiListItemWrapper.ConnectionError = true;
                            foreach (ModelItem modelItem in xiListItemWrapper.ModelItems)
                            {
                                modelItem.ForceNotifyClientObj = true;
                            }
                        }                        
                        xiListItemWrapper.InvalidId = false;
                        xiListItemWrapper.XiListItem = null;
                    }
                }
                else
                {
                    foreach (TXiListItem notAddedXiListItem in notAddedXiListItems)
                    {
                        var xiListItemWrapper = notAddedXiListItem.Obj as XiListItemWrapper;
                        if (xiListItemWrapper == null) throw new InvalidOperationException();
                        xiListItemWrappersToAdd.Remove(xiListItemWrapper);
                        if (!xiListItemWrapper.InvalidId)
                        {
                            xiListItemWrapper.InvalidId = true;
                            foreach (ModelItem modelItem in xiListItemWrapper.ModelItems)
                            {
                                modelItem.ForceNotifyClientObj = true;
                            }
                        }                        
                        xiListItemWrapper.ConnectionError = false;
                        xiListItemWrapper.XiListItem = null;
                    }
                    foreach (var xiListItemWrapper in xiListItemWrappersToAdd)
                    {
                        if (xiListItemWrapper.XiListItem == null)
                        {
                            if (!xiListItemWrapper.InvalidId)
                            {
                                xiListItemWrapper.InvalidId = true;
                                foreach (ModelItem modelItem in xiListItemWrapper.ModelItems)
                                {
                                    modelItem.ForceNotifyClientObj = true;
                                }
                            }
                            xiListItemWrapper.ConnectionError = false;
                        }
                        else
                        {
                            xiListItemWrapper.InvalidId = false;
                            xiListItemWrapper.ConnectionError = false;
                            foreach (ModelItem modelItem in xiListItemWrapper.ModelItems)
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
                    var xiListItemWrapper = modelItem.XiListItemWrapper;
                    if (xiListItemWrapper != null)
                    {
                        var modelItems = xiListItemWrapper.ModelItems;
                        modelItems.Remove(modelItem);
                        modelItem.XiListItemWrapper = null;
                        /* // Remove Xi Item
                        var xiListItem = xiListItemWrapper.XiListItem;
                        if (modelItems.Count == 0 && xiListItem != null)
                        {
                            xiListItem.PrepareForRemove();
                            xiListItem.Obj = null;                            
                        }
                        xiListItemWrapper.XiListItem = null;*/
                    }
                }
                /*
                if (!connectionError && XiList != null && !XiList.Disposed)
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
                else connectionError = true;*/
            }

            return connectionError;
        }        

        protected void SubscribeFinal()
        {
            _modelItemsToRemove.Clear();
        }

        protected bool XiItemsMustBeAddedOrRemoved
        {
            get { return _xiItemsMustBeAddedOrRemoved; }
            set { _xiItemsMustBeAddedOrRemoved = value; }
        }

        protected Dictionary<object, ModelItem> ModelItemsDictionary
        {
            get { return _modelItemsDictionary; }
        }

        protected CaseInsensitiveDictionary<XiListItemWrapper> XiListItemWrappersDictionary
        {
            get { return _xiListItemWrappersDictionary; }
        }

        protected object XiListItemsDictionarySyncRoot = new object();

        #endregion

        #region private fields

        private readonly Dictionary<object, ModelItem> _modelItemsDictionary =
            new Dictionary<object, ModelItem>(256, ReferenceEqualityComparer<object>.Default);

        private readonly CaseInsensitiveDictionary<XiListItemWrapper> _xiListItemWrappersDictionary =
            new CaseInsensitiveDictionary<XiListItemWrapper>(256);

        private volatile bool _xiItemsMustBeAddedOrRemoved;
        private readonly List<ModelItem> _modelItemsToRemove = new List<ModelItem>(256);        

        #endregion

        public class XiListItemWrapper
        {            
            public readonly List<ModelItem> ModelItems = new List<ModelItem>();

            public TXiListItem? XiListItem { get; set; }

            public bool ConnectionError;

            public bool InvalidId;
        }

        public class ModelItem
        {
            #region construction and destruction
            
            /// <summary>            
            /// </summary>
            /// <param name="id"></param>
            public ModelItem(string id)
            {
                Id = id;                
            }

            #endregion

            #region public functions
            
            public string Id { get; private set; }
            
            public XiListItemWrapper? XiListItemWrapper { get; set; }

            public object? ClientObj { get; set; }

            public bool ForceNotifyClientObj { get; set; }

            #endregion
        }
    }
}