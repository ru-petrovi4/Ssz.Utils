﻿using Microsoft.Extensions.Logging;
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

        /// <summary>
        ///     Default is unlimited.
        /// </summary>
        public uint DaysCountToStoreFiles { get; set; } = UInt32.MaxValue;

        /// <summary>
        ///     If size exeeds this limit, new file is created.        
        ///     Default is 10 MB.
        /// </summary>
        public long LogFileMaxSizeInBytes { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        ///     Default is 150 MB
        /// </summary>
        public long LogFilesWarningSizeInBytes { get; set; } = 150 * 1024 * 1024;

        /// <summary>
        ///     Default is 200 MB
        /// </summary>
        public long LogFilesMaxSizeInBytes { get; set; } = 200 * 1024 * 1024;

        #endregion        
    }
}
