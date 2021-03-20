using System;
using System.Collections.Generic;
using Ssz.Utils;
using Ssz.DataGrpc.Server.Core.Context;


namespace Ssz.DataGrpc.Server.Core.Lists
{
    /// <summary>
    ///   This class is intended to be used as the root or base class for 
    ///   all list types.  The attribute, properties and methods defined 
    ///   in this class are common to all list types.
    /// </summary>
    public abstract class ServerListRoot
    {
        #region construction and destruction

        /// <summary>
        ///   Constructs a new instance of the <see cref = "ServerListRoot" /> class.
        /// </summary>
        /// <param name = "context"></param>
        /// <param name = "clientId"></param>
        /// <param name = "updateRate"></param>
        /// <param name = "bufferingRate"></param>
        /// <param name = "listType"></param>
        /// <param name = "listKey"></param>
        /// <param name = "mib"></param>
        public ServerListRoot(ServerContext<ServerListRoot> context, uint clientId, uint updateRate, uint bufferingRate, uint listType,
                        uint listKey, StandardMib mib)
        {
            Context = context;
            ServerId = listKey;
            ClientId = clientId;
            ListType = listType;
            UpdateRate = updateRate; // to be negotiated later
            BufferingRate = NegotiateBufferingRate(mib, bufferingRate);
            NumberOfUpdateCyclesToQueue = 2;
        }

        /// <summary>
        ///   This is the implementation of the IDisposable.Dispose method.  The client 
        ///   application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            using (SyncRoot.Enter())
            {
                Dispose(true);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   This method is invoked when the IDisposable.Dispose or Finalize actions are 
        ///   requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                // Release and Dispose managed resources.
            }
            // Release unmanaged resources.
            // Set large fields to null.  
            Context = null;

            Disposed = true;
        }

        /// <summary>
        ///   Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~ServerListRoot()
        {
            Dispose(false);
        }

        #endregion

        /*
		public void Func()
		{
			if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ListRoot.");
		}
		*/

        #region public functions

        public virtual void Process()
        {
            /*
            lock (ListLock)
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ListRoot.");
            }*/
        }

        public virtual List<AddDataObjectResult> OnAddDataObjectsToList(List<ListInstanceId> dataObjectsToAdd)
        {
            throw RpcExceptionHelper.Create("Invalid List Type for this Request");
        }

        public virtual List<AliasResult> OnRemoveDataObjectsFromList(List<uint> serverAliasesToDelete)
        {
            throw RpcExceptionHelper.Create("Invalid List Type for this Request");
        }        

        /// <summary>
        ///   Normally this method will be overridden in the implementation 
        ///   subclass to perform any actions in changed the state of this 
        ///   list and the specified data values to the requested updating state.
        /// </summary>
        /// <param name = "enableUpdating"></param>
        public virtual ListAttributes OnEnableListUpdating(bool enableUpdating)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ListRoot.");

