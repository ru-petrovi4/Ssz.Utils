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
    public class SszLogger : ILogger, IDisposable
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _timer.Dispose();
                lock (_logFileTextWriter)
                {
                    _logFileTextWriter.Dispose();
                }
            }
            
            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~SszLogger()
        {
            Dispose(false);
        }

        #endregion

        public SszLoggerOptions Options { get; set; }

        public IDisposable? BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel)
        {
            if (Options.LogLevel == LogLevel.None) return false;
            return logLevel >= Options.LogLevel;
        }   

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (Options.EventId != 0 && Options.EventId != eventId.Id)
            {
                return;
            }

            string line = $"     {_name} - {formatter(state, exception)}";
            Exception? ex = exception;
            while (ex != null)
            {
                line += "\n";
                line += "Exception: ";
                line += ex.Message;

                line += "\n";
                line += "StackTrace: ";
                line += ex.StackTrace;

                ex = ex.InnerException;
            }

            if (Options.DuplicateInConsole)
            {
                ConsoleColor originalColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");

                Console.ForegroundColor = originalColor;                
                Console.WriteLine(line);
            }
            lock (_logFileTextWriter)
            {
                _logFileTextWriter.WriteLine($"[{eventId.Id,2}: {logLevel,-12}]");

                _logFileTextWriter.WriteLine(line);
            }                       
        }

        #region private functions

        private void OnTimerCallback(object? state)
        {
            if (_disposed) return;

            lock (_logFileTextWriter)
            {
                _logFileTextWriter.Flush();
            }            
        }

        #endregion

        #region private fields

        private volatile bool _disposed;

        private Timer _timer;

        private readonly string _name;

        private readonly LogFileTextWriter _logFileTextWriter;

        #endregion
    }
}
