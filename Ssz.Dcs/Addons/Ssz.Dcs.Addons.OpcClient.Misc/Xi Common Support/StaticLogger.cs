using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xi.Common.Support
{
    public static class StaticLogger
    {
        public static void Initialize(ILogger logger)
        {
            Logger = logger;
        }

        public static ILogger Logger { get; private set; } = null!; 
    }    
}
