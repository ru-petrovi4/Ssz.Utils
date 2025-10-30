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

            // Creates all directories and subdirectories in the specified path unless they already exist.
            Directory.CreateDirectory(@"FilesStore");
            FilesStoreDirectoryInfo = new DirectoryInfo(@"FilesStore");            

            MainUtilityDataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(ServiceProvider);

            AdditionalUtilityDataAccessProviderHolders.CollectionChanged += AdditionalUtilityDataAccessProviderHolders_OnCollectionChanged;
        }        

        #endregion

        #region public functions

        public ILogger<Worker> Logger { get; }

        public IConfiguration Configuration { get; }

        public IHostEnvironment Environment { get; }

        public IServiceProvider ServiceProvider { get; }

        public GrpcDataAccessProvider MainUtilityDataAccessProvider { get; }

        public ObservableCollection<DataAccessProviderHolder> AdditionalUtilityDataAccessProviderHolders { get; } = new();

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
                new CaseInsensitiveOrderedDictionary<string?>()
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
            
            foreach (var additionalUtilityDataAccessProviderHolderh in AdditionalUtilityDataAccessProviderHolders)
            {
                await additionalUtilityDataAccessProviderHolderh.DataAccessProvider.CloseAsync();
            }
        }

        #endregion

        #region private functions

        private void CentralServersValueSubscription_OnUpdated(object? sender, ValueStatusTimestampUpdatedEventArgs e)
        {
            List<DataAccessProviderHolder> newAdditionalUtilityDataAccessProviderHolders = new();

            foreach (var it in CsvHelper.ParseCsvLine(@",", e.NewValueStatusTimestamp.Value.ValueAsString(false)))
            {
                string? centralServerAddress = it;
                if (String.IsNullOrEmpty(centralServerAddress))
                    continue;
                
                if (centralServerAddress == @"*")
                    centralServerAddress = MainUtilityDataAccessProvider.ServerAddress;

                DataAccessProviderHolder newAdditionalUtilityDataAccessProviderHolder = new()
                {
                    CentralServerAddress = centralServerAddress,
                };
                newAdditionalUtilityDataAccessProviderHolders.Add(newAdditionalUtilityDataAccessProviderHolder);
            }            

            AdditionalUtilityDataAccessProviderHolders.Update(newAdditionalUtilityDataAccessProviderHolders.OrderBy(a => ((IObservableCollectionItem)a).ObservableCollectionItemId).ToArray(), CancellationToken.None);
        }

        private void AdditionalUtilityDataAccessProviderHolders_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:                    
                    AdditionalUtilityDataAccessProviderHolders_Added(e.NewItems!.OfType<DataAccessProviderHolder>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    AdditionalUtilityDataAccessProviderHolders_Removed(e.OldItems!.OfType<DataAccessProviderHolder>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void AdditionalUtilityDataAccessProviderHolders_Added(IEnumerable<DataAccessProviderHolder> addedAdditionalUtilityDataAccessProviderHolders)
        {
            foreach (var addedAdditionalUtilityDataAccessProviderHolder in addedAdditionalUtilityDataAccessProviderHolders)
            {
                var additionalUtilityDataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(ServiceProvider);

                additionalUtilityDataAccessProvider.EventMessagesCallback += AdditionalUtilityDataAccessProvider_OnEventMessagesCallback;                

                additionalUtilityDataAccessProvider.Initialize(null,
                    addedAdditionalUtilityDataAccessProviderHolder.CentralServerAddress,
                    DataAccessConstants.CentralServer_ClientWindowsService_ClientApplicationName,
                    System.Environment.MachineName,
                    @"",
                    new CaseInsensitiveOrderedDictionary<string?>()
                    {
                        { DataAccessConstants.ParamName_Engine_ProcessModelNames, ConfigurationHelper.GetValue<string>(Configuration, DataAccessConstants.ParamName_Engine_ProcessModelNames, @"") },
                        { DataAccessConstants.ParamName_RunningControlEnginesCount, new Any(_runningControlEngineServerAddresses.Count).ValueAsString(false) }
                    },
                    new DataAccessProviderOptions(),
                    _threadSafeDispatcher);                

                addedAdditionalUtilityDataAccessProviderHolder.DataAccessProvider = additionalUtilityDataAccessProvider;
            }
        }

        private void AdditionalUtilityDataAccessProviderHolders_Removed(IEnumerable<DataAccessProviderHolder> removedAdditionalUtilityDataAccessProviderHolders)
        {
            foreach (var removedAdditionalUtilityDataAccessProviderHolder in removedAdditionalUtilityDataAccessProviderHolders)
            {
                var additionalUtilityDataAccessProvider = removedAdditionalUtilityDataAccessProviderHolder.DataAccessProvider;

                additionalUtilityDataAccessProvider.EventMessagesCallback -= AdditionalUtilityDataAccessProvider_OnEventMessagesCallback;

                var t = additionalUtilityDataAccessProvider.CloseAsync();
            }
        }

        private async Task UtilityDataAccessProviderHolders_UpdateContextParams()
        {            
            foreach (var additionalUtilityDataAccessProviderHolder in AdditionalUtilityDataAccessProviderHolders)
            {
                await additionalUtilityDataAccessProviderHolder.DataAccessProvider.UpdateContextParamsAsync(new CaseInsensitiveOrderedDictionary<string?>()
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
