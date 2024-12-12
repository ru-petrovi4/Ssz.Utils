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

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public class MainBackgroundService : BackgroundService
    {
        #region construction and destruction

        public MainBackgroundService(ILogger<MainBackgroundService> logger, Worker worker)
        {
            Logger = logger;            
            _worker = worker;
        }

        #endregion

        #region public functions

        public ILogger<MainBackgroundService> Logger { get; }

        #endregion

        #region protected functions

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("ExecuteAsync begin.");

            await _worker.InitializeAsync(cancellationToken);

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(3, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    await _worker.DoWorkAsync(DateTime.UtcNow, cancellationToken);
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, @"_worker.DoWorkAsync(...) Exception");                    
                }                
            }

            await _worker.CloseAsync();
        }

        #endregion

        #region private fields

        private readonly Worker _worker;

        #endregion
    }
}
