using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.Logging
{    
    public abstract class SszLoggerBase : ILogger, IDisposable
    {
        #region construction and destruction

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {   
            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~SszLoggerBase()
        {
            Dispose(false);
        }        

        #endregion

        #region public functions

        public IDisposable BeginScope<TState>(TState state) => new Scope(this, new Any(state).ValueAsString(true));

        public abstract bool IsEnabled(LogLevel logLevel);

        public abstract void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter);

        #endregion

        #region protected functions

        protected bool Disposed => _disposed;

        protected List<string> ScopeStringsCollection { get; } = new();

        #endregion        

        #region private functions

        private void PushScope(string scopeString)
        {
            ScopeStringsCollection.Add(scopeString);
        }

        private void PopScope()
        {
            ScopeStringsCollection.RemoveAt(ScopeStringsCollection.Count - 1);
        }

        #endregion

        #region private fields

        private volatile bool _disposed;                

        #endregion

        private class Scope : IDisposable
        {
            #region construction and destruction

            public Scope(SszLoggerBase sszLogger, string scopeString)
            {
                _sszLogger  = sszLogger;
                _sszLogger.PushScope(scopeString);
            }

            public void Dispose()
            {
                _sszLogger.PopScope();
            }

            #endregion

            #region private fields

            private readonly SszLoggerBase _sszLogger;

            #endregion
        }        
    }
}
