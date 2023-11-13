using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.IdentityServer
{
    public class MainBackgroundService : BackgroundService
    {
        #region construction and destruction

        public MainBackgroundService(ILogger<MainBackgroundService> logger, IConfiguration configuration)
        {
            Logger = logger;
            Configuration = configuration;            
        }

        #endregion

        #region public functions

        public ILogger<MainBackgroundService> Logger { get; }

        public IConfiguration Configuration { get; }

        #endregion

        #region protected functions

        protected override async Task ExecuteAsync(CancellationToken cancellationTokenn)
        {
            Logger.LogDebug("ExecuteAsync begin.");            

            while (true)
            {
                if (cancellationTokenn.IsCancellationRequested) break;
                await Task.Delay(3000);
                if (cancellationTokenn.IsCancellationRequested) break;

                DateTime nowUtc = DateTime.UtcNow;
            }
        }

        #endregion
    }
}
