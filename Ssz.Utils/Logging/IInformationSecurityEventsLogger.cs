using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Logging
{
    public interface IInformationSecurityEventsLogger : IUserFriendlyLogger
    {        
    }

    public class NullInformationSecurityEventsLoggers : IInformationSecurityEventsLogger
    {
        public static readonly NullInformationSecurityEventsLoggers Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {            
        }
    }
}