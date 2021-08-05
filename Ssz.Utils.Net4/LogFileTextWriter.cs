using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Ssz.Utils
{
    public class LogFileTextWriter : TextWriter, IDisposable
    {
        #region construction and destruction

        public LogFileTextWriter(string logDirectoryName, string exeFileName)
            : base(null)
        {
            _logDirectoryName = logDirectoryName;
            _exeFileName = exeFileName;

            LogFileName = _logDirectoryName + "\\" + _exeFileName + @"." + Process.GetCurrentProcess().Id +
                                 @".log";

            _buffer = new StringBuilder();

            #region DeleteOldFiles

            string[] files = Directory.GetFiles(_logDirectoryName, _exeFileName + @".*.log");

            foreach (string file in files)
            {
                try
                {
                    var f = new FileInfo(file);
                    if (f.LastAccessTime < DateTime.Now.AddDays(-3))
                        f.Delete();
                }
                catch
                {
                }
            }

            #endregion
        }

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

        public string LogFileName { get; private set; }

        public override void Flush()
        {
            if (_buffer.Length > 0)
            {
                try
                {
                    StreamWriter sw;
                    var fi = new FileInfo(LogFileName);
                    if (!fi.Exists)
                    {
                        sw = File.CreateText(LogFileName);
                    }
                    else
                    {
                        if (fi.Length > 50*1024*1024)
                        {
                            fi.Delete();
                            sw = File.CreateText(LogFileName);
                        }
                        else
                        {
                            sw = File.AppendText(LogFileName);
                        }
                    }
                    sw.Write(_buffer.ToString());
                    _buffer.Length = 0;
                    sw.Close();
                }
                catch
                {
                }
            }
        }

        public override void Write(bool value)
        {
            _buffer.Append(value);
        }

        public override void Write(char value)
        {
            _buffer.Append(value);
        }

        public override void Write(char[] buffer)
        {
            _buffer.Append(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _buffer.Append(buffer, index, count);
        }

        public override void Write(decimal value)
        {
            _buffer.Append(value);
        }

        public override void Write(double value)
        {
            _buffer.Append(value);
        }

        public override void Write(float value)
        {
            _buffer.Append(value);
        }

        public override void Write(int value)
        {
            _buffer.Append(value);
        }

        public override void Write(long value)
        {
            _buffer.Append(value);
        }

        public override void Write(object value)
        {
            _buffer.Append(value);
        }

        public override void Write(string format, object arg0)
        {
            _buffer.AppendFormat(FormatProvider, format, arg0);
        }

        public override void Write(string format, object arg0, object arg1)
        {
            _buffer.AppendFormat(FormatProvider, format, arg0, arg1);
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            _buffer.AppendFormat(FormatProvider, format, arg0, arg1, arg2);
        }

        public override void Write(string format, params object[] arg)
        {
            _buffer.AppendFormat(FormatProvider, format, arg);
        }

        public override void Write(string value)
        {
            _buffer.Append(value);
        }

        public override void WriteLine()
        {
            _buffer.AppendLine();
        }

        public override void WriteLine(bool value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(char value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(char[] buffer)
        {
            Write(buffer);
            WriteLine();
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            Write(buffer, index, count);
            WriteLine();
        }

        public override void WriteLine(decimal value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(double value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(float value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(int value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(long value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(object value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine(string format, object arg0)
        {
            Write(format, arg0);
            WriteLine();
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            Write(format, arg0, arg1);
            WriteLine();
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            Write(format, arg0, arg1, arg2);
            WriteLine();
        }

        public override void WriteLine(string format, params object[] arg)
        {
            Write(format, arg);
            WriteLine();
        }

        public override void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }

        public override Encoding Encoding
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        #region private fields

        private readonly string _logDirectoryName;
        private readonly string _exeFileName;

        private readonly StringBuilder _buffer;

        #endregion
    }
}