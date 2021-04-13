using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Common;
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
        public void JournalAddItem(string elementId, object valueSubscription)
        {
            BeginInvoke(ct =>
            {
                _clientElementValueJournalListManager.AddItem(elementId, valueSubscription);
                _clientElementValueJournalListManager.Subscribe(_clientConnectionManager, _сallbackDispatcher);
            }
            );
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void JournalRemoveItem(object valueSubscription)
        {
            BeginInvoke(ct =>
            {
                _clientElementValueJournalListManager.RemoveItem(valueSubscription);
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
        public void ReadElementValueJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerDataObject, Ssz.Utils.DataAccess.TypeId calculation, object[] valueSubscriptionsCollection,
            Action<ValueStatusTimestamp[][]?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                _clientElementValueJournalListManager.HdaReadElementValueJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerDataObject, calculation, valueSubscriptionsCollection,
                    setResultAction);
            }
            );            
        }

        #endregion

        #region private fields

        private readonly ClientElementValueJournalListManager _clientElementValueJournalListManager;

        #endregion
    }
}
