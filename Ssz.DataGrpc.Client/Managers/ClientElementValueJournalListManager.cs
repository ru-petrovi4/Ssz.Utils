using System;
using Ssz.Utils;
using Ssz.DataGrpc.Client.ClientListItems;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using System.Collections.Generic;
using Ssz.DataGrpc.Server;

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
        public void Subscribe(ClientConnectionManager clientConnectionManager)
        {
            try
            {
                if (!DataGrpcItemsMustBeAddedOrRemoved) return;

                bool firstTimeDataJournalConnection = (DataGrpcList is null);

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
        ///     Result.Length == valueSubscriptionsList.Length or Result is null, if failed.
        /// </summary>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="numValuesPerSubscription"></param>
        /// <param name="calculation"></param>
        /// <param name="_params"></param>
        /// <param name="valueSubscriptionsCollection"></param>
        /// <returns></returns>
        public ValueStatusTimestamp[][]? HdaReadElementValueJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, Ssz.Utils.DataAccess.TypeId calculation,
            CaseInsensitiveDictionary<string>? _params,
            object[] valueSubscriptionsCollection)
        {
            ValueStatusTimestamp[][]? result;

            try
            {
                var dataGrpcList = DataGrpcList;
                if (dataGrpcList is not null && !dataGrpcList.Disposed)
                {
                    var serverAliases = new List<uint>();
                    foreach (var valueSubscription in valueSubscriptionsCollection)
                    {
                        var clientObjectInfo = GetClientObjectInfo(valueSubscription);
                        if (clientObjectInfo is not null && clientObjectInfo.DataGrpcListItemWrapper is not null && clientObjectInfo.DataGrpcListItemWrapper.DataGrpcListItem is not null)
                        {
                            serverAliases.Add(clientObjectInfo.DataGrpcListItemWrapper.DataGrpcListItem.ServerAlias);
                        }
                        else
                        {
                            serverAliases.Add(0);
                        }
                    }

                    result = dataGrpcList.ReadElementValueJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerSubscription, calculation, _params, serverAliases.ToArray());
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

            return result;
        }

        #endregion
    }
}