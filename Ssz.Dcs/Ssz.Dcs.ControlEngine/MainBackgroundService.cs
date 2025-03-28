using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.ControlEngine;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public partial class MainBackgroundService : BackgroundService
    {
        #region construction and destruction

        public MainBackgroundService(
            ILogger<MainBackgroundService> logger, 
            IConfiguration configuration, 
            IServiceProvider serviceProvider, 
            DataAccessServerWorkerBase serverWorker,
            IHostLifetime hostLifetime)
        {
            Logger = logger;
            Configuration = configuration;
            ServiceProvider = serviceProvider;
            _serverWorker = serverWorker;
            _hostLifetime = hostLifetime;

            _serverWorker.ShutdownRequested += ShutdownRequested;

            _utilityDataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(serviceProvider);
            _processDataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(ServiceProvider);
        }        

        #endregion

        #region public functions

        public ILogger<MainBackgroundService> Logger { get; }

        public IConfiguration Configuration { get; }

        public IServiceProvider ServiceProvider { get; }        

        #endregion

        #region protected functions

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("ExecuteAsync begin.");

            string controlEngineServerAddress = ConfigurationHelper.GetValue<string>(Configuration, @"Kestrel:Endpoints:HttpsDefaultCert:Url", @"");
            controlEngineServerAddress = controlEngineServerAddress.Replace(@"*", System.Environment.MachineName);

            //_utilityDataAccessProvider.EventMessagesCallback += UtilityDataAccessProviderOnEventMessagesCallback;
            _utilityDataAccessProvider.Initialize(null,                
                Program.Options.GetCentralServerAddress(),
                DataAccessConstants.ControlEngine_ClientApplicationName,
                Environment.MachineName,
                @"", // Utility context
                new CaseInsensitiveDictionary<string?>
                {
                    { DataAccessConstants.ParamName_EngineSessionId, Program.Options.EngineSessionId },
                    { DataAccessConstants.ParamName_ControlEngineServerAddress, controlEngineServerAddress }
                },
                new DataAccessProviderOptions(),
                _serverWorker.ThreadSafeDispatcher);            

            DsDevice device;
            if (Program.Options.ProcessDirectory != @"")
            {
                device = ActivatorUtilities.CreateInstance<DsDevice>(ServiceProvider,
                    _processDataAccessProvider,
                    new DirectoryInfo(Program.Options.ProcessDirectory),
                    _serverWorker.ThreadSafeDispatcher);
            }
            else
            {
                device = ActivatorUtilities.CreateInstance<DsDevice>(ServiceProvider,
                    _processDataAccessProvider,                    
                    _serverWorker.ThreadSafeDispatcher);
            }

            //new DsController(
            //    ActivatorUtilities.CreateInstance<ILogger<DsController>>(ServiceProvider),
            //    );
            ((ServerWorker)_serverWorker).Device = device;

            _processDataAccessProvider.Initialize(device.ElementIdsMap,                
                Program.Options.GetCentralServerAddress(),
                DataAccessConstants.ControlEngine_ClientApplicationName,
                Environment.MachineName,
                Program.Options.CentralServerSystemName,
                new CaseInsensitiveDictionary<string?>(),
                new DataAccessProviderOptions(),
                _serverWorker.ThreadSafeDispatcher);

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(3, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    DateTime nowUtc = DateTime.UtcNow;

                    if (nowUtc - _processDataAccessProvider.InitializedDateTimeUtc > DataAccessConstants.UnrecoverableTimeout &&
                            nowUtc - _processDataAccessProvider.LastSuccessfulConnectionDateTimeUtc > DataAccessConstants.UnrecoverableTimeout)
                        break;

                    await _serverWorker.DoWorkAsync(nowUtc, cancellationToken);
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogDebug(@"_serverWorker.DoWorkAsync(...) cancellationToken.IsCancellationRequested");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, @"_serverWorker.DoWorkAsync(...) Exception");                    
                }
            }

            await _processDataAccessProvider.CloseAsync();

            ((ServerWorker)_serverWorker).Device = null;
            device.Dispose();            

            await _serverWorker.CloseAsync();
            
            await _utilityDataAccessProvider.DisposeAsync();
        }

        #endregion

        #region private functions

        private async void ShutdownRequested(object? sender, EventArgs args)
        {
            //await _hostLifetime.StopAsync(CancellationToken.None);
            await Program.SafeShutdownAsync();
        }

        #endregion        

        #region private fields

        private readonly DataAccessServerWorkerBase _serverWorker;

        private readonly IHostLifetime _hostLifetime;

        private readonly GrpcDataAccessProvider _utilityDataAccessProvider;

        /// <summary>
        ///    No one can use this provider, except DsController, because DsController can call GrpcDataAccessProvider.ReInitialize(),
        ///    and all items are lost.
        ///    DsController can add items only in DoWork function, when GrpcDataAccessProvider is Initialized.
        /// </summary>
        private readonly GrpcDataAccessProvider _processDataAccessProvider;

        #endregion
    }
}
