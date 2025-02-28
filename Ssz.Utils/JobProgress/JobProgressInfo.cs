using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ssz.Utils
{
    public class JobProgressInfo
    {
        public JobProgressInfo(IJobProgress jobProgress, int progressMaxValue)
        {
            JobProgress = jobProgress;
            ProgressMaxValue = progressMaxValue;
            Stopwatch = Stopwatch.StartNew();
        }

        public IJobProgress JobProgress { get; }

        /// <summary>
        ///    
        /// </summary>
        public int ProgressMaxValue { get; }

        /// <summary>
        ///     
        /// </summary>
        public int ProgressCurrentValue { get; set; }

        public Stopwatch Stopwatch { get; }

        public uint GetProgressPercent()
        {
            if (ProgressMaxValue == 0)
                return 0;
            return (uint)(100.0 * ProgressCurrentValue / ProgressMaxValue);
        }
    }
}
