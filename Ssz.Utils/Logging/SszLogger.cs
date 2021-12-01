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
    public class SszLogger : SszLoggerBase
    {
        #region construction and destruction

        public SszLogger(
            string name,
            SszLoggerOptions options)
        {
            (_name, Options) = (name, options);

            _logFileTextWriter = new LogFileTextWriter(options);

            _timer = new Timer(OnTimerCallback, null, 1000, 1000);
        }        
        
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _timer.Dispose();
                lock (_logFileTextWriter)
                {
                    _logFileTextWriter.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public SszLoggerOptions Options { get; set; }

        public override bool IsEnabled(LogLevel logLevel)
        {
            if (Options.LogLevel == LogLevel.None) return false;
            return logLevel >= Options.LogLevel;
        }

        public override void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string line1;
            if (eventId.Id != 0)
                line1 = $"[ {eventId.Id}: {logLevel,-11} ]";
            else
                line1 = $"[ {logLevel,-11} ]";

            string line2 = "\t";
            foreach (var scopeString in ScopeStringsCollection)
            {
                line2 += scopeString + @" -> ";
            }
            line2 += formatter(state, exception);
            Exception? ex = exception;
            while (ex is not null)
            {
                line2 += "\n";
                line2 += "\tException: ";
                line2 += ex.Message;

                line2 += "\n";
                line2 += "\tStackTrace: ";
                line2 += ex.StackTrace;

                ex = ex.InnerException;
            }

            if (Options.DuplicateInConsole)
            {
                ConsoleColor originalColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(line1);

                Console.ForegroundColor = originalColor;
                Console.WriteLine(line2);
            }
            lock (_logFileTextWriter)
            {
                _logFileTextWriter.WriteLine(line1);

                _logFileTextWriter.WriteLine(line2);
            }
        }

        #endregion        

        #region private functions

        private void OnTimerCallback(object? state)
        {
            if (Disposed) return;

            lock (_logFileTextWriter)
            {
                _logFileTextWriter.Flush();
            }            
        }

        #endregion

        #region private fields

        private Timer _timer;

        private readonly string _name;

        private readonly LogFileTextWriter _logFileTextWriter;

        #endregion
    }
}
