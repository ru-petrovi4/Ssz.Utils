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

        public UserFriendlyLogger(Action<string> writeLineAction)
        {
            _writeLineAction = writeLineAction;
        }

        #endregion

        #region public functions

        public override bool IsEnabled(LogLevel logLevel) => true;

        public override void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {            
            string line = $"{logLevel,-12}";
            lock (ScopeStringsStack)
            {
                foreach (var scopeString in ScopeStringsStack.Reverse())
                {
                    line += scopeString + @" -> ";
                }
            }                
            line += formatter(state, exception);
            Exception? ex = exception;
            if (ex is not null)
            {                
                line += "\tException: " + ex.Message;
            }

            _writeLineAction(line);
        }

        #endregion

        #region private fields

        private readonly Action<string> _writeLineAction;

        #endregion
    }
}
