using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using OwnedDataSerializableAndCloneable = Ssz.Operator.Core.Utils.OwnedDataSerializableAndCloneable;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using Microsoft.Extensions.FileProviders;

namespace Ssz.Operator.Core.Addons
{
    public static class AddonsManager
    {
        #region public functions

        public static string AddonsSearchPattern { get; set; } = @"Ssz.Operator.Addons.*.dll";

        public static readonly AddonsCollection AddonsCollection =
            new();

        public static void Initialize(Guid[] desiredAdditionalAddonsGuids)
        {
            var newAddonsCollection = new List<AddonBase>(StandardAddons);

            AddonBase[]? availableAdditionalAddons = null;
            if (desiredAdditionalAddonsGuids.Length > 0)
            {
                //var watch = new System.Diagnostics.Stopwatch();
                //watch.Start();
                availableAdditionalAddons = GetAvailableAdditionalAddonsCache();

                //watch.Stop();
                //DsProject.LoggersSet.Logger.Critical("GetAvailableAdditionalAddonsCache() time = " + watch.ElapsedMilliseconds + " ms");
            }

            if (DsProject.Instance.Mode != DsProject.DsProjectModeEnum.BrowserPlayMode)
            {
                var catalog = new AggregateCatalog();                
                if (availableAdditionalAddons is not null)
                    foreach (var desiredAdditionalAddonGuid in desiredAdditionalAddonsGuids)
                    {
                        var desiredAvailableAdditionalAddon =
                            availableAdditionalAddons.FirstOrDefault(
                                p => p.Guid == desiredAdditionalAddonGuid);
                        if (desiredAvailableAdditionalAddon is not null)
                        {
                            newAddonsCollection.Add(desiredAvailableAdditionalAddon);

                            var additionalAddonFileInfo =
                                GetAssemblyFileInfo(desiredAvailableAdditionalAddon.GetType().Assembly);

                            if (additionalAddonFileInfo is not null)
                                catalog.Catalogs.Add(new DirectoryCatalog(additionalAddonFileInfo.DirectoryName ?? "",
                                    additionalAddonFileInfo.Name));
                        }
                    }

                lock (SyncRoot)
                {
                    _container = new CompositionContainer(catalog);
                }
            }

            if (!AddonsCollection.ObservableCollection.SequenceEqual(newAddonsCollection,
                AddonsEqualityComparer.Default))
            {
                var toRemoveAddonsCollection = new List<AddonBase>(AddonsCollection.ObservableCollection);
                foreach (AddonBase newAddon in newAddonsCollection)
                {
                    var addon = AddonsCollection.ObservableCollection.FirstOrDefault(p => p.Guid == newAddon.Guid);
                    if (addon is not null)
                        toRemoveAddonsCollection.RemoveAll(p => p.Guid == addon.Guid);
                    else
                        AddonsCollection.ObservableCollection.Add(newAddon);
                }

                foreach (AddonBase addon in toRemoveAddonsCollection)
                    AddonsCollection.ObservableCollection.Remove(addon);
            }
        }

