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
    public abstract class SszLoggerBase : ILogger, IUserFriendlyLogger, IDisposable
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

        public CaseInsensitiveDictionary<string?> Fields { get; set; } = new();

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

        protected Stack<string> ScopeStringsStack { get; } = new();

        #endregion        

        #region private functions

        private void PushScope(string scopeString)
        {
            lock (ScopeStringsStack)
            {
                ScopeStringsStack.Push(scopeString);
            }                
        }

        private void PopScope()
        {
            lock (ScopeStringsStack)
            {
                ScopeStringsStack.Pop();
            }
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
                var sszLogger = Interlocked.Exchange(ref _sszLogger, null);
                if (sszLogger != null)
                {
                    sszLogger.PopScope();                    
                }
            }

            #endregion

            #region private fields

            private SszLoggerBase? _sszLogger;

            #endregion
        }        
    }
}
