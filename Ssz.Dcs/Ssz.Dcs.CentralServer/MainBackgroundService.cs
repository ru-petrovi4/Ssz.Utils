using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer
{
    public class MainBackgroundService : BackgroundService
    {
        #region construction and destruction

        public MainBackgroundService(ILogger<MainBackgroundService> logger, IConfiguration configuration, ServerWorkerBase serverWorker)
        {
            Logger = logger;
            Configuration = configuration;
            _serverWorker = serverWorker;
        }

        #endregion

        #region public functions

        public ILogger<MainBackgroundService> Logger { get; }

        public IConfiguration Configuration { get; }

        #endregion

        #region protected functions

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("ExecuteAsync begin.");

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) 
                    break;
                await Task.Delay(3);
                if (cancellationToken.IsCancellationRequested) 
                    break;

                DateTime nowUtc = DateTime.UtcNow;

                try
                {
                    await _serverWorker.DoWorkAsync(nowUtc, cancellationToken);
                }
                catch (Exception ex) 
                {
                    Logger.LogError(ex, @"_serverWorker.DoWorkAsync(...) Exception");
                }
            }

            await _serverWorker.ShutdownAsync();
        }

        #endregion

        #region private fields

        private readonly ServerWorkerBase _serverWorker;

        #endregion
    }
}
