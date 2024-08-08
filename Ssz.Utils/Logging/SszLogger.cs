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
            string categoryName,
            string name,
            SszLoggerOptions options)
        {
            _categoryName = categoryName;
            _name = name;
            Options = options;

            lock (LogFileTextWriterSyncRoot)
            {
                if (LogFileTextWriter is null)
                {
                    LogFileTextWriter = new LogFileTextWriter(options);
                }
                else if (LogFileTextWriter.Options != options)
                {
                    LogFileTextWriter.Dispose();
                    LogFileTextWriter = new LogFileTextWriter(options);
                }
            }                
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
            if (!String.IsNullOrEmpty(_categoryName))
                header += $" [{_categoryName}]";
            if (eventId.Id != 0)
                header += $" Event ID: {eventId.Id}";            
            string content = "\t";

            lock (SyncRoot)
            {                
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
            }

            lock (LogFileTextWriterSyncRoot)
            {
                LogFileTextWriter!.WriteLine(header);
                LogFileTextWriter!.WriteLine(content);
            }            
        }

        #endregion

        #region private fields        

        private readonly string _categoryName;

        private readonly string _name;

        private static readonly object LogFileTextWriterSyncRoot = new();

        private static LogFileTextWriter? LogFileTextWriter;

        #endregion
    }
}
