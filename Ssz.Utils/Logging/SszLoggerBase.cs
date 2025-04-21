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
    public abstract class SszLoggerBase : ILogger, IUserFriendlyLogger
    {
        #region public functions

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            var t = state.GetType();                            
            if (t.Name == typeof(ValueTuple<,>).Name)
                return new Scope(this, 
                    (new Any(t.GetField("Item1")?.GetValue(state)).ValueAsString(true), t.GetField("Item2")?.GetValue(state)));  
            else if (state is Array array)
            {
                List<(string, object?)> scopeTuples = new(array.Length);
                foreach (int i in Enumerable.Range(0, array.Length))
                {
                    object? o = array.GetValue(i);
                    if (o is null) 
                        continue;
                    t = o.GetType();
                    if (t.Name == typeof(ValueTuple<,>).Name)
                        scopeTuples.Add(
                            (new Any(t.GetField("Item1")?.GetValue(o)).ValueAsString(true), t.GetField("Item2")?.GetValue(o)));
                    else
                        scopeTuples.Add((@"", new Any(o).ValueAsString(true)));
                }
                return new ScopesSet(this, scopeTuples);
            }            
            else
                return new Scope(this, (@"", new Any(state).ValueAsString(true)));
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

        protected object SyncRoot { get; } = new();

        /// <summary>
        ///     Lock SyncRoot before use
        /// </summary>
        protected List<(string, object?)> ScopesStack { get; } = new();

        /// <summary>
        ///     Lock SyncRoot before use
        /// </summary>
        /// <param name="excludeScopeNames"></param>
        /// <returns></returns>
        protected string GetScopesString(string[]? excludeScopeNames = null)
        {
            string line = @"";
            foreach (var scope in ScopesStack.Distinct())
            {
                var scopeName = scope.Item1;
                string scopeValue;
                if (scope.Item2 is null)
                    scopeValue = @"<null>";
                else
                    scopeValue = EscapeScopeString(new Any(scope.Item2).ValueAsString(true));
                if (String.IsNullOrEmpty(scopeName))
                {
                    line += scopeValue + @"; ";
                }
                else
                {
                    if (excludeScopeNames is not null && excludeScopeNames.Contains(scopeName, StringComparer.InvariantCultureIgnoreCase))
                        continue;
                    line += EscapeScopeString(scopeName) + ": " + scopeValue + @"; ";
                }
            }
            return line;
        }

        /// <summary>
        ///     Lock SyncRoot before use
        ///     Returns null if not found.
        /// </summary>
        /// <param name="scopeName"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected object? TryGetScopeValue(string scopeName)
        {
            return ScopesStack.FirstOrDefault(s => String.Equals(s.Item1, scopeName, StringComparison.InvariantCultureIgnoreCase)).Item2;
        }

        protected static string EscapeScopeString(string scopeString)
        {
            if (String.IsNullOrEmpty(scopeString))
                return "\"\"";
            if (scopeString.Contains(':') ||
                        scopeString.Contains(';') ||
                        scopeString.Contains('"'))
                scopeString = "\"" + scopeString.Replace("\"", "\"\"") + "\"";
            return scopeString;
        }

        #endregion        

        #region private functions

        private void PushScope((string, object?) scopeTuple)
        {
            lock (SyncRoot)
            {
                ScopesStack.Add(scopeTuple);
            }
        }

        private void PopScope()
        {
            lock (SyncRoot)
            {
                ScopesStack.RemoveAt(ScopesStack.Count - 1);
            }
        }

        private void PushScopes(List<(string, object?)> scopeTuples)
        {
            lock (SyncRoot)
            {
                ScopesStack.AddRange(scopeTuples);
            }                
        }

        private void PopScopes(int count)
        {
            lock (SyncRoot)
            {
                ScopesStack.RemoveRange(ScopesStack.Count - count, count);
            }
        }        

        #endregion        

        private class Scope : IDisposable
        {
            #region construction and destruction

            public Scope(SszLoggerBase sszLogger, (string, object?) scopeTuple)
            {
                _sszLogger = sszLogger;                
                _sszLogger.PushScope(scopeTuple);
            }

            public void Dispose()
            {
                var sszLogger = Interlocked.Exchange(ref _sszLogger, null);
                if (sszLogger is not null)
                {
                    sszLogger.PopScope();                    
                }
            }

            #endregion

            #region private fields

            private SszLoggerBase? _sszLogger;

            #endregion
        }

        private class ScopesSet : IDisposable
        {
            #region construction and destruction

            public ScopesSet(SszLoggerBase sszLogger, List<(string, object?)> scopeTuples)
            {
                _sszLogger = sszLogger;
                _scopeTuplesCount = scopeTuples.Count;
                _sszLogger.PushScopes(scopeTuples);
            }

            public void Dispose()
            {
                var sszLogger = Interlocked.Exchange(ref _sszLogger, null);
                if (sszLogger is not null)
                {
                    sszLogger.PopScopes(_scopeTuplesCount);
                }
            }

            #endregion

            #region private fields

            private SszLoggerBase? _sszLogger;

            private int _scopeTuplesCount;

            #endregion
        }
    }
}
