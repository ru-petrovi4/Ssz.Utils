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
        ///     valueSubscription.Update() is called from сallbackDoer.
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public void HdaAddItem(string elementId, object valueSubscription)
        {
            BeginInvoke(ct =>
            {
                _dataGrpcElementValueJournalListItemsManager.AddItem(elementId, valueSubscription);
                _dataGrpcElementValueJournalListItemsManager.Subscribe(_dataGrpcServerManager, _сallbackDoer);
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
                _dataGrpcElementValueJournalListItemsManager.RemoveItem(valueSubscription);
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
        public void HdaReadElementValueJournalForTimeInterval(DateTime firstTimeStampUtc, DateTime secondTimeStampUtc, uint numValuesPerDataObject, TypeId calculation, object[] valueSubscriptionsCollection,
            Action<DataGrpcValueStatusTimestamp[][]?> setResultAction)
        {
            BeginInvoke(ct =>
            {
                _dataGrpcElementValueJournalListItemsManager.HdaReadElementValueJournalForTimeInterval( firstTimeStampUtc, secondTimeStampUtc, numValuesPerDataObject, calculation, valueSubscriptionsCollection,
                    setResultAction);
            }
            );            
        }

        #endregion

        #region private fields

        private readonly DataGrpcElementValueJournalListItemsManager _dataGrpcElementValueJournalListItemsManager;

        #endregion
    }
}
