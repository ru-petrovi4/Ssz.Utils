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

        public WrapperUserFriendlyLogger(params ILogger[] loggers)
        {
            Loggers = loggers;
        }

        #endregion

        #region public functions

        public ILogger[] Loggers { get; }

        public IDisposable BeginScope<TState>(TState state)
        {            
            return new ComplexScope(Loggers.Select(l => l.BeginScope(state)).ToArray());
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Loggers.Any(l => IsEnabled(logLevel));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Array.ForEach(Loggers, l => l.Log(logLevel, eventId, state, exception, formatter));
        }

        #endregion

        private class ComplexScope : IDisposable
        {
            public ComplexScope(IDisposable[] disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                Array.ForEach(_disposables, d => d.Dispose());
            }

            private IDisposable[] _disposables;
        }
    }
}
