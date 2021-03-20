using Ssz.DataGrpc.Server.Core.Context;
using System;

namespace Ssz.DataGrpc.Server.Data
{
    /// <summary>
    ///   This event is raised when the context collection is changed.
    ///   The removal of a context may be a due to closing the context or the context timed out.
    /// </summary>
    /// <typeparam name = "ServerContext"></typeparam>
    public class ServerContextsCollectionChangedEventArgs : EventArgs
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addedServerContext"></param>
        /// <param name="removedServerContext"></param>
        internal ServerContextsCollectionChangedEventArgs(ServerContext? addedServerContext, ServerContext? removedServerContext)
        {
            AddedServerContext = addedServerContext;
            RemovedServerContext = removedServerContext;
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        public ServerContext? AddedServerContext { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ServerContext? RemovedServerContext { get; private set; }

        #endregion
    }
}