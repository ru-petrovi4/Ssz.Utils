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

        public MainBackgroundService(ILogger<MainBackgroundService> logger, ServerWorkerBase serverWorker)
        {
            Logger = logger;
            _serverWorker = serverWorker;
        }

        #endregion

        #region public functions

        public ILogger<MainBackgroundService> Logger { get; }

        #endregion

        #region protected functions

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("ExecuteAsync begin.");

            await _serverWorker.InitializeAsync(cancellationToken);

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(20, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    await _serverWorker.DoWorkAsync(DateTime.UtcNow, cancellationToken);
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, @"_serverWorker.DoWorkAsync(...) Exception");
                    break;
                }
            }

            await _serverWorker.CloseAsync();
        }

        #endregion

        #region private fields

        private readonly ServerWorkerBase _serverWorker;

        #endregion        
    }
}
