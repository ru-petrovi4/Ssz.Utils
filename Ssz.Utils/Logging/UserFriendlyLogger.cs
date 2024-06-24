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
    public class UserFriendlyLogger : SszLoggerBase
    {
        #region construction and destruction

        public UserFriendlyLogger(Action<LogLevel, EventId, string> writeLineAction)
        {
            _writeLineAction = writeLineAction;
        }

        #endregion

        #region public functions

        public static IUserFriendlyLogger Empty { get; } = new UserFriendlyLogger((logLevel, eventId, line) => { });

        public LogLevel LogLevel { get; set; } = LogLevel.Trace;

        public override bool IsEnabled(LogLevel logLevel)
        {
            if (LogLevel == LogLevel.None) return false;
            return logLevel >= LogLevel;
        }

        public override void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string line = $"{logLevel,-11} {DateTime.Now:O}";
            if (eventId.Id != 0) 
                line += $" ID: {eventId.Id}";
            line += " ";
            lock (SyncRoot)
            {
                line += GetScopesString();
            }
            try
            {
                line += formatter(state, exception);
            }
            catch
            {
                line += "<Invalid message params>";
            }
            Exception? ex = exception;
            if (ex is not null)
            {                
                line += "  Exception: " + ex.Message;
            }

            _writeLineAction(logLevel, eventId, line);
        }

        #endregion

        #region private fields

        private readonly Action<LogLevel, EventId, string> _writeLineAction;

        #endregion
    }
}
