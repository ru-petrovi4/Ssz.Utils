using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Ssz.Utils;


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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverContext"></param>
        /// <param name="listClientAlias"></param>
        /// <param name="listParams"></param>
        public ServerListRoot(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
        {
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

        public virtual List<AddItemToListResult> AddItemsToList(List<ListItemInfo> itemsToAdd)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual List<AliasResult> RemoveItemsFromList(List<uint> serverAliasesToRemove)
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

        public virtual ServerContext.EventMessagesCallbackMessage? GetNextEventMessagesCallbackMessage()
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }
        
        public virtual Task<ElementValuesJournalsCollection> ReadElementValuesJournalsAsync(            
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

        public virtual List<AliasResult> WriteElementValues(ElementValuesCollection elementValuesCollection)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid List Type for this Request."));
        }

        public virtual List<EventIdResult> AckAlarms(string operatorName, string comment,
                                                               IEnumerable<EventId> eventIdsToAck)
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
    }
}