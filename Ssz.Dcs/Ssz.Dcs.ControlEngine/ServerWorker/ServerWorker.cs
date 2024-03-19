using System;
using System.Collections.Generic;
using System.Threading;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Ssz.Dcs.ControlEngine.ServerListItems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;

namespace Ssz.Dcs.ControlEngine
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region construction and destruction

        public ServerWorker(
            IResourceMonitor resourceMonitor,
            ILogger<ServerWorker> logger, 
            IConfiguration configuration, 
            IServiceProvider serviceProvider) :
            base(resourceMonitor, logger)
        {            
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region public functions  

        public DsDevice? Device { get; set; }

        public override async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) 
                return;

            if (Device is not null)
                Device.DoWork(nowUtc, cancellationToken);

            DoWorkUtilityItems(nowUtc, cancellationToken);

            await base.DoWorkAsync(nowUtc, cancellationToken);
        }

        #endregion        

        #region private fields

        private readonly IConfiguration _configuration;

        private readonly IServiceProvider _serviceProvider;

        #endregion
    }
}