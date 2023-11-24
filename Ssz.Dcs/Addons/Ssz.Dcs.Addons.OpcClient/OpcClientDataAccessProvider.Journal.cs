using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;


namespace Ssz.Dcs.Addons.OpcClient
{
    public partial class OpcClientDataAccessProvider
    {
        #region public functions

        /// <summary>        
        ///     valueSubscription.Update() is called from сallbackDoer.
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        public override void JournalAddItem(string elementId, object valueSubscription)
        {
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _xiDataJournalListItemsManager.AddItem(elementId, valueSubscription);
                if (_xiServerProxy is null) throw new InvalidOperationException();
                _xiDataJournalListItemsManager.Subscribe(_xiServerProxy);
            });
        }

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        public override void JournalRemoveItem(object valueSubscription)
        {
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _xiDataJournalListItemsManager.RemoveItem(valueSubscription);
            });
        }

        public override async Task<ValueStatusTimestamp[][]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveDictionary<string?>? params_, object[] valueSubscriptionsCollection)
        {
            var taskCompletionSource = new TaskCompletionSource<ValueStatusTimestamp[][]?>();
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                var xiList = _xiDataJournalListItemsManager.XiList;
                if (xiList is null || xiList.Disposed)
                {
                    taskCompletionSource.SetResult(null);
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
                        numValuesPerSubscription, null);

                    var result = new List<ValueStatusTimestamp[]>();
                    foreach (var valueSubscription in valueSubscriptionsCollection)
                    {
                        var clientObjectInfo = _xiDataJournalListItemsManager.GetClientObjectInfo(valueSubscription);
                        if (clientObjectInfo is not null && clientObjectInfo.XiListItemWrapper is not null && clientObjectInfo.XiListItemWrapper.XiListItem is not null)
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
                    taskCompletionSource.SetResult(result.ToArray());
                }
                catch (Exception)
                {
                    taskCompletionSource.SetResult(null);
                }
            });
            return await taskCompletionSource.Task;
        }

        public override async Task<Utils.DataAccess.EventMessagesCollection?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            var taskCompletionSource = new TaskCompletionSource<Utils.DataAccess.EventMessagesCollection?>();
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                var result = _xiEventListItemsManager.ReadEventMessagesJournal(firstTimestampUtc, secondTimestampUtc, params_);
                ElementIdsMap?.AddCommonFieldsToEventMessagesCollection(result);
                taskCompletionSource.SetResult(result);
            }
            );
            return await taskCompletionSource.Task;
        }

        #endregion

        #region private fields

        private readonly XiDataJournalListItemsManager _xiDataJournalListItemsManager;

        #endregion
    }
}
