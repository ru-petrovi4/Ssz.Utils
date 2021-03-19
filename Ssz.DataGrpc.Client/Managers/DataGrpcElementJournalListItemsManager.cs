using System;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Core.ListItems;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.Core.Lists;
using Ssz.DataGrpc.Common;
using System.Collections.Generic;
using Ssz.DataGrpc.Server;

namespace Ssz.DataGrpc.Client.Managers
{
    public class DataGrpcElementValueJournalListItemsManager : DataGrpcElementListItemsManagerBase<DataGrpcElementValueJournalListItem, DataGrpcElementValueJournalList>
    {
        #region construction and destruction

        public DataGrpcElementValueJournalListItemsManager(ILogger<DataGrpcProvider> logger) :
            base(logger, true)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Creates List, adds/removes items.
        ///     No throw.
        /// </summary>
        /// <param name="dataGrpcServerProxy"></param>
        /// <param name="ñallbackDoer"></param>
        public void Subscribe(DataGrpcServerManager dataGrpcServerProxy, ICallbackDoer? ñallbackDoer)
        {
            _ñallbackDoer = ñallbackDoer;

            try
            {
                if (!DataGrpcItemsMustBeAddedOrRemoved) return;

                bool firstTimeDataJournalConnection = (DataGrpcList == null);

                if (firstTimeDataJournalConnection)
                {
                    try
                    {
                        if (dataGrpcServerProxy.ConnectionExists)
                        {
                            DataGrpcList = dataGrpcServerProxy.NewElementValueJournalList(null);
                        }                            
                    }
                    catch (Exception)
                    {                        
                    }
                }

                bool connectionError = SubscribeInitial();

                if (!connectionError)
                {
                    DataGrpcItemsMustBeAddedOrRemoved = false;
                }                    
            }
            finally
            {
                SubscribeFinal();
            }
        }

        /// <summary>        
        ///   Result.Length == valueSubscriptionsList.Length or Result == Null, if failed.
        /// </summary>
        /// <param name="firstTimeStamp"></param>
        /// <param name="secondTimeStamp"></param>
        /// <param name="numValuesPerDataObject"></param>
        /// <param name="valueSubscriptionsCollection"></param>
        /// <returns></returns>
        public void HdaReadElementValueJournalForTimeInterval(DateTime firstTimeStampUtc, DateTime secondTimeStampUtc, uint numValuesPerDataObject, TypeId calculation,
            object[] valueSubscriptionsCollection,
            Action<DataGrpcValueStatusTimestamp[][]?> setResultAction)
        {
            DataGrpcValueStatusTimestamp[][]? result;

            try
            {
                var dataGrpcList = DataGrpcList;
                if (dataGrpcList != null && !dataGrpcList.Disposed)
                {
                    var serverAliases = new List<uint>();
                    foreach (var valueSubscription in valueSubscriptionsCollection)
                    {
                        var modelItem = GetModelItem(valueSubscription);
                        if (modelItem != null && modelItem.DataGrpcListItemWrapper != null && modelItem.DataGrpcListItemWrapper.DataGrpcListItem != null)
                        {
                            serverAliases.Add(modelItem.DataGrpcListItemWrapper.DataGrpcListItem.ServerAlias);
                        }
                        else
                        {
                            serverAliases.Add(0);
                        }
                    }

                    result = dataGrpcList.ReadElementValueJournalForTimeInterval(firstTimeStampUtc, secondTimeStampUtc, numValuesPerDataObject, calculation, serverAliases.ToArray());
                }
                else
                {
                    result = null;
                }
            }
            catch (Exception)
            {
                result = null;
            }

            ICallbackDoer? ñallbackDoer = _ñallbackDoer;
            if (ñallbackDoer != null)
            {
                try
                {
                    ñallbackDoer.BeginInvoke(ct => setResultAction(result));
                }
                catch (Exception)
                {
                }
            }
        }

        # endregion

        #region private fields

        private ICallbackDoer? _ñallbackDoer;

        #endregion
    }
}