                Enabled = enableUpdating;
                return ListAttributesInternal;
            }
        }

        public virtual uint OnTouchList()
        {
            throw RpcExceptionHelper.Create("Invalid List Type for this Request");
        }

        public virtual DataValueArraysWithAlias OnPollDataChanges()
        {
            throw RpcExceptionHelper.Create("Invalid List Type for this Request");
        }

        public virtual EventMessage[] OnPollEventChanges()
        {
            throw RpcExceptionHelper.Create("Invalid List Type for this Request");
        }

        /// <summary>
        /// </summary>
        /// <param name = "firstTimeStamp"></param>
        /// <param name = "secondTimeStamp"></param>
        /// <param name = "numValuesPerAlias"></param>
        /// <param name = "serverAliases"></param>
        /// <returns></returns>
        public virtual JournalDataValues[] OnReadJournalDataForTimeInterval(FilterCriterion firstTimeStamp,
                                                                            FilterCriterion secondTimeStamp,
                                                                            uint numValuesPerAlias,
                                                                            List<uint> serverAliases)
        {
            throw RpcExceptionHelper.Create("Invalid List Type for this Request");
        }

        public virtual List<AliasResult> OnWriteVST(DataValueArraysWithAlias writeValueArrays)
        {
            throw RpcExceptionHelper.Create("Invalid List Type for this Request");
        }        

        public virtual List<EventIdResult> OnAcknowledgeAlarms(string operatorName, string comment,
                                                               List<EventId> alarmsToAck)
        {
            throw RpcExceptionHelper.Create("Invalid List Type for this Request");
        }

        /// <summary>
        ///   Use this property to determine if this list is activily reporting data value changes
        /// </summary>
        public bool CallbackActivated
        {
            get { return (RegisterForCallbackEndpointEntry != null); }
        }

        /// <summary>
        ///   The number of queue entries that have been discarded since the last poll.
        /// </summary>
        public DateTime LastPollTime
        {
            get { return _lastPollTime; }
        }

        /// <summary>
        ///   Each Xi List belongs to one context.  This property returns the 
        ///   context to which this list belongs.
        /// </summary>
        public ServerContext<ServerListRoot> Context { get; private set; }

        /// <summary>
        ///   Unique per Server List ID (context based).  Cannot change after assignment.
        ///   ServerId is in referece to the Xi server.
        /// </summary>
        public uint ServerId { get; set; }

        /// <summary>
        ///   Unique per Client List ID (context based).  Cannot change after assignment.
        ///   ClientId is in reference to the Xi client.
        /// </summary>
        public uint ClientId { get; set; }

        /// <summary>
        ///   This is the List Type as defined in List Attributes.
        /// </summary>
        public uint ListType { get; private set; }

        /// <summary>
        /// </summary>
        public DateTime LastFetch { get; set; }

        #endregion

        #region protected functions

        /// <summary>
        ///   This method is invoked to issue an Information Report 
        ///   back to the Xi client for data changes.
        /// </summary>
        /// <param name = "updatedValues"></param>
        protected virtual void OnInformationReport(DataValueArraysWithAlias readValueList)
        {
            Context.OnInformationReport(ClientId, readValueList);
        }

        /// <summary>
        ///   This method invokes an Enent Notification back to the Xi
        ///   client when an event needs to be reported.
        /// </summary>
        /// <param name = "evenxiList"></param>
        protected virtual void OnEventNotification(EventMessage[] eventsArray)
        {
            Context.OnEventNotification(ClientId, eventsArray);
        }

        protected bool Disposed { get; private set; }

        protected FilterSet FilterSet_ { get; set; }

        /// <summary>
        ///   Only enabled lists may be actively used for read, write, poll and callbacks.
        /// </summary>
        protected bool Enabled { get; set; }

        #endregion

        #region private fields

        /// <summary>
        ///   The time that the last poll was received for the list.
        /// </summary>
        private DateTime _lastPollTime;

        #endregion
    }

    public abstract class ListBase<TListItemRoot> : ServerListRoot
        where TListItemRoot : ListItemRoot
    {
        #region construction and destruction

        /// <summary>
        ///   Constructs a new instance of the <see cref = "List{T}" /> class.
        /// </summary>
        public ListBase(ServerContext<ServerListRoot> context, uint clientId, uint updateRate, uint bufferingRate, uint listType,
                        uint listKey, StandardMib mib)
            : base(context, clientId, updateRate, bufferingRate, listType, listKey, mib)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                // Release and Dispose managed resources.
                foreach (TListItemRoot item in _items)
                {
                    item.Dispose();
                }
                _items.Clear();
                _changedItemsQueue.Clear();
                _itemsBuffer.Clear();
            }
            // Release unmanaged resources.
            // Set large fields to null.
            _items = null;
            _changedItemsQueue = null;
            _itemsBuffer = null;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /*
        /// <summary>
        ///   This method is invoked to queue an item to the list that is being polled.
        /// </summary>
        /// <param name = "items">The list of entries to be queued</param>
        public void QueueChangedValues(List<TListItemRoot> items)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ListRoot.");

                if ((items == null) || (items.Count == 0)) return;

                TimeSpan timeDiff = DateTime.UtcNow - LastPollTime;
                if (timeDiff.TotalMilliseconds > MissedPollIntervalMsecs) DiscardQueuedEntries();

                //entries.Add(new QueueMarker()); // add a queue marker as the last item to queue
                foreach (TListItemRoot item in items)
                {
                    _changedItemsQueue.Enqueue(item);
                }
            }
        }*/

        #endregion

        #region protected functions

        /*
        /// <summary>
        ///   This method is used to delete the oldest entries from the queue when 
        ///   the queue becomes full.  If the queue is not full, no entries are 
        ///   deleted. Implementations may override this method.
        /// </summary>
        protected virtual void DiscardQueuedEntries()
        {
            // dequeue entries to the next marker
            if (_changedItemsQueue.Count > 0)
            {
                ListItemRoot item = _changedItemsQueue.Dequeue();
                while (item.GetType() != typeof (QueueMarker))
                {
                    item = _changedItemsQueue.Dequeue();
                    DiscardedQueueEntries = (DiscardedQueueEntries == uint.MaxValue)
                                                ? DiscardedQueueEntries
                                            // don't bump if max value reached (should be impossible)
                                                : DiscardedQueueEntries + 1;
                }
            }
        }*/

        protected ObjectManager<TListItemRoot> Items
        {
            get { return _items; }
        }

        protected Queue<TListItemRoot> ChangedItemsQueue
        {
            get { return _changedItemsQueue; }
        }

        protected Buffer<TListItemRoot> ItemsBuffer
        {
            get { return _itemsBuffer; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///   This queue contains the values {always a subclass of Value Root}
        ///   that have changed since the last poll request.
        /// </summary>
        private ObjectManager<TListItemRoot> _items = new ObjectManager<TListItemRoot>(256);

        private Queue<TListItemRoot> _changedItemsQueue = new Queue<TListItemRoot>(256);
        private Buffer<TListItemRoot> _itemsBuffer = new Buffer<TListItemRoot>(256);

        #endregion
    }
}