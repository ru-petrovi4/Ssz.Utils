using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
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
    ///     Only GetExportedValues<T>() is thread-safe.
    ///     Lock SyncRoot in every call.
    /// </summary>
    public class AddonsManager
    {
        #region construction and destruction

        public AddonsManager(ILogger<AddonsManager> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            Logger = logger;
            Configuration = configuration;
            ServiceProvider = serviceProvider;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Lock this in every call
        /// </summary>
        public object SyncRoot { get; } = new();

        /// <summary>
        ///     Switchd ON addons.
        /// </summary>
        public ObservableCollection<AddonBase> Addons { get; } = new();

        /// <summary>
        ///     
        /// </summary>
        /// <param name="userFriendlyLogger"></param>
        /// <param name="addonsSearchPattern"></param>
        /// <param name="csvDb"></param>
        public void Initialize(IUserFriendlyLogger? userFriendlyLogger, string addonsSearchPattern, CsvDb csvDb)
        {
            UserFriendlyLogger = userFriendlyLogger;
            WrapperUserFriendlyLogger = new WrapperUserFriendlyLogger(Logger, UserFriendlyLogger);
            AddonsSearchPattern = addonsSearchPattern;
            CsvDb = csvDb;

            if (CsvDb.CsvDbDirectoryInfo is null)
            {
                WrapperUserFriendlyLogger.LogInformation(@"Addons Configuration Directory does not specified.");
                return;
            }

            CsvDb.CsvFileChanged += OnCsvDb_CsvFileChanged;

            RefreshAddons();
        }

        /// <summary>
        ///     
        /// </summary>
        public void Close()
        {
            Addons.SafeClear();

            if (CsvDb.CsvDbDirectoryInfo is not null)
            {
                CsvDb.CsvFileChanged -= OnCsvDb_CsvFileChanged;
            }

            _container = null;
        }

        /// <summary>
        ///     SourceId property is empty in result.
        /// </summary>
        /// <returns></returns>
        public ConfigurationCsvFiles ReadConfiguration()
        {
            ConfigurationCsvFiles result = new();

            if (CsvDb.CsvDbDirectoryInfo is null)
                return result;

            List<string?[]> addonsAvailableFileData = new();

            ResetAvailableAddonsCache();
            
            foreach (AddonBase availableAddon in GetAvailableAddonsCache())
            {
                addonsAvailableFileData.Add(new[] { availableAddon.Name, availableAddon.Desc });                

                foreach (var optionsInfo in availableAddon.OptionsInfo)
                {
                    addonsAvailableFileData.Add(new[] { availableAddon.Name + "." + optionsInfo.Item1, optionsInfo.Item2 });                    
                }
            }

            CsvDb.FileClear(AddonBase.AddonsAvailableCsvFileName);
            CsvDb.SetValues(AddonBase.AddonsAvailableCsvFileName, addonsAvailableFileData);
            CsvDb.SaveData(AddonBase.AddonsAvailableCsvFileName);

            foreach (FileInfo csvFileInfo in CsvDb.GetFileInfos())
            {
                result.ConfigurationCsvFilesCollection.Add(ConfigurationCsvFile.CreateFromFileInfo(@"", csvFileInfo));
            }

            foreach (DirectoryInfo subDirectoryInfo in CsvDb.CsvDbDirectoryInfo.GetDirectories())
            {
                var subCsvDb = new CsvDb(CsvDb.Logger, CsvDb.UserFriendlyLogger, subDirectoryInfo);
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
            if (CsvDb.CsvDbDirectoryInfo is null)
                return;

            foreach (ConfigurationCsvFile configurationCsvFile in configurationCsvFiles.ConfigurationCsvFilesCollection)
            {
                if (!configurationCsvFile.PathRelativeToRootDirectory.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception("addonCsvFile.Name must have .csv extension");

                if (configurationCsvFile.PathRelativeToRootDirectory.Count(f => f == '/') > 1)
                    throw new Exception("addonCsvFile.PathRelativeToRootDirectory must have no more that one '/'");

                var fileInfo = new FileInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo.FullName, configurationCsvFile.PathRelativeToRootDirectory_PlatformSpecific));
                if (fileInfo.Exists && FileSystemHelper.FileSystemTimeIsLess(configurationCsvFile.LastWriteTimeUtc, fileInfo.LastWriteTimeUtc))
                    throw new AddonCsvFileChangedOnDiskException { FilePathRelativeToRootDirectory = configurationCsvFile.PathRelativeToRootDirectory };
            }

            foreach (ConfigurationCsvFile configurationCsvFile in configurationCsvFiles.ConfigurationCsvFilesCollection)
            {
                var fileInfo = new FileInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo.FullName, configurationCsvFile.PathRelativeToRootDirectory_PlatformSpecific));
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
            lock (SyncRoot)
            {
                List<TAddon> result = new();
                foreach (var addon in Addons.OfType<TAddon>().OrderBy(a => a.IsDummy).ToArray())
                {
                    TAddon? newAddon = CreateAvailableAddon(addon, addon.InstanceId, addon.InstanceIdToDisplay) as TAddon;
                    if (newAddon is not null)
                        result.Add(newAddon);
                };
                return result.ToArray();
            }
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
            lock (SyncRoot)
            {
                TAddon? addon = Addons.OfType<TAddon>().OrderBy(a => a.IsDummy).FirstOrDefault();
                if (addon is null)
                    return null;

                TAddon? newAddon = CreateAvailableAddon(addon, addon.InstanceId, addon.InstanceIdToDisplay) as TAddon;
                if (newAddon is null)
                    return null;

                return newAddon;
            }
        }

        /// <summary>
        ///     Returns new instance of switched ON addon.
        ///     Not need to lock SyncRoot
        /// </summary>
        /// <typeparam name="TAddon"></typeparam>
        /// <returns></returns>
        public TAddon? CreateAddonThreadSafe<TAddon>(string addonName)
            where TAddon : AddonBase
        {
            lock (SyncRoot)
            {
                TAddon? addon = Addons.OfType<TAddon>().OrderBy(a => a.IsDummy)
                    .FirstOrDefault(a => String.Equals(a.Name, addonName, StringComparison.InvariantCultureIgnoreCase));
                if (addon is null)
                    return null;

                TAddon? newAddon = CreateAvailableAddon(addon, addon.InstanceId, addon.InstanceIdToDisplay) as TAddon;
                if (newAddon is null)
                    return null;

                return newAddon;
            }
        }

        /// <summary>
        ///     Create available but not necesary switched ON adddon. 
        /// </summary>
        /// <param name="addonName"></param>
        /// <param name="addonInstanceId"></param>
        /// <returns></returns>
        public AddonBase? CreateAvailableAddon(string addonName, string? addonInstanceId, string? addonInstanceIdToDisplay)
        {
            if (String.IsNullOrEmpty(addonName))
            {
                WrapperUserFriendlyLogger.LogError(Properties.Resources.AddonNameIsEmpty);
                return null;
            }

            AddonBase[] availableAddons = GetAvailableAddonsCache();

            var availableAddon = availableAddons.FirstOrDefault(
                p => String.Equals(p.Name, addonName, StringComparison.InvariantCultureIgnoreCase));
            if (availableAddon is null)
            {
                WrapperUserFriendlyLogger.LogError(Properties.Resources.AvailableAddonIsNotFound, addonName);
                return null;
            }

            return CreateAvailableAddon(availableAddon, addonInstanceId, addonInstanceIdToDisplay);
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
                    Logger.LogError(ex, @"_container.GetExportedValues<T> Error.");
                }
            return result;
        }

        #endregion

        #region protected functions

        protected ILogger Logger { get; }

        protected IConfiguration Configuration { get; }

        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        ///     Has value after Initialize()
        /// </summary>
        protected IUserFriendlyLogger? UserFriendlyLogger { get; private set; }

        /// <summary>
        ///     Writes to Logger and UserFriendlyLogger, if any.
        ///     Has value after Initialize()
        /// </summary>
        protected WrapperUserFriendlyLogger WrapperUserFriendlyLogger { get; private set; } = null!;

        /// <summary>
        ///     Has value after Initialize()
        /// </summary>
        protected string AddonsSearchPattern { get; private set; } = null!;

        /// <summary>
        ///     Has value after Initialize()
        /// </summary>
        protected CsvDb CsvDb { get; private set; } = null!;

        #endregion

        #region private functions

        private void OnCsvDb_CsvFileChanged(CsvFileChangeAction csvFileChangeAction, string? csvFileName)
        {
            if (csvFileName == null ||
                String.Equals(csvFileName, AddonBase.AddonsCsvFileName))
            {
                lock (SyncRoot)
                {
                    RefreshAddons();
                }
            }
        }

        private void RefreshAddons()
        {
            ResetAvailableAddonsCache();

            var newAddons = new List<AddonBase>();
            var catalog = new AggregateCatalog();
            foreach (var line in CsvDb.GetValues(AddonBase.AddonsCsvFileName).Values)
            {
                if (line.Count < 2 || String.IsNullOrEmpty(line[0]) || line[0]!.StartsWith("!"))
                    continue;

                string addonName = line[1] ?? @"";
                string addonInstanceId = line[0]!;
                string? addonInstanceIdToDisplay = line.Count > 2 ? line[2] : null;

                var desiredAndAvailableAddon = CreateAvailableAddon(addonName, addonInstanceId, addonInstanceIdToDisplay);
                if (desiredAndAvailableAddon is not null)
                {
                    newAddons.Add(desiredAndAvailableAddon);

                    catalog.Catalogs.Add(new DirectoryCatalog(
                            Path.GetDirectoryName(desiredAndAvailableAddon.DllFileFullName)!,
                            Path.GetFileName(desiredAndAvailableAddon.DllFileFullName)));
                }
            }

            _container = new CompositionContainer(catalog);

            Addons.Update(newAddons.OrderBy(a => a.Id).ToArray());
        }

        private void ResetAvailableAddonsCache()
        {
            _availableAddons = null;
        }

        private AddonBase? CreateAvailableAddon(AddonBase availableAddon, string? addonInstanceId, string? addonInstanceIdToDisplay)
        {
            try
            {
                var availableAddonClone = (AddonBase)Activator.CreateInstance(availableAddon.GetType())!;
                availableAddonClone.DllFileFullName = availableAddon.DllFileFullName;
                availableAddon = availableAddonClone;

                DirectoryInfo? addonConfigDirectoryInfo = null;
                if (!String.IsNullOrEmpty(addonInstanceId))
                {
                    if (addonInstanceId.Contains(Path.DirectorySeparatorChar))
                    {
                        WrapperUserFriendlyLogger.LogError(Properties.Resources.AddonConfigDirectoryError_CannotContainPath, addonInstanceId);
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
                        WrapperUserFriendlyLogger.LogError(Properties.Resources.AddonConfigDirectoryError, addonInstanceId);
                }

                var parameters = new List<object>();
                if (UserFriendlyLogger is not null)
                    parameters.Add(UserFriendlyLogger);
                if (addonConfigDirectoryInfo is not null)
                    parameters.Add(addonConfigDirectoryInfo);
                availableAddon.CsvDb = ActivatorUtilities.CreateInstance<CsvDb>(ServiceProvider, parameters.ToArray());
                var idNameValueCollection = availableAddon.CsvDb.GetFileInfos().ToDictionary(fi => fi.Name, fi => (string?)new Any(fi.LastWriteTimeUtc).ValueAsString(false));
                idNameValueCollection.Add(@"addonName", availableAddon.Name);
                if (!String.IsNullOrEmpty(addonInstanceId))
                    idNameValueCollection.Add(@"addonInstanceId", addonInstanceId);
                if (!String.IsNullOrEmpty(addonInstanceIdToDisplay))
                    idNameValueCollection.Add(@"addonInstanceIdToDisplay", addonInstanceIdToDisplay);
                availableAddon.Id = NameValueCollectionHelper.GetNameValueCollectionString(idNameValueCollection);
                availableAddon.InstanceId = addonInstanceId ?? @"";
                availableAddon.InstanceIdToDisplay = addonInstanceIdToDisplay ?? @"";
                availableAddon.Logger = Logger;
                availableAddon.UserFriendlyLogger = UserFriendlyLogger;
                availableAddon.WrapperUserFriendlyLogger = WrapperUserFriendlyLogger;
                availableAddon.Configuration = Configuration;
                availableAddon.ServiceProvider = ServiceProvider;
            }
            catch (Exception ex)
            {
                WrapperUserFriendlyLogger.LogError(ex, Properties.Resources.DesiredAddonFailed, availableAddon.Name);
                return null;
            }

            return availableAddon;
        }

        /// <summary>
        ///     Addons are just created, only DllFileFullName propery is set.
        /// </summary>
        /// <returns></returns>
        private AddonBase[] GetAvailableAddonsCache()
        {
            if (_availableAddons is null)
                _availableAddons = GetAvailableAddonsUnconditionally();
            return _availableAddons;
        }

        /// <summary>
        ///     Addons are just created, only DllFileFullName propery is set.
        /// </summary>
        /// <returns></returns>
        private AddonBase[] GetAvailableAddonsUnconditionally()
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

            var availableAddonsList = new List<AddonBase>();
            foreach (FileInfo addonFileInfo in addonsFileInfos)
            {
                var availableAddon = TryGetAddon(addonFileInfo);
                if (availableAddon is not null) availableAddonsList.Add(availableAddon);
            }

            var availableAddonsDictionary = new Dictionary<Guid, AddonBase>();
            var addedAddonsWithDuplicates = new Dictionary<AddonBase, List<string>>();
            foreach (AddonBase addon in availableAddonsList)
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
                                duplicateFiles.Add(Path.GetDirectoryName(p.DllFileFullName) ?? "");
                    }
                }

            //#if !DEBUG
            //            if (addedAddonsWithDuplicates.Count > 0)
            //            {
            //                StringBuilder message = new StringBuilder(byte.MaxValue);
            //                message.AppendLine(Properties.Resources.DuplicateAddonsMessage + ":");
            //                foreach (var key in addedAddonsWithDuplicates.Keys)
            //                {
            //                    message.Append(key.Name);
            //                    message.Append(" (");
            //                    message.Append(Path.GetFileName(key.DllFileFullName));
            //                    message.AppendLine(")");

            //                    message.Append("    using   - ");
            //                    message.AppendLine(Path.GetDirectoryName(key.DllFileFullName));
            //                    message.Append("    ignored - ");
            //                    foreach (string ignored in addedAddonsWithDuplicates[key])
            //                    {
            //                        message.AppendLine(ignored);
            //                    }
            //                }
            //                MessageBoxHelper.ShowWarning(message.ToString());
            //            }
            //#endif

            return availableAddonsDictionary.Values.ToArray();
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