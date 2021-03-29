using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Common;

namespace Ssz.DataGrpc.Client
{
    public partial class DataGrpcProvider
    {
        #region public functions

        /// <summary>        
        ///     valueSubscription.Update() is called from сallbackDispatcher.
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public void HdaAddItem(string elementId, object valueSubscription)
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
        public void HdaRemoveItem(object valueSubscription)
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
        /// <param name="firstTimeStampUtc"></param>
        /// <param name="secondTimeStampUtc"></param>
        /// <param name="numValuesPerDataObject"></param>
        /// <param name="calculation"></param>
        /// <param name="valueSubscriptionsCollection"></param>
        /// <param name="setResultAction"></param>
        public void HdaReadElementValueJournals(DateTime firstTimeStampUtc, DateTime secondTimeStampUtc, uint numValuesPerDataObject, TypeId calculation, object[] valueSubscriptionsCollection,
            Action<DataGrpcValueStatusTimestamp[][]?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                _clientElementValueJournalListManager.HdaReadElementValueJournals( firstTimeStampUtc, secondTimeStampUtc, numValuesPerDataObject, calculation, valueSubscriptionsCollection,
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
