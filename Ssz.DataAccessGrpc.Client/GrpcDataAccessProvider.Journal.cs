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
            WorkingThreadSafeDispatcher.BeginInvokeEx(async ct =>
            {
                if (!IsInitialized)
                    return;
                _clientElementValuesJournalListManager.AddItem(elementId, valueSubscription);
                await _clientElementValuesJournalListManager.SubscribeAsync(_clientContextManager, Options.UnsubscribeValuesJournalListItemsFromServer);
            }
            );
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public override void JournalRemoveItem(object valueSubscription)
        {
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
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
        public override async Task<ElementValuesJournal[]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveDictionary<string?>? params_, object[] valueSubscriptionsCollection)
        {
            var taskCompletionSource = new TaskCompletionSource<ElementValuesJournal[]?>();
            WorkingThreadSafeDispatcher.BeginInvokeEx(async ct =>
            {
                try
                {
                    var result = await _clientElementValuesJournalListManager.ReadElementValuesJournalsAsync(firstTimestampUtc, secondTimestampUtc, numValuesPerSubscription, calculation, params_, valueSubscriptionsCollection);

                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            }
            );
            return await taskCompletionSource.Task;
        }

        public override async Task<List<Utils.DataAccess.EventMessagesCollection>?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            var taskCompletionSource = new TaskCompletionSource<List<Utils.DataAccess.EventMessagesCollection>?>();
            WorkingThreadSafeDispatcher.BeginInvokeEx(async ct =>
            {
                ClientEventList? clientEventList =
                    _clientEventListManager.GetRelatedClientEventList(OnClientEventListManager_EventMessagesCallbackInternal);

                if (clientEventList is null) return;

                try
                {
                    if (clientEventList.Disposed) return;

                    var result = await clientEventList.ReadEventMessagesJournalAsync(firstTimestampUtc, secondTimestampUtc, params_);
                    foreach (var eventMessagesCollection in result)
                    {
                        ElementIdsMap?.AddCommonFieldsToEventMessagesCollection(eventMessagesCollection);
                    }                    
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "Exception");
                    taskCompletionSource.SetResult(null);
                }                                
            }
            );
            return await taskCompletionSource.Task;
        }

        #endregion

        #region protected functions

        protected async Task<ElementValuesJournal> ReadElementValuesJournalInternalAsync(string elementId, DateTime firstTimestampUtc,
            DateTime secondTimestampUtc)
        {
            if (!IsInitialized)
                return new ElementValuesJournal();

            object clientObj = elementId;
            _clientElementValuesJournalListManager.AddItem(elementId, clientObj);
            await _clientElementValuesJournalListManager.SubscribeAsync(_clientContextManager, Options.UnsubscribeValuesJournalListItemsFromServer);

            var data = await _clientElementValuesJournalListManager.ReadElementValuesJournalsAsync(firstTimestampUtc, secondTimestampUtc, uint.MaxValue, new Ssz.Utils.DataAccess.TypeId(), null, new[] { clientObj });
            if (data is not null)
                return data[0];

            if (Options.UnsubscribeValuesJournalListItemsFromServer)
            {
                _clientElementValuesJournalListManager.RemoveItem(clientObj);
                await _clientElementValuesJournalListManager.SubscribeAsync(_clientContextManager, Options.UnsubscribeValuesJournalListItemsFromServer);
            }

            return new ElementValuesJournal();
        }

        #endregion

        #region private fields

        private ClientElementValuesJournalListManager _clientElementValuesJournalListManager { get; }

        #endregion
    }
}
