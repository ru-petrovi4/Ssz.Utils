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
        public async Task<List<Utils.DataAccess.EventMessagesCollection>> PollEventsChangesAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return await Context.PollEventsChangesAsync(this);
        }

        public async Task<List<Utils.DataAccess.EventMessagesCollection>> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return await Context.ReadEventMessagesJournalAsync(this, firstTimestampUtc, secondTimestampUtc, params_);
        }

        /// <summary>
        ///     Returns new EventMessagesCollection or null, if waiting next message.
        /// </summary>
        /// <param name="eventMessagesCollection"></param>
        /// <returns></returns>
        public Utils.DataAccess.EventMessagesCollection? GetEventMessagesCollection(ServerBase.EventMessagesCollection eventMessagesCollection)
        {
            if (Disposed) 
                throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            _incompleteEventMessagesCollections.Add(eventMessagesCollection);            

            if (eventMessagesCollection.IsIncomplete)
            {
                return null;
            }
            else
            {
                Utils.DataAccess.EventMessagesCollection result = new();

                foreach (var incompleteEventMessagesCollection in _incompleteEventMessagesCollections)
                {
                    foreach (var eventMessage in incompleteEventMessagesCollection.EventMessages)
                    {
                        result.EventMessages.Add(eventMessage.ToEventMessage());
                    }

                    if (incompleteEventMessagesCollection.CommonFields.Count > 0)
                    {
                        result.CommonFields = new CaseInsensitiveDictionary<string?>(eventMessagesCollection.CommonFields
                                    .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));

                    }
                }
                _incompleteEventMessagesCollections.Clear();

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
        private readonly List<ServerBase.EventMessagesCollection> _incompleteEventMessagesCollections = new();

        #endregion
    }
}