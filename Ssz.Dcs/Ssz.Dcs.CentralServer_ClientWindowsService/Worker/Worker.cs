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

            UtilityDataAccessProviderHolders.CollectionChanged += DataAccessProviderHolders_OnCollectionChanged;
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
                    { DataAccessConstants.ParamName_HostType, ConfigurationHelper.GetValue<string>(Configuration, @"HostType", @"") }
                },
                new DataAccessProviderOptions(),
                _threadSafeDispatcher);

            _centralServersValueSubscription = new ValueSubscription(MainUtilityDataAccessProvider, DataAccessConstants.CentralServers_UtilityItem, CentralServersValueSubscription_OnUpdated);

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

            foreach (var centralServerAddress in CsvHelper.ParseCsvLine(@",", e.NewValueStatusTimestamp.Value.ValueAsString(false)))
            {
                if (String.IsNullOrEmpty(centralServerAddress))
                    continue;

                DataAccessProviderHolder newDataAccessProviderHolder = new()
                {
                    CentralServerAddress = centralServerAddress,
                };
                newUtilityDataAccessProviderHolders.Add(newDataAccessProviderHolder);
            }            

            UtilityDataAccessProviderHolders.Update(newUtilityDataAccessProviderHolders.OrderBy(a => ((IObservableCollectionItem)a).ObservableCollectionItemId).ToArray(), CancellationToken.None);
        }

        private void DataAccessProviderHolders_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:                    
                    DataAccessProviderHolders_Added(e.NewItems!.OfType<DataAccessProviderHolder>());
                    break;
                case NotifyCollectionChangedAction.Remove:                    
                    DataAccessProviderHolders_Removed(e.OldItems!.OfType<DataAccessProviderHolder>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void DataAccessProviderHolders_Added(IEnumerable<DataAccessProviderHolder> addedDataAccessProviderHolders)
        {
            foreach (var addedDataAccessProviderHolder in addedDataAccessProviderHolders)
            {
                var utilityDataAccessProvider = ActivatorUtilities.CreateInstance<GrpcDataAccessProvider>(ServiceProvider);

                utilityDataAccessProvider.EventMessagesCallback += UtilityDataAccessProvider_OnEventMessagesCallback;

                var centralServerAddress = addedDataAccessProviderHolder.CentralServerAddress;
                if (centralServerAddress == @"*")
                    centralServerAddress = MainUtilityDataAccessProvider.ServerAddress;

                utilityDataAccessProvider.Initialize(null,
                    centralServerAddress,
                    DataAccessConstants.CentralServer_ClientWindowsService_ClientApplicationName,
                    System.Environment.MachineName,
                    @"",
                    new CaseInsensitiveDictionary<string?>(),
                    new DataAccessProviderOptions(),
                    _threadSafeDispatcher);                

                addedDataAccessProviderHolder.DataAccessProvider = utilityDataAccessProvider;
            }
        }

        private void DataAccessProviderHolders_Removed(IEnumerable<DataAccessProviderHolder> removedDataAccessProviderHolders)
        {
            foreach (var removedDataAccessProviderHolder in removedDataAccessProviderHolders)
            {
                var utilityDataAccessProvider = removedDataAccessProviderHolder.DataAccessProvider;

                utilityDataAccessProvider.EventMessagesCallback -= UtilityDataAccessProvider_OnEventMessagesCallback;

                var t = utilityDataAccessProvider.CloseAsync();
            }
        }

        #endregion

        #region private fields

        private readonly ThreadSafeDispatcher _threadSafeDispatcher = new();

        private ValueSubscription? _centralServersValueSubscription;

        #endregion
    }
}
