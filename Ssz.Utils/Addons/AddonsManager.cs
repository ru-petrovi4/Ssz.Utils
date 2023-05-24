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
            LoggersSet = new LoggersSet<AddonsManager>(logger, null);
            Configuration = configuration;
            ServiceProvider = serviceProvider;
        }

        #endregion

        #region public functions        

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
        public void Initialize(IUserFriendlyLogger? userFriendlyLogger, AddonBase[]? standardAddons, string? addonsSearchPattern, CsvDb csvDb, IDispatcher dispatcher)
        {
            LoggersSet.SetUserFriendlyLogger(userFriendlyLogger);
            StandardAddons = standardAddons;
            AddonsSearchPattern = addonsSearchPattern;

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

        /// <summary>
        ///     SourceId property is empty in result.
        /// </summary>
        /// <returns></returns>
        public ConfigurationCsvFiles ReadConfiguration()
        {
            ConfigurationCsvFiles result = new();                        

            _availableAddons = GetAvailableAddonsUnconditionally();                       

            foreach (FileInfo csvFileInfo in CsvDb.GetFileInfos())
            {
                result.ConfigurationCsvFilesCollection.Add(ConfigurationCsvFile.CreateFromFileInfo(@"", csvFileInfo));
            }

            foreach (DirectoryInfo subDirectoryInfo in CsvDb.CsvDbDirectoryInfo!.GetDirectories())
            {
                var subCsvDb = new CsvDb(CsvDb.LoggersSet.Logger, CsvDb.LoggersSet.UserFriendlyLogger, subDirectoryInfo);
                foreach (FileInfo csvFileInfo in subCsvDb.GetFileInfos())
                {
                    result.ConfigurationCsvFilesCollection.Add(ConfigurationCsvFile.CreateFromFileInfo(subDirectoryInfo.Name, csvFileInfo));
                }
            }

            return result;
        }

        /// <summary>
        ///     Throws AddonCsvFileChangedOnDiskException, if any file changed on disk and no data writtten.
        /// </summary>
        /// <param name="configurationCsvFiles"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public void WriteConfiguration(ConfigurationCsvFiles configurationCsvFiles)
        {
            foreach (ConfigurationCsvFile configurationCsvFile in configurationCsvFiles.ConfigurationCsvFilesCollection)
            {
                if (!configurationCsvFile.PathRelativeToRootDirectory.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception("addonCsvFile.Name must have .csv extension");

                if (configurationCsvFile.PathRelativeToRootDirectory.Count(f => f == '/') > 1)
                    throw new Exception("addonCsvFile.PathRelativeToRootDirectory must have no more that one '/'");

                var fileInfo = new FileInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo!.FullName, configurationCsvFile.PathRelativeToRootDirectory_PlatformSpecific));
                if (fileInfo.Exists && FileSystemHelper.FileSystemTimeIsLess(configurationCsvFile.LastWriteTimeUtc, fileInfo.LastWriteTimeUtc))
                    throw new AddonCsvFileChangedOnDiskException { FilePathRelativeToRootDirectory = configurationCsvFile.PathRelativeToRootDirectory };
            }

            foreach (ConfigurationCsvFile configurationCsvFile in configurationCsvFiles.ConfigurationCsvFilesCollection)
            {
                var fileInfo = new FileInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo!.FullName, configurationCsvFile.PathRelativeToRootDirectory_PlatformSpecific));
                fileInfo.Directory!.Create();                
                using (var writer = new StreamWriter(fileInfo.FullName, false, new UTF8Encoding(true)))
                {
                    writer.Write(configurationCsvFile.FileData);
                }
            }
        }

        /// <summary>
        ///     Returns new instances of switched ON addons.
        ///     Not need to lock SyncRoot        
        /// </summary>
        /// <returns></returns>
        public TAddon[] CreateAddonsThreadSafe<TAddon>()
            where TAddon : AddonBase
        {
            List<TAddon> result = new();
            var addonsCopy = _addonsCopy;
            foreach (var addon in addonsCopy.OfType<TAddon>().OrderBy(a => a.IsDummy).ToArray())
            {
                TAddon? newAddon = CreateAvailableAddonThreadSafe(addon, addon.InstanceId, null) as TAddon;
                if (newAddon is not null)
                    result.Add(newAddon);
            };
            return result.ToArray();
        }

        /// <summary>
        ///     Returns new instance of switched ON addon.
        ///     Not need to lock SyncRoot
        /// </summary>
        /// <typeparam name="TAddon"></typeparam>
        /// <returns></returns>
        public TAddon? CreateAddonThreadSafe<TAddon>()
            where TAddon : AddonBase

        {
            var addonsCopy = _addonsCopy;
            TAddon? addon = addonsCopy.OfType<TAddon>().OrderBy(a => a.IsDummy).FirstOrDefault();
            if (addon is null)
                return null;

            TAddon? newAddon = CreateAvailableAddonThreadSafe(addon, addon.InstanceId, null) as TAddon;
            if (newAddon is null)
                return null;

            return newAddon;
        }

        /// <summary>
        ///     Returns new instance of switched ON addon.
        ///     Not need to lock SyncRoot
        /// </summary>
        /// <typeparam name="TAddon"></typeparam>
        /// <returns></returns>
        public TAddon? CreateAddonThreadSafe<TAddon>(string addonIdentifier)
            where TAddon : AddonBase
        {
            if (String.IsNullOrEmpty(addonIdentifier))
                return null;

            var addonsCopy = _addonsCopy;
            TAddon? addon = addonsCopy.OfType<TAddon>().OrderBy(a => a.IsDummy)
                    .FirstOrDefault(a => String.Equals(a.Identifier, addonIdentifier, StringComparison.InvariantCultureIgnoreCase));
            if (addon is null)
                return null;

            TAddon? newAddon = CreateAvailableAddonThreadSafe(addon, addon.InstanceId, null) as TAddon;
            if (newAddon is null)
                return null;

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
        protected LoggersSet<AddonsManager> LoggersSet { get; private set; }

        protected IConfiguration Configuration { get; }

        protected IServiceProvider ServiceProvider { get; }

        protected AddonBase[]? StandardAddons { get; private set; }

        /// <summary>
        ///     Has value after Initialize()
        /// </summary>
        protected string? AddonsSearchPattern { get; private set; }

        /// <summary>
        ///     Has value after Initialize()
        /// </summary>
        protected CsvDb CsvDb { get; private set; } = null!;

        #endregion

        #region private functions

        private void OnCsvDb_CsvFileChanged(object? sender, CsvFileChangedEventArgs args)
        {            
            if (args.CsvFileName == AddonBase.AddonsCsvFileName)
                RefreshAddons();
        }

        private void OnAddonCsvDb_CsvFileChanged(object? sender, CsvFileChangedEventArgs args)
        {
            if (args.CsvFileName == AddonBase.OptionsCsvFileName)
                RefreshAddons();
        }

        private void RefreshAddons()
        {
            _availableAddons = null;

            var newAddons = new List<AddonBase>();
            var catalog = new AggregateCatalog();
            foreach (var line in CsvDb.GetValues(AddonBase.AddonsCsvFileName).Values)
            {
                if (line.Count < 2 || String.IsNullOrEmpty(line[0]) || line[0]!.StartsWith("!"))
                    continue;

                string addonInstanceId = line[0]!;
                string addonIdentifier = line[1] ?? @"";
                bool switchedOn = true;
                if (line.Count >= 3 && !String.IsNullOrEmpty(line[2]))
                    switchedOn = new Any(line[2]).ValueAsBoolean(false);

                if (switchedOn)
                {
                    var desiredAndAvailableAddon = CreateAvailableAddon(addonIdentifier, addonInstanceId, null);
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
        /// <param name="addonOptions"></param>
        /// <returns></returns>
        private AddonBase? CreateAvailableAddonThreadSafe(AddonBase availableAddon, string addonInstanceId, IEnumerable<IEnumerable<string?>>? addonOptions)
        {
            if (!String.IsNullOrEmpty(addonInstanceId) && addonOptions is not null)
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
                if (addonOptions is not null)
                    availableAddonClone.CsvDb.SetValues(AddonBase.OptionsCsvFileName, addonOptions);
                availableAddonClone.SubstitutedOptionsThreadSafe = new CaseInsensitiveDictionary<string?>(availableAddonClone.CsvDb.GetValues(AddonBase.OptionsCsvFileName)
                    .Where(kvp => kvp.Key != @"").Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value.Count > 1 ? SubstituteOptionValue(kvp.Key, kvp.Value[1], availableAddonClone.Identifier) : null)));
                var observableCollectionItemIds = new CaseInsensitiveDictionary<string?>(availableAddonClone.SubstitutedOptionsThreadSafe);                    
                observableCollectionItemIds.Add(@"#addonIdentifier", availableAddonClone.Identifier);
                observableCollectionItemIds.Add(@"#addonInstanceId", addonInstanceId);                
                availableAddonClone.ObservableCollectionItemId = NameValueCollectionHelper.GetNameValueCollectionString(observableCollectionItemIds);
                availableAddonClone.InstanceId = addonInstanceId;                
                availableAddonClone.LoggersSet = new LoggersSet<AddonBase>(ServiceProvider.GetService<ILogger<AddonBase>>()!, LoggersSet.UserFriendlyLogger);                
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

        private string? SubstituteOptionValue(string optionName, string? optionValue, string addonIdentifier)
        {
            if (optionValue is null || optionValue != @"appsettings.json")
                return optionValue;
            return Configuration.GetValue<string>(@"AddonsOptions:" + addonIdentifier + ":" + optionName);
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

            if (!String.IsNullOrEmpty(AddonsSearchPattern))
            {
                var exeDirectory = AppContext.BaseDirectory;

                if (exeDirectory is null) return new AddonBase[0];

                var addonsFileInfos = new List<FileInfo>();

                addonsFileInfos.AddRange(
                    Directory.GetFiles(exeDirectory, AddonsSearchPattern, SearchOption.TopDirectoryOnly)
                        .Select(fn => new FileInfo(fn)));
                try
                {
                    string addonsDirerctoryFullname = Path.Combine(exeDirectory, @"Addons");
                    if (Directory.Exists(addonsDirerctoryFullname))
                        addonsFileInfos.AddRange(
                            Directory.GetFiles(addonsDirerctoryFullname, AddonsSearchPattern, SearchOption.AllDirectories)
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

            List<string?[]> addonsAvailableFileData = new();

            foreach (AddonBase availableAddon in availableAddons)
            {
                addonsAvailableFileData.Add(new[] { availableAddon.Identifier, availableAddon.Desc, new Any(availableAddon.IsMultiInstance).ValueAsString(false) });

                foreach (var optionsInfo in availableAddon.OptionsInfo)
                {
                    addonsAvailableFileData.Add(new[] { availableAddon.Identifier + "." + optionsInfo.Item1, optionsInfo.Item2, optionsInfo.Item3 });
                }
            }

            if (!CsvDb.FileEquals(AddonBase.AddonsAvailableCsvFileName, addonsAvailableFileData))
            {
                CsvDb.FileClear(AddonBase.AddonsAvailableCsvFileName);
                CsvDb.SetValues(AddonBase.AddonsAvailableCsvFileName, addonsAvailableFileData);
                CsvDb.SaveData(AddonBase.AddonsAvailableCsvFileName);
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

        public AddonBase[] _addonsCopy = new AddonBase[0];

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
}