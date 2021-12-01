﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;

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
        public LogFileTextWriter(SszLoggerOptions options)
            : base(null)
        {
            _options = options;

            _logsDirectoryFullName = Environment.ExpandEnvironmentVariables(_options.LogDirectory);

            //     Creates all directories and subdirectories in the specified path unless they
            //     already exist.
            if (String.IsNullOrEmpty(_logsDirectoryFullName))
            {
                _logsDirectoryFullName = Directory.GetCurrentDirectory();
            }
            else
            {
                Directory.CreateDirectory(_logsDirectoryFullName);
            }

            if (!String.IsNullOrEmpty(_options.LogFileName))
            {
                LogFileFullName = Path.Combine(_logsDirectoryFullName, _options.LogFileName);                
            }
            else
            {
                Process currentProcess = Process.GetCurrentProcess();
                string? moduleName = currentProcess.MainModule?.ModuleName;
                if (moduleName is null) throw new InvalidOperationException();
                var exeFileName = new FileInfo(moduleName).Name;

                LogFileFullName = Path.Combine(_logsDirectoryFullName, exeFileName + @"." + currentProcess.Id + @".log");

                #region DeleteOldFiles

                string[] files = Directory.GetFiles(_logsDirectoryFullName, exeFileName + @".*.log");

                foreach (string file in files)
                {
                    try
                    {
                        var f = new FileInfo(file);
                        if (f.LastAccessTime < DateTime.Now.AddDays(-_options.DaysCountToStoreFiles))
                            f.Delete();
                    }
                    catch
                    {
                    }
                }

                #endregion
            }

            if (_options.LogFileMaxSizeInBytes == 0)
            {
                var fi = new FileInfo(LogFileFullName);
                if (fi.Exists)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        public string LogFileFullName { get; }

        /// <summary>
        /// 
        /// </summary>
        public override void Flush()
        {
            if (_buffer.Length > 0)
            {
                StreamWriter? sw = null;
                try
                {   
                    if (_options.LogFileMaxSizeInBytes > 0)
                    {
                        var fi = new FileInfo(LogFileFullName);
                        if (fi.Exists && fi.Length > _options.LogFileMaxSizeInBytes)
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
                    sw = new StreamWriter(LogFileFullName, true, new UTF8Encoding(true));
                    sw.Write(_buffer.ToString());
                    _buffer.Clear();                    
                }
                catch
                {
                }
                finally
                {
                    if (sw is not null)
                    {
                        sw.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(bool value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(char value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        public override void Write(char[]? buffer)
        {
            _buffer.Append(buffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public override void Write(char[] buffer, int index, int count)
        {
            _buffer.Append(buffer, index, count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(decimal value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(double value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(float value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(int value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(long value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(object? value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        public override void Write(string format, object? arg0)
        {
            _buffer.AppendFormat(FormatProvider, format, arg0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        public override void Write(string format, object? arg0, object? arg1)
        {
            _buffer.AppendFormat(FormatProvider, format, arg0, arg1);
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
            _buffer.AppendFormat(FormatProvider, format, arg0, arg1, arg2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public override void Write(string format, params object?[] arg)
        {
            _buffer.AppendFormat(FormatProvider, format, arg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string? value)
        {
            _buffer.Append(value);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void WriteLine()
        {
            _buffer.AppendLine();
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

        #region private fields

        private readonly SszLoggerOptions _options;

        private readonly string _logsDirectoryFullName;           

        private readonly StringBuilder _buffer = new();

        #endregion
    }
}