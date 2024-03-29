﻿using System;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Ssz.Xi.Client.Internal.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    public class XiDataJournalListItemsManager : XiListItemsManager<IXiDataJournalListItem, IXiDataJournalListProxy>
    {
        #region public functions

        /// <summary>
        ///     Creates List, adds/removes items.
        ///     No throw.
        /// </summary>
        public void Subscribe(XiServerProxy xiServerProxy, bool unsubscribeItemsFromServer)
        {
            try
            {                
                if (!XiItemsMustBeAddedOrRemoved) return;

                bool firstTimeDataJournalConnection = (XiList is null);

                if (firstTimeDataJournalConnection)
                {
                    try
                    {
                        if (xiServerProxy.ContextExists)
                            XiList = xiServerProxy.NewDataJournalList(0, 0, null);
                    }
                    catch (Exception)
                    {                        
                    }
                }

                if (XiList is null || XiList.Disposed) return;

                bool connectionError = SubscribeInitial(unsubscribeItemsFromServer);

                try
                {                    
                    if (!connectionError && XiList is not null && !XiList.Disposed)
                    {
                        XiList.EnableListUpdating(true);
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "Exception");
                    connectionError = true;
                }

                if (!connectionError)
                    XiItemsMustBeAddedOrRemoved = false;
            }
            finally
            {
                SubscribeFinal();
            }
        }

        public override InstanceId GetInstanceId(string id)
        {
            return new InstanceId(InstanceIds.ResourceType_HDA, XiSystem, id);
        }

        #endregion
    }
}