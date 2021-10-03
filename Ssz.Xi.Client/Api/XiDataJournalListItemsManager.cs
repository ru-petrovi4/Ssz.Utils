using System;
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
        public void Subscribe(XiServerProxy xiServerProxy)
        {
            try
            {                
                if (!XiItemsMustBeAddedOrRemoved) return;

                bool firstTimeDataJournalConnection = (XiList == null);

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

                if (XiList == null || XiList.Disposed) return;

                bool connectionError = SubscribeInitial();

                try
                {                    
                    if (!connectionError && XiList != null && !XiList.Disposed)
                    {
                        if (firstTimeDataJournalConnection)
                        {
                            try
                            {
                                XiList.Readable = true;
                            }
                            catch
                            {
                            }                            
                        }

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