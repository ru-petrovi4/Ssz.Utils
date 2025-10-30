using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using Grpc.Core;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;


namespace Ssz.DataAccessGrpc.ServerBase
{
    /// <summary>
    ///   This class is intended to be used as the root or base class for 
    ///   all list types.  The attribute, properties and methods defined 
    ///   in this class are common to all list types.
    /// </summary>
    public abstract class ServerListRoot
    {
        #region construction and destruction
        
        public ServerListRoot(DataAccessServerWorkerBase dataAccessServerWorker, ServerContext serverContext, uint listClientAlias, CaseInsensitiveOrderedDictionary<string?> listParams)
        {
            ServerWorker = dataAccessServerWorker;
            ServerContext = serverContext;
            ListClientAlias = listClientAlias;
            ListParams = listParams;
        }

        /// <summary>
        ///   This is the implementation of the IDisposable.Dispose method.  The client 
        ///   application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   This method is invoked when the IDisposable.Dispose or Finalize actions are 
        ///   requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            Disposed = true;
        }

        /// <summary>
        ///   Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~ServerListRoot()
        {
            Dispose(false);
        }

        #endregion        

        #region public functions

        public bool Disposed { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ServerContext ServerContext { get; }

        /// <summary>
        /// 
        /// </summary>
        public uint ListClientAlias { get; }

        public CaseInsensitiveOrderedDictionary<string?> ListParams { get; }

        public bool ListCallbackIsEnabled { get; protected set; }

        public virtual Task<List<Utils.DataAccess.AliasResult>> AddItemsToListAsync(List<Utils.DataAccess.ListItemInfo> itemsToAdd)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        /// <summary>
        ///     Returns failed AliasResultMessages only.
        /// </summary>
        /// <param name="serverAliasesToRemove"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public virtual Task<List<Utils.DataAccess.AliasResult>> RemoveItemsFromListAsync(List<uint> serverAliasesToRemove)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual void EnableListCallback(bool enable)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual void TouchList()
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual ElementValuesCallbackMessage? GetElementValuesCallbackMessage()
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual List<EventMessagesCallbackMessage>? GetEventMessagesCallbackMessages()
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }
        
        public virtual Task<ElementValuesJournal[]> ReadElementValuesJournalsAsync(            
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,
            uint numValuesPerAlias,
            Ssz.Utils.DataAccess.TypeId calculation,
            CaseInsensitiveOrderedDictionary<string?> params_,
            List<uint> serverAliases)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual Task<EventMessagesCallbackMessage> ReadEventMessagesJournalAsync(
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,            
            CaseInsensitiveOrderedDictionary<string?> params_)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        /// <summary>
        ///     Returns failed AliasResultMessages only.
        /// </summary>
        /// <param name="elementValuesCollection"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public async Task<List<Utils.DataAccess.AliasResult>?> WriteElementValuesAsync(ReadOnlyMemory<byte> elementValuesCollectionBytes)
        {
            List<(uint, ValueStatusTimestamp)> elementValuesCollection = new();

            using (var stream = elementValuesCollectionBytes.AsStream())
            using (var reader = new SerializationReader(stream))
            {
                using (Block block = reader.EnterBlock())
                {
                    switch (block.Version)
                    {
                        case 1:
                            int count = reader.ReadInt32();
                            for (int index = 0; index < count; index += 1)
                            {
                                uint alias = reader.ReadUInt32();
                                ValueStatusTimestamp vst = new();
                                vst.DeserializeOwnedData(reader, null);

                                elementValuesCollection.Add((alias, vst));
                            }
                            break;
                        default:
                            throw new BlockUnsupportedVersionException();
                    }
                }
            }

            return await WriteElementValuesAsync(elementValuesCollection);
        }

        public virtual List<Utils.DataAccess.EventIdResult> AckAlarms(string operatorName, string comment, IEnumerable<Ssz.Utils.DataAccess.EventId> eventIdsToAck)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual void DoWork(DateTime nowUtc, CancellationToken token)
        {            
        }

        /// <summary>
        ///     Reset list to initial state.
        /// </summary>
        public virtual void ResetList()
        {
        }

        /// <summary>
        ///   The number of queue entries that have been discarded since the last poll.
        /// </summary>
        public DateTime LastCallbackTime { get; set; } = DateTime.MinValue;

        #endregion

        #region protected functions        

        /// <summary>
        ///     Returns failed AliasResultMessages only.
        /// </summary>
        /// <param name="elementValuesCollection"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        protected virtual Task<List<Utils.DataAccess.AliasResult>> WriteElementValuesAsync(List<(uint, ValueStatusTimestamp)> elementValuesCollection)
        {
            return Task.FromResult(new List<Utils.DataAccess.AliasResult>());
        }

        protected DataAccessServerWorkerBase ServerWorker { get; }

        #endregion
    }
}