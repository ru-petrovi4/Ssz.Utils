using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.DataAccessGrpc.Client.Data;

namespace Ssz.DataAccessGrpc.Client.ClientLists
{
    /// <summary>
    /// 
    /// </summary>
    internal class ClientEventList : ClientListRoot        
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public ClientEventList(ClientContext context, CaseInsensitiveDictionary<string>? listParams)
            : base(context)
        {
            ListType = (uint)StandardListType.EventList;
            Context.DefineList(this, listParams);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Returns the array EventIds and result codes for the alarms whose
        ///     acknowledgement failed.
        /// </summary>
        /// <param name="operatorName"></param>
        /// <param name="comment"></param>
        /// <param name="eventIdsToAck"></param>
        /// <returns></returns>
        public EventIdResult[] AckAlarms(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return Context.AckAlarms(ListServerAlias, operatorName, comment, eventIdsToAck);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ServerBase.EventMessage[] PollEventsChanges()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return Context.PollEventsChanges(this);
        }

        public ServerBase.EventMessage[] ReadEventMessagesJournal(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return Context.ReadEventMessagesJournal(this, firstTimestampUtc, secondTimestampUtc, params_);
        }

        /// <summary>
        ///     Returns new EventMessages or null, if waiting next message.
        /// </summary>
        /// <param name="eventMessagesCollection"></param>
        /// <returns></returns>
        public ServerBase.EventMessage[]? EventMessagesCallback(EventMessagesCollection eventMessagesCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            if (eventMessagesCollection.Guid != @"" && _incompleteEventMessagesCollectionCollection.Count > 0)
            {
                var beginEventMessagesCollection = _incompleteEventMessagesCollectionCollection.TryGetValue(eventMessagesCollection.Guid);
                if (beginEventMessagesCollection is not null)
                {
                    _incompleteEventMessagesCollectionCollection.Remove(eventMessagesCollection.Guid);
                    beginEventMessagesCollection.CombineWith(eventMessagesCollection);
                    eventMessagesCollection = beginEventMessagesCollection;
                }
            }

            if (eventMessagesCollection.NextCollectionGuid != @"")
            {
                _incompleteEventMessagesCollectionCollection[eventMessagesCollection.NextCollectionGuid] = eventMessagesCollection;

                return null;
            }
            else
            {
                var result = new List<ServerBase.EventMessage>();

                foreach (var eventMessage in eventMessagesCollection.EventMessages)
                {
                    result.Add(eventMessage);
                }

                return result.ToArray();
            }
        }

        /// <summary>
        ///     Throws or invokes EventMessagesCallbackEvent.        
        /// </summary>
        /// <param name="newEventMessages"></param>
        public void RaiseEventMessagesCallbackEvent(ServerBase.EventMessage[] newEventMessages)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            try
            {
                EventMessagesCallbackEvent(this, newEventMessages);
            }
            catch
            {
                //Logger.LogWarning(ex, "");
            }
        }        

        /// <summary>
        ///     This event is used to notify the client application when new events are received.
        /// </summary>
        public event EventMessagesCallbackEventHandler EventMessagesCallbackEvent = delegate { };

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     ElementValuesCallback callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<EventMessagesCollection> _incompleteEventMessagesCollectionCollection = new CaseInsensitiveDictionary<EventMessagesCollection>();

        #endregion
    }
}