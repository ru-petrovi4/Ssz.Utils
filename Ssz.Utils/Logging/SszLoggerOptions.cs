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

        public int EventId { get; set; } = 0;

        public LogLevel LogLevel { get; set; } = LogLevel.Trace;

        public string LogsDirectory { get; set; } = @"%ProgramData%\Ssz\Logs";

        public bool DuplicateInConsole { get; set; } = false;

        public uint DaysCountToStoreFiles { get; set; } = 3;

        public uint LogFileMaxSizeInBytes { get; internal set; } = 50 * 1024 * 1024;

        #endregion        
    }
}
