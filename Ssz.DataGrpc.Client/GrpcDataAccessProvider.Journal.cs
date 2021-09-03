using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.Utils.DataAccess;

namespace Ssz.DataGrpc.Client
{
    public partial class GrpcDataAccessProvider
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
                ClientElementValueJournalListManager.Subscribe(ClientConnectionManager, CallbackDispatcher);
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
        /// <param name="numValuesPerDataObject"></param>
        /// <param name="calculation"></param>
        /// <param name="valueSubscriptionsCollection"></param>
        /// <param name="setResultAction"></param>
        public virtual void ReadElementValueJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerDataObject, Ssz.Utils.DataAccess.TypeId calculation, object[] valueSubscriptionsCollection,
            Action<ValueStatusTimestamp[][]?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                ClientElementValueJournalListManager.HdaReadElementValueJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerDataObject, calculation, valueSubscriptionsCollection,
                    setResultAction);
            }
            );            
        }

        #endregion

        #region protected functions

        protected ClientElementValueJournalListManager ClientElementValueJournalListManager { get; }

        #endregion
    }
}
