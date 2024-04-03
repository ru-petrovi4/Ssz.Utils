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
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.ServerListItems;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using Ssz.Utils.Addons;
using System.Linq;
using Ssz.Dcs.CentralServer.Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region construction and destruction

        public ServerWorker(
                IResourceMonitor resourceMonitor,
                ILogger<ServerWorker> logger, 
                IConfiguration configuration, 
                IServiceProvider serviceProvider, 
                AddonsManager addonsManager,
                IDbContextFactory<DcsCentralServerDbContext> dbContextFactory) :
            base(
                resourceMonitor, 
                logger)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _addonsManager = addonsManager;
            _dbContextFactory = dbContextFactory;

            FilesStoreDirectoryInfo = ProgramDataDirectoryHelper.GetFilesStoreDirectoryInfo(_configuration);

            ServerContextAddedOrRemoved += On_ServerContextAddedOrRemoved;

            _csvDb = ActivatorUtilities.CreateInstance<CsvDb>(
                _serviceProvider, ProgramDataDirectoryHelper.GetCsvDbDirectoryInfo(_configuration), ThreadSafeDispatcher);
            //_csvDb.CsvFileChanged += CsvDbOnCsvFileChanged;
            //CsvDbOnCsvFileChanged(CsvFileChangeAction.Added, null);
                        
            string dcsCentralServerAddonDesc = ConfigurationHelper.GetValue(_configuration, @"DcsCentralServerAddonDesc", @"");
            if (dcsCentralServerAddonDesc != @"")
                DcsCentralServerAddon.DescStatic = dcsCentralServerAddonDesc;            
        }        

        #endregion

        #region public functions  

        public DirectoryInfo FilesStoreDirectoryInfo { get; private set; }

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await base.InitializeAsync(cancellationToken);

            _addonsManager.Addons.CollectionChanged += OnAddons_CollectionChanged;
            _addonsManager.Initialize(null,
                new AddonBase[] { new DcsCentralServerAddon() },
                _csvDb,
                ThreadSafeDispatcher,
                SubstituteAddonOption,
                new AddonsManagerOptions
                {
                    AddonsSearchPattern = @"Ssz.Dcs.Addons.*.dll",
                    CanModifyAddonsCsvFiles = true
                });
        }

        public override async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) 
                return;

            foreach (IWorkDoer workDoer in _addonsManager.AddonsThreadSafe.OfType<IWorkDoer>().ToArray())
            {
                await workDoer.DoWorkAsync(nowUtc, cancellationToken);
            }

            Cleanup(nowUtc, cancellationToken);

            if (cancellationToken.IsCancellationRequested) 
                return;

            DoWorkUtilityItems(nowUtc, cancellationToken);

            await base.DoWorkAsync(nowUtc, cancellationToken);
        }

        public override async Task CloseAsync()
        {
            _addonsManager.Close();

            await base.CloseAsync();
        }

        #endregion

        #region private functions

        private string? SubstituteAddonOption(string? optionValue)
        {
            return SszQueryHelper.ComputeValueOfSszQueries(optionValue, GetConstantValue);            
        }

        private string GetConstantValue(string constant)
        {
            if (constant.StartsWith(@"%(ConfigurationCrypter:", StringComparison.InvariantCultureIgnoreCase))
            {
                return ConfigurationHelper.GetValue<string>(_configuration, constant.Substring(2, constant.Length - 3), @"");
            }
            else if (String.Equals(constant, @"%(Port)", StringComparison.InvariantCultureIgnoreCase))
            {
                var url = ConfigurationHelper.GetValue<string>(_configuration, @"Kestrel:Endpoints:HttpsDefaultCert:Url", @"");
                int i = url.LastIndexOf(':');
                if (i > 0)
                    return url.Substring(i + 1);
            }
            return constant;
        }

        #endregion

        #region private fields

        private readonly IConfiguration _configuration;

        private readonly IServiceProvider _serviceProvider;

        private readonly CsvDb _csvDb;

        private readonly AddonsManager _addonsManager;

        private readonly IDbContextFactory<DcsCentralServerDbContext> _dbContextFactory;

        /// <summary>
        ///     [ProcessModelingSessionId, ProcessModelingSession]
        /// </summary>
        private readonly CaseInsensitiveDictionary<ProcessModelingSession> _processModelingSessionsCollection = new();               

        #endregion
    }
}

//public DsFilesStoreDirectory GetRootDsFilesStoreDirectoryCached()
//{
//    if (_rootDsFilesStoreDirectory is null)
//    {
//        _rootDsFilesStoreDirectory = DsFilesStoreHelper.GetRootDsFilesStoreDirectory(_configuration);
//    }

//    return _rootDsFilesStoreDirectory;
//}

//private DsFilesStoreDirectory? _rootDsFilesStoreDirectory;

//_filesStoreFileSystemWatcher = new FileSystemWatcher();
//try
//{
//    var filesStoreDirectoryInfo = DsFilesStoreHelper.GetFilesStoreDirectoryInfo(_configuration);

//    _filesStoreFileSystemWatcher.Created += FilesStoreFileSystemWatcherOnEvent;
//    _filesStoreFileSystemWatcher.Changed += FilesStoreFileSystemWatcherOnEvent;
//    _filesStoreFileSystemWatcher.Deleted += FilesStoreFileSystemWatcherOnEvent;
//    _filesStoreFileSystemWatcher.Renamed += FilesStoreFileSystemWatcherOnEvent;
//    _filesStoreFileSystemWatcher.Path = filesStoreDirectoryInfo.FullName;
//    _filesStoreFileSystemWatcher.IncludeSubdirectories = true;
//    _filesStoreFileSystemWatcher.EnableRaisingEvents = true;
//}
//catch (Exception ex)
//{
//    Logger.LogWarning(ex, "AppSettings FilesStore directory error. Please, specify correct directory and restart service.");
//}

//private readonly FileSystemWatcher _filesStoreFileSystemWatcher;

//#region private functions

//private void FilesStoreFileSystemWatcherOnEvent(object sender, FileSystemEventArgs e)
//{
//    BeginInvoke(ct =>
//    {
//        _rootDsFilesStoreDirectory = null;
//        _utilityItemsDoWorkNeeded = true;
//    });
//}

//#endregion

///// <summary>
/////     WindowsService main thread
///// </summary>
//public void Initialize(Dispatcher dispatcher)
//{
//    _dispatcher = dispatcher;

//    UseCtcmLmsDb = false;
//    var ctcmLmsConnection = System.Configuration.ConfigurationManager.ConnectionStrings["CtcmLmsConnection"];
//    if (ctcmLmsConnection is not null && !String.IsNullOrWhiteSpace(ctcmLmsConnection.ConnectionString))
//    {                
//        try
//        {
//            CtcmLmsDbHelper.InitializeDatabase();
//            UseCtcmLmsDb = true;
//        }
//        catch (Exception ex)
//        {                    
//            Logger.Error(ex, "Error init DB, working without DB connection.");
//        }                
//    }

//    UseOpCompDb = false;
//    if (Helper.IsUcsMode())
//    {
//        var opCompConnection = System.Configuration.ConfigurationManager.ConnectionStrings["OpCompConnection"];
//        if (opCompConnection is not null && !String.IsNullOrWhiteSpace(opCompConnection.ConnectionString))
//        {                    
//            try
//            {
//                OpCompDbHelper.InitializeConnection();
//                UseOpCompDb = true;
//            }
//            catch (Exception ex)
//            {

//                Logger.Error(ex, "Error init OpComp DB connection, working without OpComp DB connection.");
//            }

//        }
//    }
//}