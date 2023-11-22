using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Dcs.CentralServer.Common;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.Logging;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public partial class DsDevice: IDisposable
    {
        #region public functions

        /// <summary>
        ///     If dataDirectoryInfo is null, directory is not used.
        ///     If !dataDirectoryInfo.Exists, directory is created.
        ///     If dispatcher is not null monitors dataDirectoryInfo
        ///     processDataAccessProvider must be called only in DoWork function, where it is guaranted initialized.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="processDataAccessProvider"></param>
        /// <param name="processDirectoryInfo"></param>
        /// <param name="dispatcher"></param>
        public DsDevice(ILogger<DsDevice> logger, IServiceProvider serviceProvider,            
            GrpcDataAccessProvider processDataAccessProvider, 
            DirectoryInfo? processDirectoryInfo = null, IDispatcher? dispatcher = null)
        {            
            _logger = logger;            
            ProcessDataAccessProvider = processDataAccessProvider;
            _processDirectoryInfo = processDirectoryInfo;
            if (_processDirectoryInfo is not null)
            {                
                _dataDirectoryInfo = new DirectoryInfo(Path.Combine(_processDirectoryInfo.FullName, DsFilesStoreConstants.ControlEngineDataDirectoryNameUpper));
            }
            _dispatcher = dispatcher;

            if (_dataDirectoryInfo is not null)
            {
                UserFriendlyLogger = new SszLogger(@"LoadLogger", new SszLoggerOptions
                {
                    LogLevel = LogLevel.Warning,
                    LogsDirectory = _dataDirectoryInfo.FullName,
                    LogFileName = "Load.log"
                });

                // Creates all directories and subdirectories in the specified path unless they already exist.
                Directory.CreateDirectory(_dataDirectoryInfo.FullName);                

                if (_dispatcher is not null)
                    try
                    {
                        _xmlFileSystemWatcher.Created += FileSystemWatcherOnEvent;
                        _xmlFileSystemWatcher.Changed += FileSystemWatcherOnEvent;
                        _xmlFileSystemWatcher.Deleted += FileSystemWatcherOnEvent;
                        _xmlFileSystemWatcher.Renamed += FileSystemWatcherOnEvent;
                        _xmlFileSystemWatcher.Path = _dataDirectoryInfo.FullName;
                        _xmlFileSystemWatcher.Filter = @"*.xml";
                        _xmlFileSystemWatcher.IncludeSubdirectories = false;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AppSettings FilesStore directory error. Please, specify correct directory and restart service.");
                    }

                _blockFilesCache.Load(_dataDirectoryInfo);
            }

            var parameters = new List<object>();
            if (UserFriendlyLogger != null) parameters.Add(UserFriendlyLogger);
            if (_dataDirectoryInfo != null) parameters.Add(_dataDirectoryInfo);
            if (_dispatcher != null) parameters.Add(_dispatcher);            
            CsvDb = ActivatorUtilities.CreateInstance<CsvDb>(serviceProvider, parameters.ToArray());
            CsvDb.CsvFileChanged += OnCsvDb_CsvFileChanged;

            ElementIdsMap = ActivatorUtilities.CreateInstance<ElementIdsMap>(serviceProvider);            
            HistoryValues = ActivatorUtilities.CreateInstance<HistoryValues>(serviceProvider);

            _deviceModule = new DsModule(@"SYSTEM", this);
            _deviceDsBlock = (DeviceDsBlock)DsBlocksFactory.CreateDsBlock(DsBlocksFactory.Device_DsBlockType, "SYSTEM", _deviceModule, null);
            _deviceModule.ChildDsBlocks = new[] { _deviceDsBlock };

            Modules = new[] { _deviceModule };

            OnCsvDb_CsvFileChanged(null, new CsvFileChangedEventArgs { CsvFileChangeAction = CsvFileChangeAction.Added, CsvFileName = @"" });
        }

        // <summary>
        ///     This is the implementation of the IDisposable.Dispose method.  The client
        ///     application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                if (_dataDirectoryInfo is not null && _dataDirectoryInfo.Exists)
                    _blockFilesCache.Save(_dataDirectoryInfo);
            }

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~DsDevice()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>        
        public bool Disposed { get; private set; }

        #endregion

        #region public functions            

        public IUserFriendlyLogger? UserFriendlyLogger { get; }

        /// <summary>
        ///     You can add items only in DoWork, when it is Initialized
        /// </summary>
        public GrpcDataAccessProvider ProcessDataAccessProvider { get; }

        public ElementIdsMap ElementIdsMap { get; }

        public HistoryValues HistoryValues { get; }

        public CsvDb CsvDb { get; }

        public DsModulesTempRuntimeData ModulesTempRuntimeData => _modulesTempRuntimeData;

        public Guid DataGuid { get; set; }

        public const ulong ModelTimeDeltaMs = 100;

        /// <summary>
        ///     Currently calculating Model Time (milliseconds)
        /// </summary>
        public UInt64 ModelTimeMs => _deviceDsBlock.MODEL_TIME_MS.Value.ValueAs<UInt64>(false);

        /// <summary>
        ///     Modules can be reused.
        /// </summary>
        public DsModule[] Modules
        {
            get
            {
                return _modules;
            }
            set
            {                
                _modules = value;
                _modulesTempRuntimeData = new DsModulesTempRuntimeData(_modules);
            }
        }

        public void DoWork(DateTime nowUtc, CancellationToken cancellationToken)
        {
            UInt64 modelTimeMs = _deviceDsBlock.MODEL_TIME_MS.Value.ValueAs<UInt64>(false);
            if (_desiredModelTimeMs >= ModelTimeDeltaMs && modelTimeMs <= _desiredModelTimeMs - ModelTimeDeltaMs)
            {
                // Set before compute, because child blocks can use it.
                modelTimeMs += ModelTimeDeltaMs;                
                _deviceDsBlock.MODEL_TIME_MS.Value.Set(modelTimeMs); 
                
                Compute((uint)ModelTimeDeltaMs);

                _deviceDsBlock.MODEL_TIME.Value.Set((UInt64)(modelTimeMs / 1000));
                if (modelTimeMs >= _desiredModelTimeMs)
                    _desiredModelTimeIsReached.Set();
                DataGuid = Guid.NewGuid();
            }
        }

        /// <summary>
        ///     Saves all data (config and state) to data directory.
        /// </summary>
        /// <returns></returns>
        public void SaveData()
        {
            if (_dataDirectoryInfo is null || !_dataDirectoryInfo.Exists) return;

            _xmlFileSystemWatcher.EnableRaisingEvents = false;
            CsvDb.EnableRaisingEvents = false;

            foreach (var fileInfo in _dataDirectoryInfo.EnumerateFiles(@"*.template.xml"))
            {
                try
                {
                    fileInfo.Delete();
                }
                catch
                {
                }
            }
            foreach (var fileInfo in _dataDirectoryInfo.EnumerateFiles(@"*.block.xml"))
            {
                try
                {
                    fileInfo.Delete();
                }
                catch
                {
                }
            }
            foreach (var fileInfo in _dataDirectoryInfo.EnumerateFiles(@"*.block.csv"))
            {
                try
                {
                    fileInfo.Delete();
                }
                catch
                {
                }
            }

            var savedModules = new List<DsModule>();
            foreach (var blockFileCache in _blockFilesCache.DsBlockFileCachesCollection.Values)
            {
                var blockFileFullName = Path.Combine(_dataDirectoryInfo.FullName, blockFileCache.FileName);
                var modules = blockFileCache.ModuleCachesCollection.Select(
                    mc => GetModule(mc)
                    ).ToArray();
                SaveModules(blockFileFullName, modules);
                savedModules.AddRange(modules);
                var fileInfo = new FileInfo(blockFileFullName);
                if (fileInfo.Exists)
                {
                    blockFileCache.FileLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                }                
            }

            _blockFilesCache.Save(_dataDirectoryInfo);

            {
                // save not saved yet modules          
                var blockFileFullName = Path.Combine(_dataDirectoryInfo.FullName, "main.block.xml");
                var modules = _modules.Where(m => !ReferenceEquals(m, _deviceModule) && !savedModules.Contains(m));
                SaveModules(blockFileFullName, modules);  
            }

            CsvDb.LoadData();
            CsvDb.EnableRaisingEvents = true;
            var t = FileSystemWatchersEnableRaisingEventsAsync();
        }

        public Task SaveStateAsync(string filePathRelativeToProcessDrectory)
        {
            if (_processDirectoryInfo is null || !_processDirectoryInfo.Exists) return Task.CompletedTask;

            string fileFullName = Path.Combine(_processDirectoryInfo.FullName, filePathRelativeToProcessDrectory);

            // Creates all directories and subdirectories in the specified path unless they already exist.
            Directory.CreateDirectory(Path.GetDirectoryName(fileFullName)!);

            using (var memoryStream = new MemoryStream(1024 * 1024))
            {
                var context = new DsModule.SerializationContext { SaveState = true };
                using (var writer = new SerializationWriter(memoryStream, true))
                {
                    using (writer.EnterBlock(1))
                    {                        
                        writer.Write(_modules.Length);
                        foreach (var module in _modules)
                        {                            
                            writer.Write(module.Name);
                            using (writer.EnterBlock())
                            {
                                writer.WriteOwnedDataSerializable(module, context);
                            }
                        }
                    }
                }

                using (FileStream fileStream = File.Create(fileFullName))
                {
                    memoryStream.WriteTo(fileStream);
                }
            }

            return Task.CompletedTask;
        }

        public async Task LoadStateAsync(string filePathRelativeToProcessDrectory)
        {
            if (_processDirectoryInfo is null || !_processDirectoryInfo.Exists) return;

            var fileInfo = new FileInfo(Path.Combine(_processDirectoryInfo.FullName, filePathRelativeToProcessDrectory));
            if (fileInfo.Exists)
            {
                using (var memoryStream = new MemoryStream(await File.ReadAllBytesAsync(fileInfo.FullName)))
                using (var reader = new SerializationReader(memoryStream))
                {
                    var context = new DsModule.DeserializationContext { LoadState = true };

                    using (Block block = reader.EnterBlock())
                    {
                        switch (block.Version)
                        {
                            case 1:
                                int count = reader.ReadInt32();
                                foreach (int i in Enumerable.Range(0, count))
                                {
                                    string moduleName = reader.ReadString();
                                    var module = _modulesTempRuntimeData.ModulesDictionary.TryGetValue(moduleName);
                                    using (reader.EnterBlock())
                                    {
                                        if (module is not null)
                                            reader.ReadOwnedDataSerializable(module, context);
                                    }
                                }
                                break;
                            default:
                                throw new BlockUnsupportedVersionException();
                        }
                    }
                }
            }            

            _desiredModelTimeMs = 0;
            _desiredModelTimeIsReached.Set();
            DataGuid = Guid.NewGuid();
        }        

        public async Task StepAsync(uint seconds)
        {
            if (_processDirectoryInfo is null || !_processDirectoryInfo.Exists) 
                return;

            _desiredModelTimeMs = _deviceDsBlock.MODEL_TIME_MS.Value.ValueAsUInt64(false) + seconds * 1000;
            _desiredModelTimeIsReached.Reset();

            await Task.Run(() => { _desiredModelTimeIsReached.WaitOne(); });
        }

        #endregion

        #region private functions

        private void Compute(uint dtMs)
        {
            foreach (var module in _modules)
            {
                if (module.Disposed) continue;
                module.Compute(dtMs);
            }
        }

        private void FileSystemWatcherOnEvent(object sender, FileSystemEventArgs e)
        {
            var t = LoadDataEventAsync();
        }

        private void OnCsvDb_CsvFileChanged(object? sender, CsvFileChangedEventArgs args)
        {
            _dispatcher!.BeginAsyncInvoke(async ct =>
            {
                if (args.CsvFileName == @"" ||
                String.Equals(args.CsvFileName, ElementIdsMap.StandardMapFileName, StringComparison.InvariantCultureIgnoreCase) ||
                String.Equals(args.CsvFileName, ElementIdsMap.StandardTagsFileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    ElementIdsMap.Initialize(CsvDb.GetData(ElementIdsMap.StandardMapFileName), CsvDb.GetData(ElementIdsMap.StandardTagsFileName), CsvDb);
                    // If not initialized then does nothing.
                    await ProcessDataAccessProvider.ReInitializeAsync();
                }

                if (args.CsvFileName.EndsWith(@".BLOCK.CSV", StringComparison.InvariantCultureIgnoreCase))
                {
                    await LoadDataEventAsync();
                }
            });            
        }

        private async Task LoadDataEventAsync()
        {
            if (_loadDataEventIsProcessing) return;
            _loadDataEventIsProcessing = true;
            await Task.Delay(1000);
            _loadDataEventIsProcessing = false;

            _dispatcher!.BeginInvoke(ct =>
            {
                LoadData();
            });
        }        

        /// <summary>
        ///     Loads all data (config and state) from data directory.
        /// </summary>
        private void LoadData()
        {
            if (_dataDirectoryInfo is null || !_dataDirectoryInfo.Exists) return;            

            var newDsBlockFilesCache = new DsBlockFilesCache();
            var newModulesCollection = new List<DsModule>();
            newModulesCollection.Add(_deviceModule);

            bool dataLoadedfromTemplateDsBlockFile = false;
            foreach (FileInfo fileInfo in _dataDirectoryInfo.GetFiles(@"*.template.xml", SearchOption.TopDirectoryOnly))
            {
                var modules = ProcessDsBlockFile(UserFriendlyLogger, fileInfo, newDsBlockFilesCache, 
                    false, out bool dataLoadedfromDsBlockFile);
                foreach (var module in modules)
                {
                    foreach (var componentDsBlock in module.ChildDsBlocks.OfType<ComponentDsBlock>())
                    {
                        componentDsBlock.IsTemplate = true;
                    }
                }                
                newModulesCollection.AddRange(modules);
                if (dataLoadedfromDsBlockFile) dataLoadedfromTemplateDsBlockFile = true;
            }

            foreach (FileInfo fileInfo in _dataDirectoryInfo.GetFiles(@"*.block.xml", SearchOption.TopDirectoryOnly))
            {                
                newModulesCollection.AddRange(ProcessDsBlockFile(UserFriendlyLogger, fileInfo, newDsBlockFilesCache,
                    dataLoadedfromTemplateDsBlockFile, out bool dataLoadedfromDsBlockFile));
            }

            foreach (FileInfo fileInfo in _dataDirectoryInfo.GetFiles(@"*.block.csv", SearchOption.TopDirectoryOnly))
            {
                newModulesCollection.AddRange(ProcessDsBlockFile(UserFriendlyLogger, fileInfo, newDsBlockFilesCache, 
                    dataLoadedfromTemplateDsBlockFile, out bool dataLoadedfromDsBlockFile));
            }

            _blockFilesCache = newDsBlockFilesCache;
            foreach (var module in Modules)
            {
                if (!newModulesCollection.Contains(module))
                {
                    module.Dispose();
                }
            }
            Modules = newModulesCollection.ToArray();
        }

        private async Task FileSystemWatchersEnableRaisingEventsAsync()
        {
            while (true)
            {
                try
                {
                    _xmlFileSystemWatcher.EnableRaisingEvents = true;                    
                    break;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }
        }

        private List<DsModule> ProcessDsBlockFile(ILogger? userFriendlyLogger, FileInfo blockFileInfo, DsBlockFilesCache newDsBlockFilesCache, 
            bool forceDataLoadFromDsBlockFile, out bool dataLoadedFromDsBlockFile)
        {
            dataLoadedFromDsBlockFile = false;

            var newModules = new List<DsModule>();
            try
            {
                var blockFileCache = _blockFilesCache.DsBlockFileCachesCollection.TryGetValue(blockFileInfo.Name);
                if (!forceDataLoadFromDsBlockFile && blockFileCache is not null &&
                    FileSystemHelper.FileSystemTimeIsEquals(blockFileCache.FileLastWriteTimeUtc, blockFileInfo.LastWriteTimeUtc))
                {
                    foreach (var moduleCache in blockFileCache.ModuleCachesCollection)
                    {
                        var module = GetModule(moduleCache);                        
                        newModules.Add(module);
                    }
                }
                else
                {
                    blockFileCache = new DsBlockFilesCache.DsBlockFileCache
                    {
                        FileName = blockFileInfo.Name,
                        FileLastWriteTimeUtc = blockFileInfo.LastWriteTime
                    };
                    var newModules2 = LoadModules(userFriendlyLogger, blockFileInfo.FullName);
                    foreach (var newModule in newModules2)
                    {
                        blockFileCache.ModuleCachesCollection.Add(
                            new DsBlockFilesCache.DsModuleCache
                            {
                                ModuleName = newModule.Name,
                                ModuleSerializationData = null,
                                ModuleRef = newModule
                            });                        
                    }
                    newModules.AddRange(newModules2);
                    dataLoadedFromDsBlockFile = true;
                }

                newDsBlockFilesCache.DsBlockFileCachesCollection.Add(blockFileCache.FileName, blockFileCache);
            }
            catch (Exception ex)
            {
                userFriendlyLogger?.LogError(ex, Properties.Resources.FileReadingError + " " + blockFileInfo.FullName);
            }

            return newModules;
        }

        private DsModule GetModule(DsBlockFilesCache.DsModuleCache moduleCache)
        {
            var module = moduleCache.ModuleRef;
            if (module is null)
            {
                module = new DsModule(moduleCache.ModuleName, this);
                using (var memoryStream = new MemoryStream(moduleCache.ModuleSerializationData!))
                using (var reader = new SerializationReader(memoryStream))
                {
                    module.DeserializeOwnedData(reader, null);
                }
                moduleCache.ModuleSerializationData = null;
                moduleCache.ModuleRef = module;
            }
            return module;
        }

        /// <summary>
        ///     Returns list of new DsModules.
        ///     New DsModules also are added to newDsModulesCollection.
        /// </summary>
        /// <param name="blockFileFullName"></param>
        /// <param name="newDsModulesCollection"></param>
        /// <returns></returns>
        private List<DsModule> LoadModules(ILogger? userFriendlyLogger, string blockFileFullName)
        {
            if (blockFileFullName.EndsWith(@".xml", StringComparison.InvariantCultureIgnoreCase))
            {
                return LoadModulesFromXml(userFriendlyLogger, blockFileFullName);
            }            
            else if (blockFileFullName.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase))
            {
                return LoadModulesFromCsv(userFriendlyLogger, blockFileFullName);
            }
            return new List<DsModule>();
        }

        private void SaveModules(string blockFileFullName, IEnumerable<DsModule> modules)
        {
            if (blockFileFullName.EndsWith(@".xml", StringComparison.InvariantCultureIgnoreCase))
            {
                SaveModulesToXml(blockFileFullName, modules);
            }
            else if (blockFileFullName.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase))
            {
                SaveModulesToCsv(blockFileFullName, modules);
            }            
        }

        #endregion

        #region private fields        

        private ILogger<DsDevice> _logger;

        private DirectoryInfo? _processDirectoryInfo;

        private DirectoryInfo? _dataDirectoryInfo;

        private IDispatcher? _dispatcher;        

        private readonly FileSystemWatcher _xmlFileSystemWatcher = new();        

        private volatile bool _loadDataEventIsProcessing;        

        private DsBlockFilesCache _blockFilesCache = new();

        private readonly DsModule _deviceModule;

        private readonly DeviceDsBlock _deviceDsBlock;

        private DsModule[] _modules = DsModule.ModulesEmptyArray;

        private DsModulesTempRuntimeData _modulesTempRuntimeData = DsModulesTempRuntimeData.Empty;        

        private UInt64 _desiredModelTimeMs;

        private readonly ManualResetEvent _desiredModelTimeIsReached = new ManualResetEvent(true);

        #endregion
    }
}



//private void ProcessDataAccessProviderReInitialize()
//{
//    foreach (DsModule module in _modules)
//    {
//        foreach (DsBlockBase block in module.DsBlocksTempRuntimeData.DescendantDsBlocks)
//        {
//            foreach (int index in Enumerable.Range(0, block.Params.Length))
//            {
//                ref var param = ref block.Params[index];
//                var connections = param.Connections;
//                if (connections is not null)
//                {
//                    foreach (int i in Enumerable.Range(0, connections.Length))
//                    {
//                        var connection = connections[i] as DaDsConnection;
//                        if (connection is not null)
//                        {
//                            connection.SetUnsubscribed();
//                        }
//                    }
//                }
//                else
//                {
//                    var connection = param.Connection as DaDsConnection;
//                    if (connection is not null)
//                    {
//                        connection.SetUnsubscribed();
//                    }
//                }
//            }
//        }
//    }

//    ProcessDataAccessProvider.ReInitialize();
//}
