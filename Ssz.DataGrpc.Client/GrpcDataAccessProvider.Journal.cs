using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.Utils.DataAccess;
using System.Threading.Tasks;
using Ssz.DataGrpc.Client.ClientLists;
using Microsoft.Extensions.Logging;

namespace Ssz.DataGrpc.Client
{
    public partial class GrpcDataAccessProvider : DisposableViewModelBase, IDataAccessProvider, IDispatcher
    {
        #region public functions

        /// <summary>        
        ///     valueSubscription.Update() is called from сallbackDispatcher.
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public virtual void JournalAddItem(string elementId, object valueSubscription)
        {
            BeginInvoke(ct =>
            {
                ClientElementValuesJournalListManager.AddItem(elementId, valueSubscription);
                ClientElementValuesJournalListManager.Subscribe(ClientConnectionManager);
            }
            );
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public virtual void JournalRemoveItem(object valueSubscription)
        {
            BeginInvoke(ct =>
            {
                ClientElementValuesJournalListManager.RemoveItem(valueSubscription);
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
        public virtual async Task<ValueStatusTimestamp[][]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, Ssz.Utils.DataAccess.TypeId calculation, CaseInsensitiveDictionary<string>? params_, object[] valueSubscriptionsCollection)
        {
            var taskCompletionSource = new TaskCompletionSource<ValueStatusTimestamp[][]?>();
            BeginInvoke(ct =>
            {
                var result = ClientElementValuesJournalListManager.ReadElementValuesJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerSubscription, calculation, params_, valueSubscriptionsCollection);

                taskCompletionSource.SetResult(result);
            }
            );
            return await taskCompletionSource.Task;
        }

        public virtual async Task<Utils.DataAccess.EventMessage[]?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string>? params_)
        {
            var taskCompletionSource = new TaskCompletionSource<Utils.DataAccess.EventMessage[]?>();
            BeginInvoke(ct =>
            {
                ClientEventList? clientEventList =
                    ClientEventListManager.GetRelatedClientEventList(OnEventMessagesCallbackInternal);

                if (clientEventList is null) return;

                try
                {
                    if (clientEventList.Disposed) return;

                    var result = clientEventList.ReadEventMessagesJournal(firstTimestampUtc, secondTimestampUtc, params_).Select(em => em.ToEventMessage()).ToArray();
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

        protected ClientElementValuesJournalListManager ClientElementValuesJournalListManager { get; }

        #endregion
    }
}
