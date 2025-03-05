using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.DataAccess
{
    public interface IDataAccessServerWorker
    {
        public ThreadSafeDispatcher ThreadSafeDispatcher { get; }

        IDataAccessServerContext AddServerContext(ILogger logger,
            string clientApplicationName,
            string clientWorkstationName,
            uint requestedServerContextTimeoutMs,
            string requestedCultureName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams);

        IDataAccessServerContext LookupServerContext(string contextId);

        IDataAccessServerContext? TryLookupServerContext(string contextId);

        void RemoveServerContext(IDataAccessServerContext serverContext);
    }
}
