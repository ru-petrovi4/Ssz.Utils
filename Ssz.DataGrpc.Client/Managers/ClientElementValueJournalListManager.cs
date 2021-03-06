using System;
using Ssz.Utils;
using Ssz.DataGrpc.Client.ClientListItems;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.DataGrpc.Common;
using System.Collections.Generic;
using Ssz.DataGrpc.Server;
using Ssz.Utils.DataAccess;

namespace Ssz.DataGrpc.Client.Managers
{
    public class ClientElementValueJournalListManager : ClientElementListManagerBase<ClientElementValueJournalListItem, ClientElementValueJournalList>
    {
        #region construction and destruction

        public ClientElementValueJournalListManager(ILogger<GrpcDataAccessProvider> logger) :
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
        /// <param name="callbackDispatcher"></param>
        public void Subscribe(ClientConnectionManager clientConnectionManager, IDispatcher? callbackDispatcher)
        {
            _callbackDispatcher = callbackDispatcher;

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
        /// <param name="firstTimestamp"></param>
        /// <param name="secondTimestamp"></param>
        /// <param name="numValuesPerDataObject"></param>
        /// <param name="valueSubscriptionsCollection"></param>
        /// <returns></returns>
        public void HdaReadElementValueJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerDataObject, Ssz.Utils.DataAccess.TypeId calculation,
            object[] valueSubscriptionsCollection,
            Action<ValueStatusTimestamp[][]?> setResultAction)
        {
            ValueStatusTimestamp[][]? result;

            try
            {
                var dataGrpcList = DataGrpcList;
                if (dataGrpcList != null && !dataGrpcList.Disposed)
                {
                    var serverAliases = new List<uint>();
                    foreach (var valueSubscription in valueSubscriptionsCollection)
                    {
                        var clientObjectInfo = GetClientObjectInfo(valueSubscription);
                        if (clientObjectInfo != null && clientObjectInfo.DataGrpcListItemWrapper != null && clientObjectInfo.DataGrpcListItemWrapper.DataGrpcListItem != null)
                        {
                            serverAliases.Add(clientObjectInfo.DataGrpcListItemWrapper.DataGrpcListItem.ServerAlias);
                        }
                        else
                        {
                            serverAliases.Add(0);
                        }
                    }

                    result = dataGrpcList.ReadElementValueJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerDataObject, calculation, serverAliases.ToArray());
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

            IDispatcher? callbackDispatcher = _callbackDispatcher;
            if (callbackDispatcher != null)
            {
                try
                {
                    callbackDispatcher.BeginInvoke(ct => setResultAction(result));
                }
                catch (Exception)
                {
                }
            }
        }

        # endregion

        #region private fields

        private IDispatcher? _callbackDispatcher;

        #endregion
    }
}