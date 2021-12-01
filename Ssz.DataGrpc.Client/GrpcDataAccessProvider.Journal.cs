using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.Utils.DataAccess;
using System.Threading.Tasks;

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
                ClientElementValueJournalListManager.AddItem(elementId, valueSubscription);
                ClientElementValueJournalListManager.Subscribe(ClientConnectionManager);
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
                ClientElementValueJournalListManager.RemoveItem(valueSubscription);
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
        /// <param name="_params"></param>
        /// <param name="valueSubscriptionsCollection"></param>
        /// <returns></returns>
        public virtual async Task<ValueStatusTimestamp[][]?> ReadElementValueJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, Ssz.Utils.DataAccess.TypeId calculation, CaseInsensitiveDictionary<string>? _params, object[] valueSubscriptionsCollection)
        {
            var taskCompletionSource = new TaskCompletionSource<ValueStatusTimestamp[][]?>();
            BeginInvoke(ct =>
            {
                var result = ClientElementValueJournalListManager.HdaReadElementValueJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerSubscription, calculation, _params, valueSubscriptionsCollection);

                taskCompletionSource.SetResult(result);
            }
            );
            return await taskCompletionSource.Task;
        }

        #endregion

        #region protected functions

        protected ClientElementValueJournalListManager ClientElementValueJournalListManager { get; }

        #endregion
    }
}
