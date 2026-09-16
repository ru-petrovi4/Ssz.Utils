using Microsoft.Extensions.Logging;
using System;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Writes to the browser console. Without it the browser build logs into NullLogger, so a
    ///     failure inside DsProject leaves no trace anywhere: the loading screen just reports that
    ///     the project could not be opened, with no reason.
    /// </summary>
    public class BrowserConsoleLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            Console.WriteLine("[" + logLevel + "] " + formatter(state, exception));
            if (exception is not null)
                Console.WriteLine(exception.ToString());
        }
    }
}
