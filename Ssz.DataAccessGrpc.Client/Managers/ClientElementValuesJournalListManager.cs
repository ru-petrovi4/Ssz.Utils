using System;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using System.Collections.Generic;
using Ssz.DataAccessGrpc.ServerBase;

namespace Ssz.DataAccessGrpc.Client.Managers
{
    internal class ClientElementValuesJournalListManager : ClientElementListManagerBase<ClientElementValuesJournalListItem, ClientElementValuesJournalList>
    {
        #region construction and destruction

        public ClientElementValuesJournalListManager(ILogger<GrpcDataAccessProvider> logger) :
            base(logger)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Creates List, adds/removes items.
        ///     No throw.
        /// </summary>
        /// <param name="clientContextManager"></param>
        /// <param name="unsubscribeItemsFromServer"></param>
        public void Subscribe(ClientContextManager clientContextManager, bool unsubscribeItemsFromServer)
        {
            try
            {
                if (!DataAccessGrpcItemsMustBeAddedOrRemoved) return;

                bool firstTimeDataJournalConnection = (DataAccessGrpcList is null);

                if (firstTimeDataJournalConnection)
                {
                    try
                    {
                        if (clientContextManager.ConnectionExists)
                        {
                            DataAccessGrpcList = clientContextManager.NewElementValuesJournalList(null);
                        }                            
                    }
                    catch (Exception)
                    {                        
                    }
                }

                bool connectionError = SubscribeInitial(unsubscribeItemsFromServer);

                if (!connectionError)
                {
                    DataAccessGrpcItemsMustBeAddedOrRemoved = false;
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
        /// <param name="params_"></param>
        /// <param name="valueSubscriptionsCollection"></param>
        /// <returns></returns>
        public ValueStatusTimestamp[][]? ReadElementValuesJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, Ssz.Utils.DataAccess.TypeId? calculation,
            CaseInsensitiveDictionary<string?>? params_,
            object[] valueSubscriptionsCollection)
        {
            ValueStatusTimestamp[][]? result;

            try
            {
                var dataAccessGrpcList = DataAccessGrpcList;
                if (dataAccessGrpcList is not null && !dataAccessGrpcList.Disposed)
                {
                    var serverAliases = new List<uint>();
                    foreach (var valueSubscription in valueSubscriptionsCollection)
                    {
                        var clientObjectInfo = GetClientObjectInfo(valueSubscription);
                        if (clientObjectInfo is not null && clientObjectInfo.DataAccessGrpcListItemWrapper is not null && clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem is not null)
                        {
                            serverAliases.Add(clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.ServerAlias);
                        }
                        else
                        {
                            serverAliases.Add(0);
                        }
                    }

                    result = dataAccessGrpcList.ReadElementValuesJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerSubscription, calculation, params_, serverAliases.ToArray());
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