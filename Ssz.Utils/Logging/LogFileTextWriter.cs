using Microsoft.Extensions.FileProviders;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ssz.Utils.Logging
{
    /// <summary>
    /// 
    /// </summary>
    public class LogFileTextWriter : TextWriter, IDisposable
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public LogFileTextWriter(SszLoggerOptions options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(null)
        {
            Options = options;

            _logsDirectoryFullName = Environment.ExpandEnvironmentVariables(Options.LogsDirectory);
            
            if (String.IsNullOrEmpty(_logsDirectoryFullName))
            {
                _logsDirectoryFullName = Directory.GetCurrentDirectory();
            }

            // Creates all directories and subdirectories in the specified path unless they
            // already exist.
            Directory.CreateDirectory(_logsDirectoryFullName);            

            if (!String.IsNullOrEmpty(Options.LogFileName))
            {
                SetLogFileFullName(Options.LogFileName);
            }
            else
            {
                Process currentProcess = Process.GetCurrentProcess();
                string? moduleName = currentProcess.MainModule?.ModuleName;
                if (moduleName is null) 
                    throw new InvalidOperationException();
                var exeFileName = new FileInfo(moduleName).Name;

                SetLogFileFullName(exeFileName + @".log");                
            }

            if (Options.DeleteOldFilesAtStart)
            {
                var fileInfos = Directory.GetFiles(_logsDirectoryFullName, _suggestedLogFileNameWithoutExtension + @".*" + _suggestedLogFileNameExtension)
                    .Select(n => new FileInfo(n))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();
                foreach (FileInfo fi in fileInfos.ToArray())
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch
                    {
                    }
                }
            }

            _timer = new Timer(s => Flush(), null, 5000, 5000);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                Flush();
            }

            base.Dispose(disposing);

            _disposed = true;
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        public SszLoggerOptions Options { get; }

        /// <summary>
        /// 
        /// </summary>
        public string LogFileFullName { get; private set; }        

        /// <summary>
        /// 
        /// </summary>
        public override void Flush()
        {
            if (_disposed) return;

            lock (_syncRoot)
            {
                if (_buffer.Length > 0)
                {
                    try
                    {
                        if (Options.LogFileMaxSizeInBytes > 0 && Options.LogFileMaxSizeInBytes < Int64.MaxValue)
                        {
                            var fi = new FileInfo(LogFileFullName);
                            if (fi.Exists && fi.Length > Options.LogFileMaxSizeInBytes)
                            {
                                SetLogFileFullName(_suggestedLogFileNameWithoutExtension + _suggestedLogFileNameExtension);
                            }
                        }
                        File.AppendAllText(LogFileFullName, _buffer.ToString(), new UTF8Encoding(true));
                        _buffer.Clear();
                    }
                    catch
                    {
                    }
                }

                #region DeleteOldFiles

                var nowUtc = DateTime.UtcNow;
                if (nowUtc - _oldFilesClearingDateTimeUtc > TimeSpan.FromMinutes(5))
                {
                    _oldFilesClearingDateTimeUtc = nowUtc;

                    var fileInfos = Directory.GetFiles(_logsDirectoryFullName, _suggestedLogFileNameWithoutExtension + @".*" + _suggestedLogFileNameExtension)
                        .Select(n => new FileInfo(n))
                        .OrderByDescending(f => f.LastWriteTime)
                        .ToList();

                    if (Options.DaysCountToStoreFiles > 0 && Options.DaysCountToStoreFiles < UInt32.MaxValue)
                    {
                        foreach (FileInfo fi in fileInfos.ToArray())
                        {
                            if (fi.LastWriteTime < DateTime.Now.AddDays(-Options.DaysCountToStoreFiles))
                            {
                                try
                                {
                                    fi.Delete();
                                    fileInfos.Remove(fi);
                                }
                                catch
                                {
                                }
                            }
                        }
                    }

                    if (Options.LogFilesMaxSizeInBytes > 0 && Options.LogFilesMaxSizeInBytes < Int64.MaxValue)
                    {
                        long bytesTotal = 0;
                        foreach (FileInfo fi in fileInfos)
                        {
                            bytesTotal += fi.Length;
                            if (bytesTotal > Options.LogFilesMaxSizeInBytes)
                            {
                                try
                                {
                                    fi.Delete();
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }

                #endregion
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(bool value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(char value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        public override void Write(char[]? buffer)
        {
            lock (_syncRoot)
            {
                _buffer.Append(buffer);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public override void Write(char[] buffer, int index, int count)
        {
            lock (_syncRoot)
            {
                _buffer.Append(buffer, index, count);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(decimal value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(double value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(float value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(int value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(long value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(object? value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        public override void Write(string format, object? arg0)
        {
            lock (_syncRoot)
            {
                _buffer.AppendFormat(FormatProvider, format, arg0);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        public override void Write(string format, object? arg0, object? arg1)
        {
            lock (_syncRoot)
            {
                _buffer.AppendFormat(FormatProvider, format, arg0, arg1);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public override void Write(string format, object? arg0, object? arg1, object? arg2)
        {
            lock (_syncRoot)
            {
                _buffer.AppendFormat(FormatProvider, format, arg0, arg1, arg2);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public override void Write(string format, params object?[] arg)
        {
            lock (_syncRoot)
            {
                _buffer.AppendFormat(FormatProvider, format, arg);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string? value)
        {
            lock (_syncRoot)
            {
                _buffer.Append(value);
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        public override void WriteLine()
        {
            lock (_syncRoot)
            {
                _buffer.AppendLine();
            }                
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(bool value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(char value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        public override void WriteLine(char[]? buffer)
        {
            Write(buffer);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public override void WriteLine(char[] buffer, int index, int count)
        {
            Write(buffer, index, count);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(decimal value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(double value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(float value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(int value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(long value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(object? value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        public override void WriteLine(string format, object? arg0)
        {
            Write(format, arg0);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        public override void WriteLine(string format, object? arg0, object? arg1)
        {
            Write(format, arg0, arg1);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
        {
            Write(format, arg0, arg1, arg2);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public override void WriteLine(string format, params object?[] arg)
        {
            Write(format, arg);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(string? value)
        {
            Write(value);
            WriteLine();
        }

        /// <summary>
        /// 
        /// </summary>
        public override Encoding Encoding
        {
            get { throw new NotImplementedException(); }
        }

        #endregion        

        #region private functions

        private void SetLogFileFullName(string suggestedLogFileName)
        {
            _suggestedLogFileNameWithoutExtension = Path.GetFileNameWithoutExtension(suggestedLogFileName);
            _suggestedLogFileNameExtension = Path.GetExtension(suggestedLogFileName);
            int n = 0;
            FileInfo fi;
            while (true)
            {
                n += 1;
                fi = new(Path.Combine(_logsDirectoryFullName, _suggestedLogFileNameWithoutExtension + @"." + n + _suggestedLogFileNameExtension));
                if (!fi.Exists)
                    break;
            }

            LogFileFullName = fi.FullName;
        }        

        #endregion        

        #region private fields

        private volatile bool _disposed;

        private readonly object _syncRoot = new();

        private readonly string _logsDirectoryFullName;

        private string _suggestedLogFileNameWithoutExtension = null!;

        private string _suggestedLogFileNameExtension = null!;

        private readonly StringBuilder _buffer = new();

        private DateTime _oldFilesClearingDateTimeUtc = DateTime.MinValue;

        private Timer _timer;

        #endregion
    }
}