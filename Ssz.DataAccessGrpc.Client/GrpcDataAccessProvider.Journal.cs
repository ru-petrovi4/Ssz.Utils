using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using System.Threading.Tasks;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Microsoft.Extensions.Logging;

namespace Ssz.DataAccessGrpc.Client
{
    public partial class GrpcDataAccessProvider
    {
        #region public functions

        /// <summary>        
        ///     valueSubscription.Update() is called from сallbackDispatcher.
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public override void JournalAddItem(string elementId, object valueSubscription)
        {
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _clientElementValuesJournalListManager.AddItem(elementId, valueSubscription);
                _clientElementValuesJournalListManager.Subscribe(_clientConnectionManager);
            }
            );
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public override void JournalRemoveItem(object valueSubscription)
        {
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _clientElementValuesJournalListManager.RemoveItem(valueSubscription);
            }
            );            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="numValuesPerSubscription"></param>
        /// <param name="calculation"></param>
        /// <param name="params_"></param>
        /// <param name="valueSubscriptionsCollection"></param>
        /// <returns></returns>
        public override async Task<ValueStatusTimestamp[][]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveDictionary<string?>? params_, object[] valueSubscriptionsCollection)
        {
            var taskCompletionSource = new TaskCompletionSource<ValueStatusTimestamp[][]?>();
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                var result = _clientElementValuesJournalListManager.ReadElementValuesJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerSubscription, calculation, params_, valueSubscriptionsCollection);

                taskCompletionSource.SetResult(result);
            }
            );
            return await taskCompletionSource.Task;
        }

        public override async Task<Utils.DataAccess.EventMessage[]?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            var taskCompletionSource = new TaskCompletionSource<Utils.DataAccess.EventMessage[]?>();
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                ClientEventList? clientEventList =
                    _clientEventListManager.GetRelatedClientEventList(OnEventMessagesCallbackInternal);

                if (clientEventList is null) return;

                try
                {
                    if (clientEventList.Disposed) return;

                    var result = clientEventList.ReadEventMessagesJournal(firstTimestampUtc, secondTimestampUtc, params_).Select(                        
                        em =>
                        {
                            var eventMessage = em.ToEventMessage();
                            if (ElementIdsMap is not null)
                                ElementIdsMap.AddFieldsToEventMessage(eventMessage);
                            return eventMessage;
                        }
                        ).ToArray();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception");
                    taskCompletionSource.SetResult(null);
                }                                
            }
            );
            return await taskCompletionSource.Task;
        }

        #endregion

        #region protected functions

        protected IEnumerable<ValueStatusTimestamp> ReadElementValuesJournalInternal(string elementId, DateTime firstTimestampUtc,
            DateTime secondTimestampUtc)
        {
            object clientObj = elementId;
            _clientElementValuesJournalListManager.AddItem(elementId, clientObj);
            _clientElementValuesJournalListManager.Subscribe(_clientConnectionManager);

            var data = _clientElementValuesJournalListManager.ReadElementValuesJournals(firstTimestampUtc, secondTimestampUtc, uint.MaxValue, new Ssz.Utils.DataAccess.TypeId(), null, new[] { clientObj });
            if (data is not null)
                return data[0];

            // No remove, for optimization
            //ClientElementValuesJournalListManager.RemoveItem(clientObj);
            //ClientElementValuesJournalListManager.Subscribe(ClientConnectionManager, CallbackDispatcher);

            return new ValueStatusTimestamp[0];
        }

        #endregion

        #region private fields

        private ClientElementValuesJournalListManager _clientElementValuesJournalListManager { get; }

        #endregion
    }
}
