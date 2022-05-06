using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Logging
{
    public class WrapperUserFriendlyLogger : IUserFriendlyLogger
    {
        #region construction and destruction

        public WrapperUserFriendlyLogger(ILogger? logger)
        {
            Logger = logger;
        }

        #endregion

        #region public functions

        public ILogger? Logger { get; }

        public IDisposable BeginScope<TState>(TState state)
        {
            return Logger?.BeginScope(state) ?? Disposable.Empty;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Logger?.IsEnabled(logLevel) ?? false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logger?.Log(logLevel, eventId, state, exception, formatter);
        }

        #endregion        
    }
}
