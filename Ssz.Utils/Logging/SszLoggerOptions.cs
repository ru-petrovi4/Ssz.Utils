using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Logging
{
    public class SszLoggerOptions
    {
        #region public functions
        
        public LogLevel LogLevel { get; set; } = LogLevel.Trace;

        /// <summary>
        ///     If empty (default) logs to current directory.
        /// </summary>
        public string LogDirectory { get; set; } = @"";

        /// <summary>
        ///     If empty (default) .exe and process Id is used.
        /// </summary>
        public string LogFileName { get; set; } = @"";

        public bool DuplicateInConsole { get; set; } = false;

        public uint DaysCountToStoreFiles { get; set; } = 3;

        /// <summary>
        ///     Log lines are appended to file. If size exeeds this limit, file is deleted.
        ///     If 0, file is always recreated for new logger instance.
        ///     Default is 50 MB.
        /// </summary>
        public uint LogFileMaxSizeInBytes { get; set; } = 50 * 1024 * 1024;

        #endregion        
    }
}
