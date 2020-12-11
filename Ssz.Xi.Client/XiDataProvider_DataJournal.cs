using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;


namespace Ssz.Xi.Client
{
    public partial class XiDataProvider
    {
        #region public functions

        /// <summary>        
        ///     valueSubscription.Update() is called from сallbackDoer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="valueSubscription"></param>
        public void HdaAddItem(string? id, object valueSubscription)
        {
            id = id ?? @"";

            _xiDataJournalListItemsManager.AddItem(id, valueSubscription);
            if (_xiServerProxy == null) throw new InvalidOperationException();
            _xiDataJournalListItemsManager.Subscribe(_xiServerProxy);
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public void HdaRemoveItem(object valueSubscription)
        {
            _xiDataJournalListItemsManager.RemoveItem(valueSubscription);
        }

        /// <summary>        
        ///   Result.Count == valueSubscriptionsList.Count or Result == Null, if failed.
        /// </summary>
        /// <param name="firstTimeStamp"></param>
        /// <param name="secondTimeStamp"></param>
        /// <param name="numValuesPerDataObject"></param>
        /// <param name="valueSubscriptionsList"></param>
        /// <returns></returns>
        public List<XiValueStatusTimestamp[]?>? HdaReadJournalDataForTimeInterval(FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, uint numValuesPerDataObject, IEnumerable<object> valueSubscriptionsList)
        {
            var xiList = _xiDataJournalListItemsManager.XiList;
            if (xiList == null || xiList.Disposed) return null;

            try
            {
                xiList.ReadJournalDataForTimeInterval(firstTimeStamp, secondTimeStamp,
                    numValuesPerDataObject, null);

                var result = new List<XiValueStatusTimestamp[]?>();
                foreach (var valueSubscription in valueSubscriptionsList)
                {
                    var modelItem = _xiDataJournalListItemsManager.GetModelItem(valueSubscription);
                    if (modelItem != null && modelItem.XiListItemWrapper != null && modelItem.XiListItemWrapper.XiListItem != null)
                    {
                        XiValueStatusTimestamp[] vstSet =
                            modelItem.XiListItemWrapper.XiListItem.GetExistingOrNewValueStatusTimestampSet().ToArray();
                        result.Add(vstSet);
                    }
                    else
                    {
                        result.Add(null);
                    }
                }
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region private fields

        private readonly XiDataJournalListItemsManager _xiDataJournalListItemsManager =
            new XiDataJournalListItemsManager();

        #endregion
    }
}
