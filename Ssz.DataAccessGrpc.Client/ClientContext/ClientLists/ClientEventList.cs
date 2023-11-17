using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System.Threading.Tasks;

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
        public ClientEventList(ClientContext context)
            : base(context)
        {
            ListType = (uint)StandardListType.EventList;            
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listParams"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task DefineListAsync(CaseInsensitiveDictionary<string>? listParams)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            await Context.DefineListAsync(this, listParams);
        }

        /// <summary>
        ///     Returns the array EventIds and result codes for the alarms whose
        ///     acknowledgement failed.
        /// </summary>
        /// <param name="operatorName"></param>
        /// <param name="comment"></param>
        /// <param name="eventIdsToAck"></param>
        /// <returns></returns>
        public async Task<EventIdResult[]> AckAlarmsAsync(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return await Context.AckAlarmsAsync(ListServerAlias, operatorName, comment, eventIdsToAck);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<Utils.DataAccess.EventMessagesCollection> PollEventsChangesAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return await Context.PollEventsChangesAsync(this);
        }

        public Utils.DataAccess.EventMessagesCollection ReadEventMessagesJournal(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return Context.ReadEventMessagesJournal(this, firstTimestampUtc, secondTimestampUtc, params_);
        }

        /// <summary>
        ///     Returns new EventMessagesCollection or null, if waiting next message.
        /// </summary>
        /// <param name="eventMessagesCollection"></param>
        /// <returns></returns>
        public Utils.DataAccess.EventMessagesCollection? GetEventMessagesCollection(ServerBase.EventMessagesCollection eventMessagesCollection)
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
                Utils.DataAccess.EventMessagesCollection result = new();

                foreach (var eventMessage in eventMessagesCollection.EventMessages)
                {
                    result.EventMessages.Add(eventMessage.ToEventMessage());
                }

                if (eventMessagesCollection.CommonFields.Count > 0)
                {
                    result.CommonFields = new CaseInsensitiveDictionary<string?>(eventMessagesCollection.CommonFields
                                .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));

                }

                return result;
            }
        }

        /// <summary>
        ///     Throws or invokes EventMessagesCallbackEvent.        
        /// </summary>
        /// <param name="eventMessagesCollection"></param>
        public void RaiseEventMessagesCallbackEvent(Utils.DataAccess.EventMessagesCollection eventMessagesCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            try
            {
                EventMessagesCallback(this, new EventMessagesCallbackEventArgs { EventMessagesCollection = eventMessagesCollection });
            }
            catch
            {
                //Logger.LogWarning(ex, "");
            }
        }        

        /// <summary>
        ///     This event is used to notify the client application when new events are received.
        /// </summary>
        public event EventHandler<EventMessagesCallbackEventArgs> EventMessagesCallback = delegate { };

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     ElementValuesCallback callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<ServerBase.EventMessagesCollection> _incompleteEventMessagesCollectionCollection = new();

        #endregion
    }
}