        public static void Close()
        {
            AddonsCollection.ObservableCollection.Clear();

            lock (SyncRoot)
            {
                _container = null;
            }
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<EntityInfo> GetAddonsDsShapeTypes()
        {
            var dsShapeTypes = new List<EntityInfo>();
            foreach (IDsShapeFactory dsShapeFactory in GetExportedValues<IDsShapeFactory>())
            {
                var st = dsShapeFactory.GetDsShapeTypes();
                if (st is not null) dsShapeTypes.AddRange(st);
            }

            return dsShapeTypes;
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="dsShapeTypeGuid"></param>
        /// <param name="visualDesignMode"></param>
        /// <param name="loadXamlContent"></param>
        /// <returns></returns>
        public static DsShapeBase? NewDsShape(Guid dsShapeTypeGuid, bool visualDesignMode, bool loadXamlContent)
        {
            return GetExportedValues<IDsShapeFactory>()
                .Select(factory => factory.NewDsShape(dsShapeTypeGuid, visualDesignMode, loadXamlContent))
                .FirstOrDefault(result => result is not null);
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="dsShape"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static DsShapeViewBase? NewDsShapeView(DsShapeBase dsShape, Frame? frame)
        {
            return GetExportedValues<IDsShapeFactory>()
                .Select(factory => factory.NewDsShapeView(dsShape, frame))
                .FirstOrDefault(result => result is not null);
        }

        public static string? GetAddonName(Guid addonGuid)
        {
            var addon = GetAvailableAdditionalAddonsCache().FirstOrDefault(p => p.Guid == addonGuid);
            if (addon is not null) return addon.Name;
            return null;
        }

        public static string? GetDsPageTypeName(Guid dsPageTypeGuid)
        {
            if (dsPageTypeGuid == Guid.Empty) return null;
            foreach (AddonBase addon in AddonsCollection.ObservableCollection)
            {
                var dsPageTypes = addon.GetDsPageTypes();
                if (dsPageTypes is not null)
                    foreach (DsPageTypeBase dsPageType in dsPageTypes)
                        if (dsPageTypeGuid == dsPageType.Guid)
                            return dsPageType.Name;
            }

            return null;
        }

        public static DsPageTypeBase? NewDsPageTypeObject(Guid dsPageTypeGuid)
        {
            if (dsPageTypeGuid == Guid.Empty) return null;
            foreach (AddonBase addon in AddonsCollection.ObservableCollection)
            {
                var dsPageTypes = addon.GetDsPageTypes();
                if (dsPageTypes is not null)
                    foreach (DsPageTypeBase dsPageType in dsPageTypes)
                        if (dsPageTypeGuid == dsPageType.Guid)
                            return (DsPageTypeBase) dsPageType.Clone();
            }

            return null;
        }

        public static Guid GetAddonGuidFromDsPageType(Guid dsPageTypeGuid)
        {
            foreach (AddonBase addon in AddonsCollection.ObservableCollection)
            {
                var dsPageTypes = addon.GetDsPageTypes();
                if (dsPageTypes is not null)
                    foreach (DsPageTypeBase dsPageType in dsPageTypes)
                        if (dsPageTypeGuid == dsPageType.Guid)
                            return addon.Guid;
            }

            return Guid.Empty;
        }

        public static VirtualKeyboardInfo[] GetVirtualKeyboardsInfo()
        {
            var keyboardsInfo = new List<VirtualKeyboardInfo>();
            foreach (AddonBase addon in AddonsCollection.ObservableCollection)
            {
                var supportedKeyboardInAddon = addon.GetVirtualKeyboardsInfo();
                if (supportedKeyboardInAddon is not null)
                    keyboardsInfo.AddRange(supportedKeyboardInAddon);
            }

            return keyboardsInfo.ToArray();
        }

        public static Control? NewVirtualKeyboardControl(string virtualKeyboardType)
        {
            return AddonsCollection.ObservableCollection
                .Select(factory => factory.NewVirtualKeyboardControl(virtualKeyboardType))
                .FirstOrDefault(result => result is not null);
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DataEngineBase> GetDataEngines()
        {
            return GetExportedValues<DataEngineBase>().Concat(new[] {new GenericDataEngine()});
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="dataEngineGuid"></param>
        /// <returns></returns>
        public static DataEngineBase? NewDataEngineObject(Guid dataEngineGuid)
        {
            if (dataEngineGuid == Guid.Empty) return null;
            if (dataEngineGuid == GenericDataEngine.DataEngineGuid) return new GenericDataEngine();

            return GetExportedValues<DataEngineBase>().FirstOrDefault(me => me.Guid == dataEngineGuid);
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="dataEngineGuid"></param>
        /// <returns></returns>
        public static AddonBase? GetAdditionalAddon(Guid dataEngineGuid)
        {
            if (dataEngineGuid == Guid.Empty) return null;
            if (dataEngineGuid == GenericDataEngine.DataEngineGuid) return null;

            var dataEngine =
                GetExportedValues<DataEngineBase>().FirstOrDefault(me => me.Guid == dataEngineGuid);
            if (dataEngine is null) return null;

            foreach (AddonBase addon in AddonsCollection.ObservableCollection)
                if (addon.GetType().Assembly == dataEngine.GetType().Assembly)
                    return addon;

            return null;
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static AddonBase? GetAdditionalAddon(string command)
        {
            AddonBase? commandAddon = null;
            foreach (ICommandFactory commandFactory in GetExportedValues<ICommandFactory>())
                if (commandFactory.GetCommands().FirstOrDefault(c => c == command) is not null)
                {
                    foreach (AddonBase addon in AddonsCollection.ObservableCollection)
                        if (addon.GetType().Assembly == commandFactory.GetType().Assembly)
                        {
                            commandAddon = addon;
                            break;
                        }

                    break;
                }

            if (commandAddon is not null &&
                DsProject.Instance.DesiredAdditionalAddonsInfo.Any(i => i.Guid == commandAddon.Guid))
                return commandAddon;

            return null;
        }

        public static AddonBase? GetAdditionalAddon(Type type)
        {
            AddonBase? addon = null;
            foreach (AddonBase p in AddonsCollection.ObservableCollection)
                if (p.GetType().Assembly == type.Assembly)
                {
                    addon = p;
                    break;
                }

            if (addon is not null &&
                DsProject.Instance.DesiredAdditionalAddonsInfo.Any(i => i.Guid == addon.Guid))
                return addon;

            return null;
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetCommands()
        {
            var commands = new List<string>();
            foreach (ICommandFactory commandFactory in GetExportedValues<ICommandFactory>())
                commands.AddRange(commandFactory.GetCommands());
            return commands;
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static OwnedDataSerializableAndCloneable? NewDsCommandOptionsObject(string command)
        {
            return GetExportedValues<ICommandFactory>()
                .Select(factory => factory.NewDsCommandOptionsObject(command))
                .FirstOrDefault(result => result is not null);
        }

        public static PlayControlBase? NewPlayControl(Guid typeGuid, IPlayWindow window)
        {
            return AddonsCollection.ObservableCollection
                .Select(factory => factory.NewPlayControl(typeGuid, window))
                .FirstOrDefault(result => result is not null);
        }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ToolkitOperation> GetToolkitOperations()
        {
            return GetExportedValues<ToolkitOperation>();
        }

        public static void ResetAvailableAdditionalAddonsCache()
        {
            _availableAdditionalAddons = null;
        }

        public static AddonBase[] GetAvailableAdditionalAddonsCache()
        {
            if (_availableAdditionalAddons is null)
                _availableAdditionalAddons = GetAvailableAdditionalAddonsUnconditionally();
            return _availableAdditionalAddons;
        }

        public static List<GuidAndName> GetAddonsInfo(IEnumerable<Guid> addonGuids)
        {
            var result = new List<GuidAndName>();
            foreach (var addonGuid in addonGuids.Distinct())
            {
                if (addonGuid == Guid.Empty) continue;
                result.Add(new GuidAndName
                {
                    Guid = addonGuid,
                    Name = GetAddonName(addonGuid)
                });
            }

            return result;
        }


        public static string[] GetNotInAddonsCollection(List<GuidAndName>? addonsInfo)
        {
            if (addonsInfo is null) return new string[0];

            var notInAddonsCollection = new List<string>();
            foreach (GuidAndName guidAndName in addonsInfo)
                if (AddonsCollection.ObservableCollection.All(p => p.Guid != guidAndName.Guid))
                    notInAddonsCollection.Add(guidAndName.Name ?? "");
            return notInAddonsCollection.ToArray();
        }

        public static bool IsFaceplate(Guid typeGuid)
        {
            var dsPageType = NewDsPageTypeObject(typeGuid);
            if (dsPageType is null) return false;
            return dsPageType.IsFaceplate;
        }

        public static FileInfo? GetAssemblyFileInfo(Assembly assembly)
        {
            // System.Environment.ProcessPath // As of .NET 6, the recommended approach
            if (assembly.IsDynamic) return null;
            string codeBase = assembly.Location;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            if (path.StartsWith(@"/")) path = @"//" + uri.Host + path;
            return new FileInfo(path);
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Thread-safe
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static IEnumerable<T> GetExportedValues<T>()
        {
            var result = new List<T>();
            lock (SyncRoot)
            {
                /* // For Web mode
                Func<Type, IEnumerable<object>> getExportedValuesEvent = GetExportedValuesEvent;
                if (getExportedValuesEvent is not null)
                {
                    try
                    {
                        foreach (var func in getExportedValuesEvent.GetInvocationList().OfType<Func<Type, IEnumerable<object>>>())
                        {
                            foreach (object obj in func(typeof (T)))
                            {
                                result.Add((T) obj);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.Error(ex);
                    }
                }*/
                if (_container is not null)
                    try
                    {
                        result.AddRange(_container.GetExportedValues<T>());
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, @"");
                    }
            }

            return result;
        }

        private static AddonBase[] GetAvailableAdditionalAddonsUnconditionally()
        {
            if (DsProject.Instance.FileProvider is not null)
                return new AddonBase[0];

            var exeDirectory = AppContext.BaseDirectory;
            if (String.IsNullOrEmpty(exeDirectory)) 
                return new AddonBase[0];

            var addonsFileInfos = new List<FileInfo>();
            addonsFileInfos.AddRange(
                Directory.GetFiles(exeDirectory, AddonsSearchPattern, SearchOption.TopDirectoryOnly)                    
                    .Select(fn => new FileInfo(fn)));

            var addonsDirectoryInfo = new DirectoryInfo(DsProject.Instance.AddonsDirectoryFullName);
            if (addonsDirectoryInfo.Exists)
                addonsFileInfos.AddRange(
                    Directory.GetFiles(addonsDirectoryInfo.FullName, AddonsSearchPattern, SearchOption.AllDirectories)
                        .Select(fn => new FileInfo(fn)));

            var availableAdditionalAddonsList = new List<AddonBase>();
            foreach (FileInfo additionalAddonFileInfo in addonsFileInfos)
            {
                var availableAdditionalAddon = TryGetAddon(additionalAddonFileInfo);
                if (availableAdditionalAddon is not null) availableAdditionalAddonsList.Add(availableAdditionalAddon);
            }

            var availableAdditionalAddonsDictionary = new Dictionary<Guid, AddonBase>();
            var addedAddonsWithDuplicates = new Dictionary<AddonBase, List<string>>();
            foreach (AddonBase addon in availableAdditionalAddonsList)
                if (!availableAdditionalAddonsDictionary.TryGetValue(addon.Guid, out var addedAddon))
                {
                    availableAdditionalAddonsDictionary.Add(addon.Guid, addon);
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

                        foreach (AddonBase p in availableAdditionalAddonsList)
                            if (p.Guid == addedAddon.Guid && !ReferenceEquals(p, addedAddon))
                                //Only include the duplicates.  Do not include the original addon that is being
                                //"properly" loaded up.
                                duplicateFiles.Add(Path.GetDirectoryName(p.DllFileFullName) ?? "");
                    }
                }

#if !DEBUG
            if (addedAddonsWithDuplicates.Count > 0)
            {
                StringBuilder message = new StringBuilder(byte.MaxValue);
                message.AppendLine(Properties.Resources.DuplicateAddonsMessage + ":");
                foreach (var key in addedAddonsWithDuplicates.Keys)
                {
                    message.Append(key.Name);
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
                MessageBoxHelper.ShowWarning(message.ToString());
            }
#endif

            return availableAdditionalAddonsDictionary.Values.ToArray();
        }


        private static AddonBase? TryGetAddon(FileInfo dllFileInfo)
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
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogDebug(ex, @"Addon loading error.");
            }

            return null;
        }

        #endregion

        #region private fields

        private static readonly object SyncRoot = new();
        private static CompositionContainer? _container;

        private static readonly AddonBase[] StandardAddons =
        {
            new GenericEmulationAddon(),
            new PanoramaAddon(),
            new WpfModel3DAddon(),
            new ZoomboxAddon()
        };

        private static AddonBase[]? _availableAdditionalAddons;

        #endregion

        private class AddonsEqualityComparer : EqualityComparer<AddonBase>
        {
            #region public functions

            public new static readonly IEqualityComparer<AddonBase> Default = new AddonsEqualityComparer();

            public override bool Equals(AddonBase? x, AddonBase? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
                return x.Guid == y.Guid;
            }

            public override int GetHashCode(AddonBase obj)
            {
                return obj.Guid.GetHashCode();
            }

            #endregion
        }
    }
}