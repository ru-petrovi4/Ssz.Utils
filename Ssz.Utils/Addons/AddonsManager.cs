using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Dispatcher;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Ssz.Utils.Addons
{
    /// <summary>    
    ///     Only the thread that the Dispatcher was created on may access the AddonsManager directly. 
    ///     To access a AddonsManager from a thread other than the thread the AddonsManager was created on,
    ///     call BeginInvoke or BeginAsyncInvoke on the Dispatcher the IDispatcherObject is associated with.
    /// </summary>
    public class AddonsManager : IDispatcherObject
    {
        #region construction and destruction

        public AddonsManager(ILogger<AddonsManager> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            LoggersSet = new LoggersSet(logger, null);
            Configuration = configuration;
            ServiceProvider = serviceProvider;
        }

        #endregion

        #region public functions     

        /// <summary>
        ///     Addons list .csv file name.
        /// </summary>
        public const string AddonsCsvFileName = @"addons.csv";

        /// <summary>
        ///     Available addons info .csv file name.
        /// </summary>
        public const string AddonsAvailableCsvFileName = @"addons_available.csv";

        /// <summary>
        ///     Has value after Initialize(...)
        /// </summary>
        public IDispatcher? Dispatcher { get; private set; }

        /// <summary>
        ///     Switched ON addons.
        /// </summary>
        public ObservableCollection<AddonBase> Addons { get; private set; } = new();

        /// <summary>
        ///     Thread-safe Switched ON addons.
        /// </summary>
        public AddonBase[] AddonsThreadSafe => _addonsCopy;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userFriendlyLogger"></param>
        /// <param name="standardAddons"></param>
        /// <param name="addonsSearchPattern"></param>
        /// <param name="csvDb"></param>
        public void Initialize(IUserFriendlyLogger? userFriendlyLogger, AddonBase[]? standardAddons, CsvDb csvDb, IDispatcher dispatcher, Func<string?, string?>? substituteOptionFunc, AddonsManagerOptions addonsManagerOptions)
        {
            LoggersSet.SetUserFriendlyLogger(userFriendlyLogger);
            StandardAddons = standardAddons;
            AddonsManagerOptions = addonsManagerOptions;

            CsvDb = csvDb;
            if (CsvDb.CsvDbDirectoryInfo is null)
            {
                LoggersSet.WrapperUserFriendlyLogger.LogInformation(@"CsvDb.CsvDbDirectoryInfo is null.");
                return;
            }
            if (CsvDb.Dispatcher is null)
            {
                LoggersSet.WrapperUserFriendlyLogger.LogInformation(@"CsvDb.CallbackDispatcher is null.");
                return;
            }
            CsvDb.CsvFileChanged += OnCsvDb_CsvFileChanged;

            Dispatcher = dispatcher;
            _substituteOptionFunc = substituteOptionFunc;

            RefreshAddons();
        }

        /// <summary>
        ///     
        /// </summary>
        public void Close()
        {
            Addons.SafeClear();
            _addonsCopy = new AddonBase[0];

            CsvDb.CsvFileChanged -= OnCsvDb_CsvFileChanged;            

            _container = null;
        }

        public AddonStatuses GetAddonStatuses()
        {
            AddonStatuses result = new();

            foreach (AddonBase addon in AddonsThreadSafe)
            {
                result.AddonStatusesCollection.Add(addon.GetAddonStatus());
            }

            return result;
        }

        /// <summary>
        ///     SourceId property is empty in result.
        /// </summary>
        /// <returns></returns>
        public ConfigurationFiles ReadConfiguration(string? pathRelativeToRootDirectory)
        {
            ConfigurationFiles result = new();                        

            try
            {
                if (String.IsNullOrEmpty(pathRelativeToRootDirectory)) 
                {
                    _availableAddons = GetAvailableAddonsUnconditionally();

                    foreach (FileInfo csvFileInfo in CsvDb.GetFileInfos())
                    {
                        result.ConfigurationFilesCollection.Add(ConfigurationFile.CreateFromFileInfo(csvFileInfo.Name, csvFileInfo, false));
                    }

                    foreach (DirectoryInfo subDirectoryInfo in CsvDb.CsvDbDirectoryInfo!.GetDirectories())
                    {
                        foreach (FileInfo fileInfo in subDirectoryInfo.GetFiles())
                        {
                            result.ConfigurationFilesCollection.Add(ConfigurationFile.CreateFromFileInfo(subDirectoryInfo.Name + @"/" + fileInfo.Name, fileInfo, false));
                        }
                    }
                }
                else
                {
                    pathRelativeToRootDirectory = pathRelativeToRootDirectory!.Replace(Path.DirectorySeparatorChar, '/'); // Normalize

                    int slashCount = pathRelativeToRootDirectory!.Count(f => f == '/');

                    if (slashCount == 0 && !pathRelativeToRootDirectory!.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase))
                        throw new Exception("pathRelativeToRootDirectory must have .csv extension, if requests file from root directory.");
                    if (slashCount > 1)
                        throw new Exception("pathRelativeToRootDirectory must have no more that one '/'");

                    var fileInfo = new FileInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo!.FullName, pathRelativeToRootDirectory!.Replace('/', Path.DirectorySeparatorChar)));
                    if (!fileInfo.Exists)
                        throw new Exception("pathRelativeToRootDirectory: file does not exist.");
                    
                    result.ConfigurationFilesCollection.Add(ConfigurationFile.CreateFromFileInfo(pathRelativeToRootDirectory, fileInfo, true));
                }                
            }
            catch (Exception ex)
            {
                LoggersSet.WrapperUserFriendlyLogger.LogError(ex, @"Cannot read configuration files.");
            }            

            return result;
        }

        /// <summary>
        ///     Throws AddonCsvFileChangedOnDiskException, if any file changed on disk and no data writtten.
        ///     File content is always UTF8-encoded and \n as new line char, if .csv.
        /// </summary>
        /// <param name="configurationFiles"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public void WriteConfiguration(ConfigurationFiles configurationFiles)
        {
            List<ConfigurationFile> configurationFilesToWrite = new(configurationFiles.ConfigurationFilesCollection.Count);
            List<ConfigurationFile> configurationFilesToDelete = new(configurationFiles.ConfigurationFilesCollection.Count);

            foreach (ConfigurationFile configurationFile in configurationFiles.ConfigurationFilesCollection)
            {
                configurationFile.PathRelativeToRootDirectory = configurationFile.PathRelativeToRootDirectory!.Replace(Path.DirectorySeparatorChar, '/'); // Normalize

                int slashCount = configurationFile.PathRelativeToRootDirectory.Count(f => f == '/');

                if (slashCount == 0 && configurationFile.Name != AddonsCsvFileName)
                {
                    // TEMPCODE
                    continue;
                    //throw new Exception("addonCsvFile.Name must have .csv extension");
                }                
                if (slashCount > 1)
                    throw new Exception("addonCsvFile.PathRelativeToRootDirectory must have no more that one '/'");

                if (configurationFile.IsDeleted)
                {
                    configurationFilesToDelete.Add(configurationFile);                    
                }
                else
                {
                    if (configurationFile.FileData is null)
                        throw new InvalidOperationException("configurationFile.FileData is null");

                    var fileInfo = new FileInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo!.FullName, configurationFile.GetPathRelativeToRootDirectory_PlatformSpecific()));
                    if (fileInfo.Exists)
                    {
                        ConfigurationFile onDiskConfigurationFile = ConfigurationFile.CreateFromFileInfo(configurationFile.PathRelativeToRootDirectory, fileInfo, true);
                        if (!configurationFile.FileData!.SequenceEqual(onDiskConfigurationFile.FileData!))
                        {
                            if (FileSystemHelper.FileSystemTimeIsLess(configurationFile.LastWriteTimeUtc, fileInfo.LastWriteTimeUtc))
                                throw new AddonCsvFileChangedOnDiskException { FilePathRelativeToRootDirectory = configurationFile.PathRelativeToRootDirectory };
                            configurationFilesToWrite.Add(configurationFile);
                        }
                    }
                    else
                    {
                        configurationFilesToWrite.Add(configurationFile);
                    }
                }                            
            }

            foreach (ConfigurationFile configurationFile in configurationFilesToWrite)
            {
                var fileInfo = new FileInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo!.FullName, configurationFile.GetPathRelativeToRootDirectory_PlatformSpecific()));
                //     Creates all directories and subdirectories in the specified path unless they
                //     already exist.
                Directory.CreateDirectory(fileInfo.Directory!.FullName);

                if (fileInfo.Name.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var writer = new StreamWriter(fileInfo.FullName, false, new UTF8Encoding(true)))
                    {
                        writer.Write(Encoding.UTF8.GetString(configurationFile.FileData!));
                    }
                }
                else
                {
                    File.WriteAllBytes(fileInfo.FullName, configurationFile.FileData!);
                }
            }

            foreach (ConfigurationFile configurationFile in configurationFilesToDelete)
            {
                var fileInfo = new FileInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo!.FullName, configurationFile.GetPathRelativeToRootDirectory_PlatformSpecific()));
                if (fileInfo.Exists)
                {
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch
                    {
                    }
                }                
            }
        }

        /// <summary>
        ///     Returns new instances of switched ON addons.
        ///     Not need to lock SyncRoot        
        /// </summary>
        /// <returns></returns>
        public TAddon[] CreateInitializedAddonsThreadSafe<TAddon>()
            where TAddon : AddonBase
        {
            List<TAddon> result = new();
            var addonsCopy = _addonsCopy;
            foreach (var addon in addonsCopy.OfType<TAddon>().ToArray())
            {
                TAddon? newAddon = CreateAvailableAddonThreadSafe(addon, addon.InstanceId, null) as TAddon;
                if (newAddon is not null)
                {
                    newAddon.Initialize();
                    result.Add(newAddon);
                }
            };
            return result.ToArray();
        }

        /// <summary>
        ///     Returns new instance of switched ON addon.
        ///     Not need to lock SyncRoot
        /// </summary>
        /// <typeparam name="TAddon"></typeparam>
        /// <returns></returns>
        public TAddon? CreateInitializedAddonThreadSafe<TAddon>()
            where TAddon : AddonBase

        {
            var addonsCopy = _addonsCopy;
            TAddon? addon = addonsCopy.OfType<TAddon>().FirstOrDefault();
            if (addon is null)
                return null;

            TAddon? newAddon = CreateAvailableAddonThreadSafe(addon, addon.InstanceId, null) as TAddon;
            if (newAddon is null)
                return null;

            newAddon.Initialize();

            return newAddon;
        }

        /// <summary>
        ///     Returns new instance of switched ON addon.
        ///     Not need to lock SyncRoot
        /// </summary>
        /// <typeparam name="TAddon"></typeparam>
        /// <returns></returns>
        public TAddon? CreateInitializedAddonThreadSafe<TAddon>(string addonIdentifier)
            where TAddon : AddonBase
        {
            if (String.IsNullOrEmpty(addonIdentifier))
                return null;

            var addonsCopy = _addonsCopy;
            TAddon? addon = addonsCopy.OfType<TAddon>()
                    .FirstOrDefault(a => String.Equals(a.Identifier, addonIdentifier, StringComparison.InvariantCultureIgnoreCase));
            if (addon is null)
                return null;

            TAddon? newAddon = CreateAvailableAddonThreadSafe(addon, addon.InstanceId, null) as TAddon;
            if (newAddon is null)
                return null;

            newAddon.Initialize();

            return newAddon;
        }        

        /// <summary>
        ///     Create available but not necesary switched ON adddon. 
        ///     You must specify addonInstanceId or addonOptions.
        /// </summary>
        /// <param name="addonIdentifier"></param>
        /// <param name="addonInstanceId"></param>
        /// <param name="addonOptions"></param>
        /// <returns></returns>
        public AddonBase? CreateAvailableAddon(string addonIdentifier, string addonInstanceId, IEnumerable<IEnumerable<string?>>? addonOptions)
        {
            if (!String.IsNullOrEmpty(addonInstanceId) && addonOptions is not null)
                throw new InvalidOperationException("You must specify addonInstanceId or addonOptions");

            if (String.IsNullOrEmpty(addonIdentifier))
            {
                LoggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.AddonNameIsEmpty);
                return null;
            }

            if (_availableAddons is null)
                _availableAddons = GetAvailableAddonsUnconditionally();

            var availableAddon = _availableAddons.FirstOrDefault(
                p => String.Equals(p.Identifier, addonIdentifier, StringComparison.InvariantCultureIgnoreCase));
            if (availableAddon is null)
            {
                LoggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.AvailableAddonIsNotFound, addonIdentifier);
                return null;
            }

            return CreateAvailableAddonThreadSafe(availableAddon, addonInstanceId, addonOptions);
        }

        /// <summary>
        ///     
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetExportedValues<T>()
        {
            var result = new List<T>();
            if (_container is not null)
                try
                {
                    result.AddRange(_container.GetExportedValues<T>());
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, @"_container.GetExportedValues<T> Error.");
                }
            return result;
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     Has UserFriendlyLogger after Initialize(...)
        /// </summary>
        protected ILoggersSet LoggersSet { get; private set; }

        protected IConfiguration Configuration { get; }

        protected IServiceProvider ServiceProvider { get; }

        protected AddonBase[]? StandardAddons { get; private set; }

        /// <summary>
        ///     Has value after Initialize()
        /// </summary>
        protected AddonsManagerOptions? AddonsManagerOptions { get; private set; } = null!;

        /// <summary>
        ///     Has value after Initialize()
        /// </summary>
        protected CsvDb CsvDb { get; private set; } = null!;        

        #endregion

        #region private functions

        private void OnCsvDb_CsvFileChanged(object? sender, CsvFileChangedEventArgs args)
        {            
            if (String.Equals(args.CsvFileName, AddonsCsvFileName, StringComparison.InvariantCultureIgnoreCase))
                RefreshAddons();
        }

        private void OnAddonCsvDb_CsvFileChanged(object? sender, CsvFileChangedEventArgs args)
        {
            if (String.Equals(args.CsvFileName, AddonBase.OptionsCsvFileName, StringComparison.InvariantCultureIgnoreCase))
                RefreshAddons();
        }

        private void RefreshAddons()
        {
            _availableAddons = GetAvailableAddonsUnconditionally();            

            List<AddonBase> newAddons = new();
            var catalog = new AggregateCatalog();
            foreach (var line in CsvDb.GetData(AddonsCsvFileName).Values)
            {
                if (line.Count < 2 || String.IsNullOrEmpty(line[0]) || line[0]!.StartsWith("!"))
                    continue;

                string addonInstanceId = line[0]!;
                string addonIdentifier = line[1] ?? @"";
                bool switchedOn = true;
                if (line.Count >= 3 && !String.IsNullOrEmpty(line[2]))
                    switchedOn = new Any(line[2]).ValueAsBoolean(false);

                var desiredAndAvailableAddon = CreateAvailableAddon(addonIdentifier, addonInstanceId, null);

                if (desiredAndAvailableAddon is not null && (desiredAndAvailableAddon.IsAlwaysSwitchedOn || switchedOn))
                {
                    desiredAndAvailableAddon.Initialized += (s, a) => { ((AddonBase)s!).CsvDb.CsvFileChanged += OnAddonCsvDb_CsvFileChanged; };
                    desiredAndAvailableAddon.Closed += (s, a) => { ((AddonBase)s!).CsvDb.CsvFileChanged -= OnAddonCsvDb_CsvFileChanged; };                    
                    newAddons.Add(desiredAndAvailableAddon);

                    if (!String.IsNullOrEmpty(desiredAndAvailableAddon.DllFileFullName))
                        catalog.Catalogs.Add(new DirectoryCatalog(
                                Path.GetDirectoryName(desiredAndAvailableAddon.DllFileFullName)!,
                                Path.GetFileName(desiredAndAvailableAddon.DllFileFullName)));
                }                
            }

            foreach (var availableAddon in _availableAddons)
            {                
                if (availableAddon.IsAlwaysSwitchedOn && !newAddons.Any(a => a.Guid == availableAddon.Guid))
                {
                    var desiredAndAvailableAddon = CreateAvailableAddonThreadSafe(availableAddon, availableAddon.Identifier, null);

                    if (desiredAndAvailableAddon is not null)
                    {
                        desiredAndAvailableAddon.Initialized += (s, a) => { ((AddonBase)s!).CsvDb.CsvFileChanged += OnAddonCsvDb_CsvFileChanged; };
                        desiredAndAvailableAddon.Closed += (s, a) => { ((AddonBase)s!).CsvDb.CsvFileChanged -= OnAddonCsvDb_CsvFileChanged; };
                        newAddons.Add(desiredAndAvailableAddon);

                        if (!String.IsNullOrEmpty(desiredAndAvailableAddon.DllFileFullName))
                            catalog.Catalogs.Add(new DirectoryCatalog(
                                    Path.GetDirectoryName(desiredAndAvailableAddon.DllFileFullName)!,
                                    Path.GetFileName(desiredAndAvailableAddon.DllFileFullName)));
                    }
                }
            }

            _container = new CompositionContainer(catalog);

            Addons.Update(newAddons.OrderBy(a => ((IObservableCollectionItem)a).ObservableCollectionItemId).ToArray());
            _addonsCopy = Addons.ToArray();
        }

        /// <summary>
        ///     You must specify addonInstanceId or addonOptions.
        /// </summary>
        /// <param name="availableAddon"></param>
        /// <param name="addonInstanceId"></param>
        /// <param name="addonOptionsData"></param>
        /// <returns></returns>
        private AddonBase? CreateAvailableAddonThreadSafe(AddonBase availableAddon, string addonInstanceId, IEnumerable<IEnumerable<string?>>? addonOptionsData)
        {
            if (!String.IsNullOrEmpty(addonInstanceId) && addonOptionsData is not null)
                throw new InvalidOperationException("You must specify addonInstanceId or addonOptions");

            try
            {
                var availableAddonClone = (AddonBase)Activator.CreateInstance(availableAddon.GetType())!;
                availableAddonClone.DllFileFullName = availableAddon.DllFileFullName;                

                DirectoryInfo? addonConfigDirectoryInfo = null;
                if (!String.IsNullOrEmpty(addonInstanceId))
                {
                    if (addonInstanceId.Contains(Path.DirectorySeparatorChar))
                    {
                        LoggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.AddonConfigDirectoryError_CannotContainPath, addonInstanceId);
                        return null;
                    }

                    addonConfigDirectoryInfo = new DirectoryInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo!.FullName, addonInstanceId));
                    try
                    {
                        // Creates all directories and subdirectories in the specified path unless they already exist.
                        Directory.CreateDirectory(addonConfigDirectoryInfo.FullName);
                        if (!addonConfigDirectoryInfo.Exists)
                            addonConfigDirectoryInfo = null;
                    }
                    catch
                    {
                        addonConfigDirectoryInfo = null;
                    }
                    if (addonConfigDirectoryInfo is null)
                        LoggersSet.WrapperUserFriendlyLogger.LogError(Properties.Resources.AddonConfigDirectoryError, addonInstanceId);
                }

                var parameters = new List<object>();
                parameters.Add(LoggersSet.UserFriendlyLogger);
                if (addonConfigDirectoryInfo is not null)
                    parameters.Add(addonConfigDirectoryInfo);
                parameters.Add(Dispatcher!);

                availableAddonClone.CsvDb = ActivatorUtilities.CreateInstance<CsvDb>(ServiceProvider, parameters.ToArray());                
                if (addonOptionsData is not null)
                {
                    foreach (var addonOptionsValues in addonOptionsData)
                    {
                        availableAddonClone.CsvDb.SetValues(AddonBase.OptionsCsvFileName, addonOptionsValues);
                    }
                }
                var existingOptionsData = availableAddonClone.CsvDb.GetData(AddonBase.OptionsCsvFileName);
                foreach (var optionInfo in availableAddon.OptionsInfo)
                {
                    if (!existingOptionsData.ContainsKey(optionInfo.Item1))
                    {
                        availableAddonClone.CsvDb.SetValues(AddonBase.OptionsCsvFileName, new string?[] { optionInfo.Item1, optionInfo.Item3 });                        
                    }
                }
                availableAddonClone.CsvDb.SaveData();

                availableAddonClone.OptionsSubstitutedThreadSafe = new CaseInsensitiveDictionary<string?>(availableAddonClone.CsvDb.GetData(AddonBase.OptionsCsvFileName)
                    .Where(kvp => kvp.Key != @"").Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value.Count > 1 ? SubstituteOption(kvp.Value[1]) : null)));                

                var observableCollectionItemIds = new CaseInsensitiveDictionary<string?>(availableAddonClone.OptionsSubstitutedThreadSafe);                    
                observableCollectionItemIds.Add(@"#addonIdentifier", availableAddonClone.Identifier);
                observableCollectionItemIds.Add(@"#addonInstanceId", addonInstanceId);                
                availableAddonClone.ObservableCollectionItemId = NameValueCollectionHelper.GetNameValueCollectionString(observableCollectionItemIds);
                availableAddonClone.InstanceId = addonInstanceId;                
                availableAddonClone.LoggersSet = new LoggersSet(ServiceProvider.GetService<ILogger<AddonBase>>()!, LoggersSet.UserFriendlyLogger);                
                availableAddonClone.Configuration = Configuration;
                availableAddonClone.ServiceProvider = ServiceProvider;                
                return availableAddonClone;
            }
            catch (Exception ex)
            {
                LoggersSet.WrapperUserFriendlyLogger.LogError(ex, Properties.Resources.DesiredAddonFailed, availableAddon.Identifier);
                return null;
            }
        }

        private string? SubstituteOption(string? optionValue)
        {
            if (_substituteOptionFunc is not null)
                return _substituteOptionFunc(optionValue);
            else             
                return optionValue;
        }

        /// <summary>
        ///     Addons are just created, only DllFileFullName propery is set.
        /// </summary>
        /// <returns></returns>
        private AddonBase[] GetAvailableAddonsUnconditionally()
        {
            var availableAddonsList = new List<AddonBase>();

            if (StandardAddons is not null)
                availableAddonsList.AddRange(StandardAddons);

            if (!String.IsNullOrEmpty(AddonsManagerOptions!.AddonsSearchPattern))
            {
                var exeDirectory = AppContext.BaseDirectory;

                if (exeDirectory is null) return new AddonBase[0];

                var addonsFileInfos = new List<FileInfo>();

                addonsFileInfos.AddRange(
                    Directory.GetFiles(exeDirectory, AddonsManagerOptions.AddonsSearchPattern, SearchOption.TopDirectoryOnly)
                        .Select(fn => new FileInfo(fn)));
                try
                {
                    string addonsDirerctoryFullname = Path.Combine(exeDirectory, @"Addons");
                    if (Directory.Exists(addonsDirerctoryFullname))
                        addonsFileInfos.AddRange(
                            Directory.GetFiles(addonsDirerctoryFullname, AddonsManagerOptions.AddonsSearchPattern, SearchOption.AllDirectories)
                                .Select(fn => new FileInfo(fn)));
                }
                catch
                {
                }
                
                foreach (FileInfo addonFileInfo in addonsFileInfos)
                {
                    var availableAddon = TryGetAddon(addonFileInfo);
                    if (availableAddon is not null) availableAddonsList.Add(availableAddon);
                }                               
            }

            var availableAddonsDictionary = new Dictionary<Guid, AddonBase>();
            var addedAddonsWithDuplicates = new Dictionary<AddonBase, List<string>>();
            foreach (AddonBase addon in availableAddonsList)
            {
                if (!availableAddonsDictionary.TryGetValue(addon.Guid, out var addedAddon))
                {
                    availableAddonsDictionary.Add(addon.Guid, addon);
                }
                else
                {
                    var found = false;

                    //Find out if we have this plug in our duplicates list already.
                    foreach (var p in addedAddonsWithDuplicates.Keys)
                        if (p.Guid == addedAddon.Guid)
                            found = true;
                    if (!found)
                    {
                        List<string> duplicateFiles = new();
                        addedAddonsWithDuplicates.Add(addedAddon, duplicateFiles);

                        foreach (AddonBase p in availableAddonsList)
                            if (p.Guid == addedAddon.Guid && !ReferenceEquals(p, addedAddon))
                                //Only include the duplicates.  Do not include the original addon that is being
                                //"properly" loaded up.
                                duplicateFiles.Add(p.DllFileFullName);
                    }
                }
            }

#if !DEBUG
            if (addedAddonsWithDuplicates.Count > 0)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine(Properties.Resources.DuplicateAddonsMessage + " ");
                foreach (var key in addedAddonsWithDuplicates.Keys)
                {
                    message.Append(key.Identifier);
                    message.Append(" (");
                    message.Append(Path.GetFileName(key.DllFileFullName));
                    message.AppendLine(")");

                    message.Append("    using   - ");
                    message.AppendLine(Path.GetDirectoryName(key.DllFileFullName));
                    message.Append("    ignored - ");
                    foreach (string ignored in addedAddonsWithDuplicates[key])
                    {
                        message.AppendLine(ignored);
                    }
                }
                LoggersSet.WrapperUserFriendlyLogger.LogWarning(message.ToString());
            }
