using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ssz.Utils.Logging
{
    public class WrapperUserFriendlyLogger : IUserFriendlyLogger
    {
        #region construction and destruction

        public WrapperUserFriendlyLogger(params ILogger?[] loggers)
        {
            Loggers = loggers.Where(l => l is not null).OfType<ILogger>().ToArray();
        }

        #endregion

        #region public functions

        public ILogger[] Loggers { get; }

        /// <summary>
        ///     Returns one disposable for all sub-loggers scopes.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {            
            return new ComplexScope(Loggers.Select(l => l.BeginScope(state)).ToArray());
        }

        public string GetScopesString(string[]? excludeScopeNames = null)
        {
            var logger = Loggers.OfType<IUserFriendlyLogger>().FirstOrDefault();
            if (logger is null)
                return string.Empty;
            return logger.GetScopesString(excludeScopeNames);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Loggers.Any(l => l.IsEnabled(logLevel));
        }

        /// <summary>
        ///     Logs to all sub-loggers
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Array.ForEach(Loggers, l => l.Log(logLevel, eventId, state, exception, formatter));
        }

        #endregion

        private class ComplexScope : IDisposable
        {
            public ComplexScope(IDisposable?[] disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                Array.ForEach(_disposables, d => d?.Dispose());
            }

            private IDisposable?[] _disposables;
        }
    }
}
