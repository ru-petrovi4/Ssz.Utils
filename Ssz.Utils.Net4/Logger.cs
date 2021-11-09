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

        public static TraceSource TraceSource { get; private set; }

        public static LogFileTextWriter LogFileTextWriter { get; private set; }

        public static void SetLogLevels(SourceLevels sourceLevels)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.Switch.Level = sourceLevels;
            }
            catch
            {
            }
        }

        public static bool ShouldTrace(TraceEventType eventType)
        {
            if (TraceSource is null) return false;
            try
            {
                return TraceSource.Switch.ShouldTrace(eventType);
            }
            catch
            {
                return false;
            }
        }

        public static void Critical(string message)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Critical, 0, message);
            }
            catch
            {
            }
        }

        public static void Critical(string format, params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Critical, 0, format, args);
            }
            catch
            {
            }
        }

        public static void Critical(Exception e, string format = "", params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceException(TraceEventType.Critical, e, format, args);
            }
            catch
            {
            }
        }

        public static void Error(string message)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Error, 0, message);
            }
            catch
            {
            }
        }

        public static void Error(string format, params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Error, 0, format, args);
            }
            catch
            {
            }
        }

        public static void Error(Exception e, string format = "", params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceException(TraceEventType.Error, e, format, args);
            }
            catch
            {
            }
        }

        public static void Warning(string message)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Warning, 0, message);
            }
            catch
            {
            }
        }

        public static void Warning(string format, params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Warning, 0, format, args);
            }
            catch
            {
            }
        }

        public static void Warning(Exception e, string format = "", params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceException(TraceEventType.Warning, e, format, args);
            }
            catch
            {
            }
        }

        public static void Info(string message)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Information, 0, message);
            }
            catch
            {
            }
        }

        public static void Info(string format, params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Information, 0, format, args);
            }
            catch
            {
            }
        }

        public static void Info(Exception e, string format = "", params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceException(TraceEventType.Information, e, format, args);
            }
            catch
            {
            }
        }

        public static void Verbose(string message)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Verbose, 0, message);
            }
            catch
            {
            }
        }

        public static void Verbose(string format, params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Verbose, 0, format, args);
            }
            catch
            {
            }
        }

        public static void Verbose(Exception e, string format = "", params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceException(TraceEventType.Verbose, e, format, args);
            }
            catch
            {
            }
        }

        public static void Start(string message)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Start, 0, message);
            }
            catch
            {
            }
        }

        public static void Start(string format, params object[] args)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Start, 0, format, args);
            }
            catch
            {
            }
        }

        public static void Stop(string message)
        {
            if (TraceSource is null) return;
            try
            {
                TraceSource.TraceEvent(TraceEventType.Stop, 0, message);
            }
            catch
            {
            }
        }

        public static void Stop(string format, params object[] args)
        {
            if (TraceSource is null) return;
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
            if (TraceSource is null) return null;

            if (ShouldTrace(TraceEventType.Start))
            {
                var st = new StackTrace();
                StackFrame sf = st.GetFrame(1);
                MethodBase mb = sf.GetMethod();

                string functionWithParams = string.Format("{0}({1})", mb.Name,
                    String.Join(", ",
                        args.Select(
                            o => (o != null) ? o.ToString() : "<null>").
                            ToArray()));

                Start(functionWithParams);
                return new TraceCloser(mb.Name);
            }

            return null;
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
        public static void Initialize()
        {
            try
            {
                string SszAppDataDir = ConfigurationManager.AppSettings[IDS_LOGFILEPATH];
                if (string.IsNullOrEmpty(SszAppDataDir))
                    SszAppDataDir = DefaultFolder;
                SszAppDataDir = Environment.ExpandEnvironmentVariables(SszAppDataDir);
                if (!Directory.Exists(SszAppDataDir)) Directory.CreateDirectory(SszAppDataDir);

                string logFileName = ConfigurationManager.AppSettings[IDS_LOGFILENAME];
                if (string.IsNullOrEmpty(logFileName))
                {
                    logFileName = (new FileInfo(Process.GetCurrentProcess().MainModule.ModuleName)).Name;
                    logFileName = logFileName.Replace(".vshost", "");
                }

                LogFileTextWriter = new LogFileTextWriter(SszAppDataDir, logFileName);
                var textWriterTraceListener = new TextWriterTraceListener(LogFileTextWriter);
                textWriterTraceListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ThreadId;

                Trace.Listeners.Add(textWriterTraceListener);
                Trace.AutoFlush = true;

                TraceSource = new TraceSource(logFileName);
                TraceSource.Listeners.Clear();
                TraceSource.Listeners.Add(textWriterTraceListener);
#if DEBUG
                string sourceLevelsString = ConfigurationManager.AppSettings[IDS_LOGDEBUGLEVELS];
#else
                string sourceLevelsString = ConfigurationManager.AppSettings[IDS_LOGLEVELS];
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

        private static void TraceException(TraceEventType traceEventType, Exception ex, string format,
            params object[] args)
        {
            if (!ShouldTrace(traceEventType)) return;

            var st = new StackTrace();
            StackFrame sf = st.GetFrame(2);
            MethodBase mb = sf.GetMethod();

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
            message.Append(mb.Name);            

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

            TraceSource.TraceEvent(traceEventType, 0, message.ToString());
        }

        #endregion

        #region private fields

        private const string IDS_LOGFILEPATH = "LogFilePath";
        private const string IDS_LOGFILENAME = "LogFileName";
        private const string IDS_LOGLEVELS = "LogLevels";
        private const string IDS_LOGDEBUGLEVELS = "DebugLogLevels";

        #endregion

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