using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public static class ProgressHelper
    {
        #region public functions

        public static uint GetPercent(double minPercent, double maxPercent, double k)
        {
            return (uint)(minPercent + (maxPercent - minPercent) * k);
        }

        public static uint GetPercent(double current, double max)
        {
            return (uint)(100.0 * current / max);
        }

        #endregion        
    }
}
