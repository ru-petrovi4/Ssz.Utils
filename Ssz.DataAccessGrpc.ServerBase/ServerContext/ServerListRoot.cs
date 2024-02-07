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
        
        public ServerListRoot(ServerWorkerBase serverWorker, ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
        {
            ServerWorker = serverWorker;
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

        public CaseInsensitiveDictionary<string?> ListParams { get; }

        public bool ListCallbackIsEnabled { get; protected set; }

        public virtual Task<List<AliasResult>> AddItemsToListAsync(List<ListItemInfo> itemsToAdd)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="serverAliasesToRemove"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public virtual Task<List<AliasResult>> RemoveItemsFromListAsync(List<uint> serverAliasesToRemove)
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

        public virtual ServerContext.ElementValuesCallbackMessage? GetElementValuesCallbackMessage()
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual List<ServerContext.EventMessagesCallbackMessage>? GetEventMessagesCallbackMessages()
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }
        
        public virtual Task<ElementValuesJournal[]> ReadElementValuesJournalsAsync(            
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,
            uint numValuesPerAlias,
            TypeId calculation,
            CaseInsensitiveDictionary<string?> params_,
            List<uint> serverAliases)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual Task<ServerContext.EventMessagesCallbackMessage> ReadEventMessagesJournalAsync(
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,            
            CaseInsensitiveDictionary<string?> params_)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="elementValuesCollection"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public async Task<List<AliasResult>?> WriteElementValuesAsync(ReadOnlyMemory<byte> elementValuesCollectionBytes)
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

        public virtual List<EventIdResult> AckAlarms(string operatorName, string comment, IEnumerable<EventId> eventIdsToAck)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual void DoWork(DateTime nowUtc, CancellationToken token)
        {            
        }

        /// <summary>
        ///     Force to send all data.
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        ///   The number of queue entries that have been discarded since the last poll.
        /// </summary>
        public DateTime LastCallbackTime { get; set; } = DateTime.MinValue;

        #endregion

        #region protected functions        

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="elementValuesCollection"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        protected virtual Task<List<AliasResult>> WriteElementValuesAsync(List<(uint, ValueStatusTimestamp)> elementValuesCollection)
        {
            return Task.FromResult(new List<AliasResult>());
        }

        protected ServerWorkerBase ServerWorker { get; }

        #endregion
    }
}