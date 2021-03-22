using System;
using Ssz.Utils;
using Ssz.DataGrpc.Client.ClientListItems;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.DataGrpc.Common;
using System.Collections.Generic;
using Ssz.DataGrpc.Server;

namespace Ssz.DataGrpc.Client.Managers
{
    public class ClientElementValueJournalListManager : ClientElementListManagerBase<ClientElementValueJournalListItem, ClientElementValueJournalList>
    {
        #region construction and destruction

        public ClientElementValueJournalListManager(ILogger<DataGrpcProvider> logger) :
            base(logger, true)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Creates List, adds/removes items.
        ///     No throw.
        /// </summary>
        /// <param name="clientConnectionManager"></param>
        /// <param name="callbackDoer"></param>
        public void Subscribe(ClientConnectionManager clientConnectionManager, IDispatcher? callbackDoer)
        {
            _callbackDoer = callbackDoer;

            try
            {
                if (!DataGrpcItemsMustBeAddedOrRemoved) return;

                bool firstTimeDataJournalConnection = (DataGrpcList == null);

                if (firstTimeDataJournalConnection)
                {
                    try
                    {
                        if (clientConnectionManager.ConnectionExists)
                        {
                            DataGrpcList = clientConnectionManager.NewElementValueJournalList(null);
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

            IDispatcher? callbackDoer = _callbackDoer;
            if (callbackDoer != null)
            {
                try
                {
                    callbackDoer.BeginInvoke(ct => setResultAction(result));
                }
                catch (Exception)
                {
                }
            }
        }

        # endregion

        #region private fields

        private IDispatcher? _callbackDoer;

        #endregion
    }
}