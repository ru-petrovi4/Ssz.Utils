using System;
using System.Diagnostics;
using Ssz.Utils;
using Ssz.DataGrpc.Server.Core.Context;
using Ssz.DataGrpc.Server.Core.ListItems;
using Xi.Common.Support;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Lists
{
    /// <summary>
    ///   This is the base class from which an implementation of a Xi server 
    ///   would subclass to provide access to current process data values.
    /// </summary>
    public abstract class ServerElementValueList : ElementListBase<TElementValueListItemBase>
        where TElementValueListItemBase : ElementValueListItem
    {
        #region construction and destruction

        /// <summary>
        ///   The constructor for this class.
        /// </summary>
        /// <param name = "context"></param>
        /// <param name = "clientId"></param>
        /// <param name = "updateRate"></param>
        /// <param name = "bufferingRate"></param>
        /// <param name = "listType"></param>
        /// <param name = "listKey"></param>
        public ElementValueListBase(ServerContext<ServerListRoot> context, uint clientId, uint updateRate, uint bufferingRate,
                        uint listType, uint listKey, StandardMib mib)
            : base(context, clientId, updateRate, bufferingRate, listType, listKey, mib)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///   This method may be overridden in the implementation subclass.  However, 
        ///   the implementation provided here should be adequate when the changed 
        ///   data values are added to the queue of changed Entry Root by setting 
        ///   the Entry Queued property of the data value.
        /// </summary>
        /// <returns></returns>
        public override DataValueArraysWithAlias OnPollDataChanges()
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ElementValueListBase.");

                if (PollEndpointEntry == null) throw RpcExceptionHelper.Create("List not attached to the IPoll endpoint.");
                if (!Enabled) throw RpcExceptionHelper.Create("List not Enabled.");

                if (ChangedItemsQueue.Count > 0)
                {
                    DataValueArraysWithAliasEx.Prepare(ChangedItemsQueue.Count, ChangedItemsQueue.Count, ChangedItemsQueue.Count);
                    
                    foreach (TElementValueListItemBase item in ChangedItemsQueue)
                    {
                        if (item.GetType() == typeof (QueueMarker)) continue; // ignore queue markers

                        switch (item.Value.ValueStorageType)
                        {
                            case Any.StorageType.Double:
                                DataValueArraysWithAliasEx.DoubleStatusCodes.Add(item.StatusCode);
                                DataValueArraysWithAliasEx.DoubleTimeStamps.Add(item.TimestampUtc);
                                DataValueArraysWithAliasEx.DoubleValues.Add(item.Value.StorageDouble);
                                DataValueArraysWithAliasEx.DoubleAlias.Add(item.ClientAlias);
                                break;
                            case Any.StorageType.UInt32:
                                DataValueArraysWithAliasEx.UintStatusCodes.Add(item.StatusCode);
                                DataValueArraysWithAliasEx.UintTimeStamps.Add(item.TimestampUtc);
                                DataValueArraysWithAliasEx.UintValues.Add(item.Value.StorageUInt32);
                                DataValueArraysWithAliasEx.UintAlias.Add(item.ClientAlias);
                                break;
                            case Any.StorageType.Object:
                                DataValueArraysWithAliasEx.ObjectStatusCodes.Add(item.StatusCode);
                                DataValueArraysWithAliasEx.ObjectTimeStamps.Add(item.TimestampUtc);
                                DataValueArraysWithAliasEx.ObjectValues.Add(item.Value.StorageObject);
                                DataValueArraysWithAliasEx.ObjectAlias.Add(item.ClientAlias);
                                break;
                            default:
                                Debug.Assert(false, "Bad Data Value Key");
                                break;
                        }
                        item.EntryQueued = false;
                    }
                    DiscardedQueueEntries = 0; // reset this counter for each poll 
                    ChangedItemsQueue.Clear();

                    return DataValueArraysWithAliasEx.GetValueArraysWithAlias();
                }
                return null;
            }
        }

        /*
        /// <summary>
        ///   This method provides a default implementation for On Read Data which 
        ///   is invoked by Context Base {Read}.  This implementation should be 
        ///   adequate for most situations.  This method in turn invokes another 
        ///   version of On Read Data which is generally overridden by the 
        ///   implementation subclass.
        /// </summary>
        /// <param name = "serverAliases"></param>
        /// <returns></returns>
        public override DataValueArraysWithAlias OnReadData(List<uint> serverAliases)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ElementValueListBase.");

                if (ReadEndpointEntry == null) throw RpcExceptionHelper.Create("List not attached to the IRead endpoint.");
                if (!Enabled) throw RpcExceptionHelper.Create("List not Enabled.");

                Debug.Assert(false, "This On Read Data method should not be invoked");
                throw RpcExceptionHelper.Create(XiFaultCodes.E_NOTIMPL, "IRead.ReadData");
            }
        }*/

        #endregion

        protected DataValueArraysWithAliasEx DataValueArraysWithAliasEx { get { return _dataValueArraysWithAliasEx; } }

        private DataValueArraysWithAliasEx _dataValueArraysWithAliasEx = new DataValueArraysWithAliasEx();
        /*
		/// <summary>
		/// Generally this method will be overridden in the implementation subclass.  
		/// The default behavior is to return the Data List Value in the cache.
		/// </summary>
		/// <param name="readRequests"></param>
		protected virtual DataValueArraysWithAlias OnReadData(List<TElementValueListItemBase> readRequests)
		{
			// No need to lock a return
			return null;
		}*/
    }
}