#endif

            var availableAddons = availableAddonsDictionary.Values.ToArray();

            if (AddonsManagerOptions.CanModifyAddonsCsvFiles)
            {
                List<string?[]> addonsAvailableFileData = new()
                {
                    new[] {
                        @"!Identifier",
                        @"!Desc",
                        @"!IsMultiInstance",
                        @"!IsAlwaysSwitchedOn"
                    }
                };

                foreach (AddonBase availableAddon in availableAddons)
                {
                    addonsAvailableFileData.Add(new[] {
                    availableAddon.Identifier,
                    availableAddon.Desc,
                    new Any(availableAddon.IsMultiInstance).ValueAsString(false),
                    new Any(availableAddon.IsAlwaysSwitchedOn).ValueAsString(false)
                });

                    foreach (var optionsInfo in availableAddon.OptionsInfo)
                    {
                        addonsAvailableFileData.Add(new[] { availableAddon.Identifier + "." + optionsInfo.Item1, optionsInfo.Item2, optionsInfo.Item3 });
                    }
                }

                if (!CsvDb.FileEquals(AddonsAvailableCsvFileName, addonsAvailableFileData))
                {
                    CsvDb.FileClear(AddonsAvailableCsvFileName);
                    CsvDb.SetData(AddonsAvailableCsvFileName, addonsAvailableFileData);
                    CsvDb.SaveData(AddonsAvailableCsvFileName);
                }
            }            

            return availableAddons;
        }

        /// <summary>
        ///     Addon is just created, only DllFileFullName propery is set.
        /// </summary>
        /// <param name="dllFileInfo"></param>
        /// <returns></returns>
        private AddonBase? TryGetAddon(FileInfo dllFileInfo)
        {
            if (!dllFileInfo.Exists) return null;

            try
            {
                var catalog = new AggregateCatalog();
                catalog.Catalogs.Add(new DirectoryCatalog(dllFileInfo.DirectoryName ?? "", dllFileInfo.Name));
                using (var container = new CompositionContainer(catalog))
                {
                    var addon = container.GetExportedValues<AddonBase>().FirstOrDefault();
                    if (addon is not null)
                    {
                        addon.DllFileFullName = dllFileInfo.FullName;
                        return addon;
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        //private FileInfo? GetAssemblyFileInfo(Assembly assembly)
        //{
        //    if (assembly.IsDynamic) return null;
        //    //string codeBase = assembly.Location;
        //    //var uri = new UriBuilder(codeBase);
        //    //string path = Uri.UnescapeDataString(uri.Path);
        //    //if (path.StartsWith(@"/")) path = @"//" + uri.Host + path;
        //    return new FileInfo(assembly.Location);
        //}

        #endregion

        #region private fields 

        private CompositionContainer? _container;

        /// <summary>
        ///     Addons are just created, only DllFileFullName propery is set.
        /// </summary>
        private AddonBase[]? _availableAddons;

        private AddonBase[] _addonsCopy = new AddonBase[0];

        private Func<string?, string?>? _substituteOptionFunc;

        #endregion

        public class AddonCsvFileChangedOnDiskException : Exception
        {
            /// <summary>
            ///     Path relative to the root of the Files Store.
            ///     Path separator is always '/'. No '/' at the begin, no '/' at the end.   
            /// </summary>
            public string FilePathRelativeToRootDirectory { get; set; } = @"";
        }
    }

    public class AddonsManagerOptions
    {
        public string? AddonsSearchPattern { get; set; }

        public bool CanModifyAddonsCsvFiles { get; set; } = true;
    }
}