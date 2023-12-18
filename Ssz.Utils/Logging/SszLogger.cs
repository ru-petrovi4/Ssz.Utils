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

            _timer = new Timer(OnTimerCallback, null, 5000, 5000);
        }        
        
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _timer.Dispose();
                lock (SyncRoot)
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
            if (Options.LogLevelIsExclusive)
                return logLevel == Options.LogLevel;
            else
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
                return;

            string header = $"{logLevel,-11} {DateTime.Now:O}";
            if (eventId.Id != 0)
                header += $" ID: {eventId.Id}";
            
            lock (SyncRoot)
            {
                string content = "\t";
                content += GetScopesString();
                try
                {
                    content += formatter(state, exception);
                }
                catch
                {
                    content += "<Invalid message params>";
                }
                Exception? ex = exception;
                while (ex is not null)
                {
                    content += "\n\tException: ";
                    content += ex.Message;
                    content += "\n";
                    content += ex.StackTrace;

                    ex = ex.InnerException;
                }

                ConsoleColor originalColor = Console.ForegroundColor;
                switch (logLevel)
                {
                    case LogLevel.Critical:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        break;
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogLevel.Information:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                }
                Console.WriteLine(header);
                Console.ForegroundColor = originalColor;
                Console.WriteLine(content);

                _logFileTextWriter.WriteLine(header);
                _logFileTextWriter.WriteLine(content);            
            }                        
        }

        #endregion        

        #region private functions

        private void OnTimerCallback(object? state)
        {
            if (Disposed) return;

            lock (SyncRoot)
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
