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
using System.Threading;

namespace Ssz.Utils.Addons
{
    /// <summary>
    ///     Only GetExportedValues<T>() is thread-safe.
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
            OnCsvDb_CsvFileChanged(CsvFileChangeAction.Added, null);                        
        }        

        public void Close()
        {
            if (CsvDb.CsvDbDirectoryInfo is not null)
            {
                CsvDb.CsvFileChanged -= OnCsvDb_CsvFileChanged;
            }

            lock (ContainerSyncRoot)
            {
                _container = null;
            }
        }

        public AddonBase? GetAvailableAddon(string addonName, string? addonConfigDirectoryRelativePath)
        {
            AddonBase[] availableAddons = GetAvailableAddonsCache();

            var desiredAndAvailableAddon = availableAddons.FirstOrDefault(
                p => String.Equals(p.Name, addonName, StringComparison.InvariantCultureIgnoreCase));
            if (desiredAndAvailableAddon is not null)
            {
                try
                {
                    var desiredAndAvailableAddonClone = (AddonBase)Activator.CreateInstance(desiredAndAvailableAddon.GetType())!;
                    desiredAndAvailableAddonClone.DllFileFullName = desiredAndAvailableAddon.DllFileFullName;
                    desiredAndAvailableAddon = desiredAndAvailableAddonClone;

                    DirectoryInfo? addonConfigDirectoryInfo = null;
                    if (!String.IsNullOrEmpty(addonConfigDirectoryRelativePath))
                    {
                        if (addonConfigDirectoryRelativePath.Contains(Path.PathSeparator))
                        {
                            WrapperUserFriendlyLogger.LogError(Properties.Resources.AddonConfigDirectoryError_CannotContainPath, addonConfigDirectoryRelativePath);
                            return null;
                        }
                        addonConfigDirectoryInfo = new DirectoryInfo(Path.Combine(CsvDb.CsvDbDirectoryInfo!.FullName, addonConfigDirectoryRelativePath));
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
                            WrapperUserFriendlyLogger.LogError(Properties.Resources.AddonConfigDirectoryError, addonConfigDirectoryRelativePath);
                    }

                    var parameters = new List<object>();
                    if (UserFriendlyLogger is not null)
                        parameters.Add(UserFriendlyLogger);
                    if (addonConfigDirectoryInfo is not null)
                        parameters.Add(addonConfigDirectoryInfo);
                    desiredAndAvailableAddon.CsvDb = ActivatorUtilities.CreateInstance<CsvDb>(ServiceProvider, parameters.ToArray());
                    var idNameValueCollection = desiredAndAvailableAddon.CsvDb.GetFileNameAndLastWriteTimeUtcDictionary();
                    idNameValueCollection.Add(@"addonName", addonName);
                    idNameValueCollection.Add(@"addonConfigDirectoryRelativePath", addonConfigDirectoryRelativePath);
                    desiredAndAvailableAddon.Id = NameValueCollectionHelper.GetNameValueCollectionString(idNameValueCollection);
                    desiredAndAvailableAddon.Logger = Logger;
                    desiredAndAvailableAddon.UserFriendlyLogger = UserFriendlyLogger;
                    desiredAndAvailableAddon.WrapperUserFriendlyLogger = WrapperUserFriendlyLogger;
                    desiredAndAvailableAddon.Configuration = Configuration;
                    desiredAndAvailableAddon.ServiceProvider = ServiceProvider;
                }
                catch (Exception ex)
                {
                    WrapperUserFriendlyLogger.LogError(ex, Properties.Resources.DesiredAddonFailed, addonName);
                    desiredAndAvailableAddon = null;
                }
            }
            else
            {
                WrapperUserFriendlyLogger.LogError(Properties.Resources.DesiredAddonIsNotFound, addonName);
            }

            return desiredAndAvailableAddon;
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetExportedValues<T>()
        {
            var result = new List<T>();
            lock (ContainerSyncRoot)
            {
                if (_container is not null)
                    try
                    {
                        result.AddRange(_container.GetExportedValues<T>());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, @"_container.GetExportedValues<T> Error.");
                    }
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
                RefreshAddons();
            }
        }

        private void RefreshAddons()
        {
            ResetAvailableAddonsCache();

            var newAddons = new List<AddonBase>();
            var catalog = new AggregateCatalog();
            foreach (var line in CsvDb.GetValues(AddonBase.AddonsCsvFileName).Values)
            {
                if (line.Count < 2 || String.IsNullOrEmpty(line[0]) || String.IsNullOrEmpty(line[1]))
                    continue;

                string addonName = line[1]!;
                string? addonConfigDirectoryRelativePath = line.Count > 2 ? line[2] : null;

                var desiredAndAvailableAddon = GetAvailableAddon(addonName, addonConfigDirectoryRelativePath);                    
                if (desiredAndAvailableAddon is not null)
                {
                    newAddons.Add(desiredAndAvailableAddon);

                    catalog.Catalogs.Add(new DirectoryCatalog(
                            Path.GetDirectoryName(desiredAndAvailableAddon.DllFileFullName)!,
                            Path.GetFileName(desiredAndAvailableAddon.DllFileFullName)));
                }                
            }

            lock (ContainerSyncRoot)
            {
                _container = new CompositionContainer(catalog);
            }
            Addons.Update(newAddons.OrderBy(a => a.Id).ToArray());
        }        

        private void ResetAvailableAddonsCache()
        {
            _availableAddons = null;
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

        private readonly object ContainerSyncRoot = new();

        private CompositionContainer? _container;

        /// <summary>
        ///     Addons are just created, only DllFileFullName propery is set.
        /// </summary>
        private AddonBase[]? _availableAddons;

        #endregion
    }
}