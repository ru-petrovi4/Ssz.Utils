using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public static class OperatorSessionConstants
    {
        public const int ReadyToLaunchOperator = 0;

        public const int LaunchingOperator = 1;

        public const int LaunchedOperator = 2;

        public const int ShutdowningOperator = 3;

        public const int ShutdownedOperator = 4;
    }
}
