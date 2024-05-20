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
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public partial class Worker
    {
        #region construction and destruction

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IHostEnvironment environment, IServiceProvider serviceProvider)
        {
            Logger = logger;
            Configuration = configuration;
            Environment = environment;
            ServiceProvider = serviceProvider;

            FilesStoreDirectoryInfo = ProgramDataDirectoryHelper.GetFilesStoreDirectoryInfo(Configuration);

            MainUtilityDataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(ServiceProvider);

            UtilityDataAccessProviderHolders.CollectionChanged += UtilityDataAccessProviderHolders_OnCollectionChanged;
        }        

        #endregion

        #region public functions

        public ILogger<Worker> Logger { get; }

        public IConfiguration Configuration { get; }

        public IHostEnvironment Environment { get; }

        public IServiceProvider ServiceProvider { get; }

        public GrpcDataAccessProvider MainUtilityDataAccessProvider { get; }

        public ObservableCollection<DataAccessProviderHolder> UtilityDataAccessProviderHolders { get; } = new();

        public DirectoryInfo FilesStoreDirectoryInfo { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {   
            string mainCentralServerAddress = ConfigurationHelper.GetValue<string>(Configuration, @"MainCentralServerAddress", @"");
            if (mainCentralServerAddress == @"")
            {
                Logger.LogCritical("MainCentralServerAddress is not specified in config.");
                return Task.CompletedTask;
            }            

            MainUtilityDataAccessProvider.Initialize(null,
                mainCentralServerAddress,
                DataAccessConstants.CentralServer_ClientWindowsService_ClientApplicationName,
                System.Environment.MachineName,
                @"",
                new CaseInsensitiveDictionary<string?>()
                {
                    { DataAccessConstants.ParamName_ConnectionToMain, @"true" }
                },
                new DataAccessProviderOptions(),
                _threadSafeDispatcher);

            if (ConfigurationHelper.GetValue<bool>(Configuration, DataAccessConstants.ParamName_AllCentralServers, false))            
                _centralServersValueSubscription = new ValueSubscription(MainUtilityDataAccessProvider, DataAccessConstants.UtilityItem_CentralServers, CentralServersValueSubscription_OnUpdated);
            else
                _centralServersValueSubscription = new ValueSubscription(MainUtilityDataAccessProvider, DataAccessConstants.UtilityItem_CentralServer, CentralServersValueSubscription_OnUpdated);

            return Task.CompletedTask;
        }        

        public async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            await _threadSafeDispatcher.InvokeActionsInQueueAsync(cancellationToken);
        }

        public async Task CloseAsync()
        {
            _centralServersValueSubscription?.Dispose();

            await MainUtilityDataAccessProvider.CloseAsync();   
            
            foreach (var h in UtilityDataAccessProviderHolders)
            {
                await h.DataAccessProvider.CloseAsync();
            }
        }

        #endregion

        #region private functions

        private void CentralServersValueSubscription_OnUpdated(object? sender, ValueStatusTimestampUpdatedEventArgs e)
        {
            List<DataAccessProviderHolder> newUtilityDataAccessProviderHolders = new();

            foreach (var it in CsvHelper.ParseCsvLine(@",", e.NewValueStatusTimestamp.Value.ValueAsString(false)))
            {
                string? centralServerAddress = it;
                if (String.IsNullOrEmpty(centralServerAddress))
                    continue;
                
                if (centralServerAddress == @"*")
                    centralServerAddress = MainUtilityDataAccessProvider.ServerAddress;

                DataAccessProviderHolder newDataAccessProviderHolder = new()
                {
                    CentralServerAddress = centralServerAddress,
                };
                newUtilityDataAccessProviderHolders.Add(newDataAccessProviderHolder);
            }            

            UtilityDataAccessProviderHolders.Update(newUtilityDataAccessProviderHolders.OrderBy(a => ((IObservableCollectionItem)a).ObservableCollectionItemId).ToArray(), CancellationToken.None);
        }

        private void UtilityDataAccessProviderHolders_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:                    
                    UtilityDataAccessProviderHolders_Added(e.NewItems!.OfType<DataAccessProviderHolder>());
                    break;
                case NotifyCollectionChangedAction.Remove:                    
                    UtilityDataAccessProviderHolders_Removed(e.OldItems!.OfType<DataAccessProviderHolder>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void UtilityDataAccessProviderHolders_Added(IEnumerable<DataAccessProviderHolder> addedUtilityDataAccessProviderHolders)
        {
            foreach (var addedUtilityDataAccessProviderHolder in addedUtilityDataAccessProviderHolders)
            {
                var utilityDataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(ServiceProvider);

                utilityDataAccessProvider.EventMessagesCallback += UtilityDataAccessProvider_OnEventMessagesCallback;                

                utilityDataAccessProvider.Initialize(null,
                    addedUtilityDataAccessProviderHolder.CentralServerAddress,
                    DataAccessConstants.CentralServer_ClientWindowsService_ClientApplicationName,
                    System.Environment.MachineName,
                    @"",
                    new CaseInsensitiveDictionary<string?>()
                    {
                        { DataAccessConstants.ParamName_Engine_ProcessModelNames, ConfigurationHelper.GetValue<string>(Configuration, DataAccessConstants.ParamName_Engine_ProcessModelNames, @"") },
                        { DataAccessConstants.ParamName_RunningControlEnginesCount, new Any(_runningControlEngineServerAddresses.Count).ValueAsString(false) }
                    },
                    new DataAccessProviderOptions(),
                    _threadSafeDispatcher);                

                addedUtilityDataAccessProviderHolder.DataAccessProvider = utilityDataAccessProvider;
            }
        }

        private void UtilityDataAccessProviderHolders_Removed(IEnumerable<DataAccessProviderHolder> removedUtilityDataAccessProviderHolders)
        {
            foreach (var removedUtilityDataAccessProviderHolder in removedUtilityDataAccessProviderHolders)
            {
                var utilityDataAccessProvider = removedUtilityDataAccessProviderHolder.DataAccessProvider;

                utilityDataAccessProvider.EventMessagesCallback -= UtilityDataAccessProvider_OnEventMessagesCallback;

                var t = utilityDataAccessProvider.CloseAsync();
            }
        }

        private async Task UtilityDataAccessProviderHolders_UpdateContextParams()
        {            
            foreach (var utilityDataAccessProviderHolder in UtilityDataAccessProviderHolders)
            {
                await utilityDataAccessProviderHolder.DataAccessProvider.UpdateContextParamsAsync(new CaseInsensitiveDictionary<string?>()
                    {
                        { DataAccessConstants.ParamName_Engine_ProcessModelNames, ConfigurationHelper.GetValue<string>(Configuration, DataAccessConstants.ParamName_Engine_ProcessModelNames, @"") },
                        { DataAccessConstants.ParamName_RunningControlEnginesCount, new Any(_runningControlEngineServerAddresses.Count).ValueAsString(false) }
                    });
            }
        }

        #endregion

        #region private fields

        private readonly ThreadSafeDispatcher _threadSafeDispatcher = new();

        private ValueSubscription? _centralServersValueSubscription;

        private List<string> _runningControlEngineServerAddresses = new();

        #endregion
    }
}
