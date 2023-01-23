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

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {        
            if (state is ValueTuple<string, string> valueTuple)
                return new FieldScope(this, valueTuple.Item1, valueTuple.Item2);
            else
                return new StringScope(this, new Any(state).ValueAsString(true));
        }

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

        protected object SyncRoot { get; } = new();

        /// <summary>
        ///     Lock SyncRoot before use
        /// </summary>
        protected Stack<string> ScopeStringsStack { get; } = new();

        /// <summary>
        ///     Lock SyncRoot before use
        /// </summary>
        protected CaseInsensitiveDictionary<string?> Fields { get; } = new();

        #endregion        

        #region private functions

        private void PushScopeString(string scopeString)
        {
            lock (SyncRoot)
            {
                ScopeStringsStack.Push(scopeString);
            }                
        }

        private void PopScopeString()
        {
            lock (SyncRoot)
            {
                ScopeStringsStack.Pop();
            }
        }

        private void AddField(string fieldName, string? fieldValue)
        {
            lock (SyncRoot)
            {
                Fields[fieldName] = fieldValue;
            }
        }

        private void RemoveField(string fieldName)
        {
            lock (SyncRoot)
            {
                Fields.Remove(fieldName);
            }
        }

        #endregion

        #region private fields

        private volatile bool _disposed;                

        #endregion

        private class StringScope : IDisposable
        {
            #region construction and destruction

            public StringScope(SszLoggerBase sszLogger, string scopeString)
            {
                _sszLogger  = sszLogger;                
                _sszLogger.PushScopeString(scopeString);
            }

            public void Dispose()
            {
                var sszLogger = Interlocked.Exchange(ref _sszLogger, null);
                if (sszLogger is not null)
                {
                    sszLogger.PopScopeString();                    
                }
            }

            #endregion

            #region private fields

            private SszLoggerBase? _sszLogger;

            #endregion
        }

        private class FieldScope : IDisposable
        {
            #region construction and destruction

            public FieldScope(SszLoggerBase sszLogger, string fieldName, string fieldValue)
            {
                _sszLogger = sszLogger;
                _fieldName = fieldName;
                _sszLogger.AddField(fieldName, fieldValue);
            }

            public void Dispose()
            {
                var sszLogger = Interlocked.Exchange(ref _sszLogger, null);
                if (sszLogger is not null)
                {
                    sszLogger.RemoveField(_fieldName);
                }
            }

            #endregion

            #region private fields

            private SszLoggerBase? _sszLogger;

            private readonly string _fieldName;

            #endregion
        }
    }
}
