using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;


namespace Ssz.Xi.Client
{
    public partial class XiDataAccessProvider
    {
        #region public functions

        /// <summary>        
        ///     valueSubscription.Update() is called from сallbackDoer.
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public void JournalAddItem(string elementId, object valueSubscription)
        {
            _xiDataJournalListItemsManager.AddItem(elementId, valueSubscription);
            if (_xiServerProxy == null) throw new InvalidOperationException();
            _xiDataJournalListItemsManager.Subscribe(_xiServerProxy);
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void JournalRemoveItem(object valueSubscription)
        {
            _xiDataJournalListItemsManager.RemoveItem(valueSubscription);
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
            var xiList = _xiDataJournalListItemsManager.XiList;
            if (xiList == null || xiList.Disposed)
            {
                setResultAction(null);
                return;
            }

            try
            {
                var firstTimestamp = new FilterCriterion
                {
                    OperandName = FilterOperandNames.Timestamp,
                    ComparisonValue = firstTimestampUtc,
                    Operator = FilterOperator.GreaterThan
                };

                var secondTimestamp = new FilterCriterion
                {
                    OperandName = FilterOperandNames.Timestamp,
                    ComparisonValue = secondTimestampUtc,
                    Operator = FilterOperator.LessThanOrEqual
                };

                xiList.ReadJournalDataForTimeInterval(firstTimestamp, secondTimestamp,
                    numValuesPerDataObject, null);

                var result = new List<ValueStatusTimestamp[]>();
                foreach (var valueSubscription in valueSubscriptionsCollection)
                {
                    var clientObjectInfo = _xiDataJournalListItemsManager.GetClientObjectInfo(valueSubscription);
                    if (clientObjectInfo != null && clientObjectInfo.XiListItemWrapper != null && clientObjectInfo.XiListItemWrapper.XiListItem != null)
                    {
                        ValueStatusTimestamp[] valueStatusTimestampSet =
                            clientObjectInfo.XiListItemWrapper.XiListItem.GetExistingOrNewValueStatusTimestampSet().ToArray();
                        result.Add(valueStatusTimestampSet);
                    }
                    else
                    {
                        result.Add(new ValueStatusTimestamp[0]);
                    }
                }
                setResultAction(result.ToArray());
                return;                
            }
            catch (Exception)
            {
                setResultAction(null);
                return;
            }
        }

        #endregion

        #region private fields

        private readonly XiDataJournalListItemsManager _xiDataJournalListItemsManager =
            new XiDataJournalListItemsManager();

        #endregion
    }
}
