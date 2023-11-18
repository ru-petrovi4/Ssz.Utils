using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.Dcs.CentralServer.Common;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;
using Ssz.Utils.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ssz.Dcs.CentralServer.Common.Helpers;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public partial class MainBackgroundService : BackgroundService
    {
        #region construction and destruction

        public MainBackgroundService(ILogger<MainBackgroundService> logger, IConfiguration configuration, IHostEnvironment environment, IServiceProvider serviceProvider)
        {
            Logger = logger;
            Configuration = configuration;
            Environment = environment;

            FilesStoreDirectoryInfo = ProgramDataDirectoryHelper.GetFilesStoreDirectoryInfo(Configuration);

            UtilityDataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(serviceProvider);
            UtilityDataAccessProvider.EventMessagesCallback += OnUtilityDataAccessProvider_EventMessagesCallback;
            //UtilityDataAccessProvider.PropertyChanged += UtilityDataAccessProviderOnPropertyChanged;            
        }

        public override void Dispose()
        {
            UtilityDataAccessProvider.EventMessagesCallback -= OnUtilityDataAccessProvider_EventMessagesCallback;
            //UtilityDataAccessProvider.PropertyChanged -= UtilityDataAccessProviderOnPropertyChanged;

            base.Dispose();
        }

        #endregion

        #region public functions

        public ILogger<MainBackgroundService> Logger { get; }

        public IConfiguration Configuration { get; }

        public IHostEnvironment Environment { get; }

        public GrpcDataAccessProvider UtilityDataAccessProvider { get; }        

        public DirectoryInfo FilesStoreDirectoryInfo { get; private set; }        

        #endregion

        #region protected functions

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug("ExecuteAsync begin.");

            string centralServerAddress = ConfigurationHelper.GetValue<string>(Configuration, @"CentralServerAddress", @"");
            if (centralServerAddress == @"")
            {
                Logger.LogCritical("CentralServerAddress is not specified in config.");
                return;
            }
            UtilityDataAccessProvider.Initialize(null,                 
                centralServerAddress, 
                DataAccessConstants.CentralServer_ClientWindowsService_ClientApplicationName, 
                System.Environment.MachineName, 
                @"", 
                new CaseInsensitiveDictionary<string?>(),
                new DataAccessProviderOptions(),
                _threadSafeDispatcher);

            while (true)
            {
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(10);
                if (cancellationToken.IsCancellationRequested) break;

                await _threadSafeDispatcher.InvokeActionsInQueueAsync(cancellationToken);
            }

            UtilityDataAccessProvider.Close();
        }

        #endregion

        #region private fields

        private readonly ThreadSafeDispatcher _threadSafeDispatcher = new();

        #endregion
    }
}
