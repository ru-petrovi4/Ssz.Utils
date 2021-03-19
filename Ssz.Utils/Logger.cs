using Ssz.Utils.Logging;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ssz.Utils
{
    /// <summary>
    ///     This class provides tracing support in a concrete
    ///     singleton implementation so any other type can get to the stored TraceSource.
    /// </summary>
    public static class Logger
    {
        #region public functions

        /// <summary>
        ///     When null, writes no log.
        /// </summary>
        public static TraceSource? TraceSource { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public static LogFileTextWriter? LogFileTextWriter { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceLevels"></param>
        public static void SetLogLevels(SourceLevels sourceLevels)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.Switch.Level = sourceLevels;
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        public static bool ShouldTrace(TraceEventType eventType)
        {
            if (TraceSource == null) return false;
            try
            {
                return TraceSource.Switch.ShouldTrace(eventType);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Critical(string message)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Critical, 0, message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Critical(string format, params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Critical, 0, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Critical(Exception e, string format = "", params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceException(TraceEventType.Critical, e, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Error, 0, message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Error(string format, params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Error, 0, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Error(Exception e, string format = "", params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceException(TraceEventType.Error, e, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Warning(string message)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Warning, 0, message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Warning(string format, params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Warning, 0, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Warning(Exception e, string format = "", params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceException(TraceEventType.Warning, e, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Information, 0, message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Info(string format, params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Information, 0, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Info(Exception e, string format = "", params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceException(TraceEventType.Information, e, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Verbose(string message)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Verbose, 0, message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Verbose(string format, params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Verbose, 0, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Verbose(Exception e, string format = "", params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceException(TraceEventType.Verbose, e, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Start(string message)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Start, 0, message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Start(string format, params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Start, 0, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void Stop(string message)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Stop, 0, message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Stop(string format, params object[] args)
        {
            if (TraceSource == null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Stop, 0, format, args);
            }
            catch
            {
            }
        }

        /// <summary>
        ///     This is used to log method entries.
        /// </summary>
        /// <param name="args"> Arguments </param>
        /// <returns> TraceCloser object to log exit of method </returns>
        public static IDisposable EnterMethod(params object[] args)
        {
            if (TraceSource == null) return EmptyDisposable.Instance;

            if (ShouldTrace(TraceEventType.Start))
            {
                var st = new StackTrace();
                StackFrame? sf = st.GetFrame(1);
                MethodBase? mb = sf?.GetMethod();
                string methodName = mb?.Name ?? "";
                string functionWithParams = string.Format("{0}({1})", methodName,
                    String.Join(", ",
                        args.Select(
                            o => (o != null) ? o.ToString() : "<null>").
                            ToArray()));

                Start(functionWithParams);
                return new TraceCloser(methodName);
            }

            return EmptyDisposable.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="traceSource"></param>
        public static void Initialize(TraceSource traceSource)
        {
            TraceSource = traceSource;
        }

        /// <summary>
        ///     Initializes logger. If it is not called, log doesn't write.
        ///     Reads Logger settings from App.config.
        ///     Initializes Trace class to use same log file.         
        /// </summary>  
        /// <param name="duplicateInConsole"></param>        
        public static void Initialize(bool duplicateInConsole = false)
        {
            try
            {                
                LogFileTextWriter = new LogFileTextWriter(new SszLoggerOptions());
                var textWriterTraceListener = new TextWriterTraceListener(LogFileTextWriter);
                textWriterTraceListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ThreadId;

                Trace.Listeners.Add(textWriterTraceListener);
                Trace.AutoFlush = true;

                TraceSource = new TraceSource(LogFileTextWriter.LogFileName);
                TraceSource.Listeners.Clear();
                TraceSource.Listeners.Add(textWriterTraceListener);  
                
                if (duplicateInConsole)
                {
                    var consoleTraceListener = new ConsoleTraceListener();
                    Trace.Listeners.Add(consoleTraceListener);
                    TraceSource.Listeners.Add(consoleTraceListener);
                }
                // TODO:
#if DEBUG
                string sourceLevelsString = ""; // ConfigurationManager.AppSettings[IDS_LOGDEBUGLEVELS];
#else
                string sourceLevelsString = ""; // ConfigurationManager.AppSettings[IDS_LOGLEVELS];
#endif
                SourceLevels sourceLevels;
                if (!Enum.TryParse(sourceLevelsString, true, out sourceLevels)) sourceLevels = SourceLevels.Error;
                TraceSource.Switch.Level = sourceLevels;
            }
            catch
            {
            }
        }

        /*
        We don't need this method, because Trace.AutoFlush = true;
        /// <summary>
        ///     Flushes log data if any.
        /// </summary>
        public static void Close()
        {
            try
            {
                if (LogTextWriter != null)
                {
                    LogTextWriter.Dispose();
                }
            }
            catch
            {
            }
        }*/

        #endregion

        #region internal functions

        internal static string DefaultFolder = @"%ALLUSERSPROFILE%\Application Data\Ssz";

        #endregion

        #region private functions

        private static void TraceException(TraceEventType traceEventType, Exception? ex, string format,
            params object[] args)
        {
            if (!ShouldTrace(traceEventType)) return;

            var st = new StackTrace();
            StackFrame? sf = st.GetFrame(2);
            MethodBase? mb = sf?.GetMethod();

            var message = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(format))
            {                
                message.AppendFormat(format, args);
            }
            else
            {
                message.Append("Exception");
            }

            message.Append("\n");
            message.Append("Method: ");
            message.Append(mb?.Name);            

            while (ex != null)
            {
                message.Append("\n");
                message.Append("Exception: ");
                message.Append(ex.Message);

                message.Append("\n");
                message.Append("StackTrace: ");
                message.Append(ex.StackTrace);                

                ex = ex.InnerException;
            }

            TraceSource?.TraceEvent(traceEventType, 0, message.ToString());
        }

        #endregion

        #region private fields

        private const string IDS_LOGFILEPATH = "LogFilePath";
        private const string IDS_LOGFILENAME = "LogFileName";
        private const string IDS_LOGLEVELS = "LogLevels";
        private const string IDS_LOGDEBUGLEVELS = "DebugLogLevels";

        #endregion

        private class EmptyDisposable : IDisposable
        {
            #region construction and destruction            

            public void Dispose()
            {                
            }

            #endregion

            #region public functions

            public static readonly EmptyDisposable Instance = new EmptyDisposable();

            #endregion
        }

        private class TraceCloser : IDisposable
        {
            #region construction and destruction

            public TraceCloser(string function)
            {
                _function = function;
            }

            public void Dispose()
            {
                Stop(_function);
            }

            #endregion

            #region private fields

            private readonly string _function;

            #endregion
        }
    }
}