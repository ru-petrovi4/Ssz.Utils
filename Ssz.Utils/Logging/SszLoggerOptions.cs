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
        public string LogsDirectory { get; set; } = @"";

        /// <summary>
        ///     If empty (default) .exe and process Id is used.
        /// </summary>
        public string LogFileName { get; set; } = @"";

        public bool DuplicateInConsole { get; set; } = false;

        public uint DaysCountToStoreFiles { get; set; } = 3;

        /// <summary>
        ///     If size exeeds this limit, new file is created.        
        ///     Default is 50 MB.
        /// </summary>
        public uint LogFileMaxSizeInBytes { get; set; } = 50 * 1024 * 1024;

        public uint LogFilesWarningSizeInBytes { get; set; } = 800 * 1024 * 1024;

        public uint LogFilesMaxSizeInBytes { get; set; } = 1024 * 1024 * 1024;

        #endregion        
    }
}
