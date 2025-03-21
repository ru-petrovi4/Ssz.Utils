using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.CustomExceptions;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using MsBox.Avalonia;
using Ssz.Utils;
//using Ssz.Utils.Wpf;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;

namespace Ssz.Operator.Core
{
    //[DsCategoryOrder(ResourceStrings.AddonsCategory, 0)]
    //[DsCategoryOrder(ResourceStrings.BasicCategory, 1)]
    //[DsCategoryOrder(ResourceStrings.DataCategory, 2)]
    //[DsCategoryOrder(ResourceStrings.BehaviourCategory, 3)]
    //[DsCategoryOrder(ResourceStrings.AppInfoCategory, 4)]
    public partial class DsProject : ViewModelBase,        
        IDsContainer,
        IUsedAddonsInfo
    {
        #region construction and destruction

        protected DsProject()
        {
            Mode = DsProjectModeEnum.Uninitialized;

            DsConstantsCollection.CollectionChanged += DsConstantsCollectionChanged;

            AddonsCommandLineOptions = new CaseInsensitiveDictionary<string?>();
            _globalUITimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal,
                GlobalUITimerTimerCallback);
            _globalUITimer.Start();
        }

        #endregion

        #region public functions

        public static DsProject Instance { get; } = new();

        [Browsable(false)]
        public static ILoggersSet LoggersSet { get; set; } = null!;

        public static readonly string AllDsPagesCacheFileName = @"dspages.cache";

        //public static void Initialize()
        //{
        //    // Register Utils for acces from converters.
        //    //DynamicExpressionParser.Initialize(new[] {typeof(Utils)});
        //}

        public static async Task CreateNewAsync(
            string dsProjectFileFullName, 
            DsProjectModeEnum mode,
            IProgressInfo? progressInfo = null,
            IFileProvider? fileProvider = null)
        {
            Instance.Close();

            await Instance.InitializeAsync(dsProjectFileFullName, mode, false, false, fileProvider);

            //  Add addons that has IsAutoSwitchOnForNewDsProjects == true
            var addonGuids = AddonsManager.GetAvailableAdditionalAddonsCache()
                .Where(p => p.IsAutoSwitchOnForNewDsProjects).Select(p => p.Guid);
            Instance.DesiredAdditionalAddonsInfo = AddonsManager.GetAddonsInfo(addonGuids);

            if (Instance.Mode == DsProjectModeEnum.VisualDesignMode)
            {
                try
                {
                    string fileFullName = Path.Combine(Instance.CsvDbDirectoryFullName, Instance.DataEngine.ElementIdsMapFileName);
                    if (!File.Exists(fileFullName))
                        using (StreamWriter w = File.CreateText(fileFullName))
                        {
                            w.WriteLine("# You can create mapping between interface and model OPC tags");
                            w.WriteLine("INTERFACE_TAG.PTDESC,MODEL_TAG.TAGDESC");
                        }
                }
                catch (Exception)
                {
                }
            }

            Instance.SaveUnconditionally();

            if (Instance.Mode == DsProjectModeEnum.VisualDesignMode)
            {
                FileInfo[] dsPageFileInfos = new DirectoryInfo(Instance.DsPagesDirectoryFullName)
                    .EnumerateFiles(@"*" + DsPageFileExtension, SearchOption.TopDirectoryOnly).ToArray();
                if (progressInfo is not null) progressInfo.ProgressBarMaxValue = dsPageFileInfos.Length * 2;

                DrawingInfo[] onDriveDsPageDrawingInfos =
                    await Instance.GetDrawingInfosAsync(dsPageFileInfos, progressInfo);

                await Instance.AllDsPagesCacheUpdateAsync(onDriveDsPageDrawingInfos, progressInfo);
            }

            Instance.OnDsPageDrawingsListChanged();
            Instance.OnDsShapeDrawingsListChanged();

            if (Instance.Initialized is not null) 
                Instance.Initialized();
        }

        public static async Task<bool> ReadDsProjectFromBinFileAsync(
            string dsProjectFileFullName,
            DsProjectModeEnum mode,
            bool isReadOnly, 
            bool autoConvert, 
            string constants, 
            IProgressInfo? progressInfo = null,
            IFileProvider? fileProvider = null)
        {
            // Files sync here
            try
            {
                if (!StreamExists(dsProjectFileFullName, fileProvider))
                    throw new ShowMessageException(dsProjectFileFullName + @": " +
                                               Resources.FileOpenErrorFileNotFound);

                Instance.Close();

                await Instance.InitializeAsync(dsProjectFileFullName, mode, isReadOnly, autoConvert, fileProvider);

                var dsProjectFileStream = await GetStreamAsync(dsProjectFileFullName);
                if (dsProjectFileStream is null)
                    throw new ShowMessageException(dsProjectFileFullName + @": " +
                                                   Resources.FileOpenErrorFileNotFound);

                await Instance.DeserializeOwnedDataAsync(new SerializationReader(dsProjectFileStream), Instance);
                dsProjectFileStream.Dispose();

                if (Instance.BinDeserializationSkippedBytesCount > 0 &&
                    mode == DsProjectModeEnum.VisualDesignMode)
                {
                    LoggersSet.Logger.LogError(Resources.NotAllDataWasReadMessage);
                    if (!autoConvert)
                        MessageBoxHelper.ShowWarning(Resources.NotAllDataWasReadMessage + @" "
                            + Resources.SeeErrorLogForDetails);
                    if (!isReadOnly)
                        try
                        {
                            File.Copy(dsProjectFileFullName, dsProjectFileFullName + ".backup", true);
                        }
                        catch (Exception)
                        {
                        }
                }

                FileInfo[]? dsPageFileInfos = null;
                if (mode != DsProjectModeEnum.BrowserPlayMode)
                {
                    dsPageFileInfos = new DirectoryInfo(Instance.DsPagesDirectoryFullName)
                        .EnumerateFiles(@"*" + DsPageFileExtension, SearchOption.TopDirectoryOnly).ToArray();
                    if (progressInfo is not null)
                    {
                        if (Instance.Mode == DsProjectModeEnum.VisualDesignMode)
                            progressInfo.ProgressBarMaxValue = dsPageFileInfos.Length * 3;
                        else
                            progressInfo.ProgressBarMaxValue = 0;
                    }
                }

                bool allDsPagesCacheIsReaded = false;
                var allDsPagesCacheFileStream = await GetStreamAsync(Path.Combine(Instance.DsProjectPath, AllDsPagesCacheFileName));
                if (allDsPagesCacheFileStream is not null)
                {
                    try
                    {
                        using (var reader = new SerializationReader(allDsPagesCacheFileStream))
                        {
                            using (Block block = reader.EnterBlock())
                            {
                                var serializationVersionDateTime = reader.ReadDateTime();
                                if (serializationVersionDateTime == DrawingBase.CurrentSerializationVersionDateTime)
                                {
                                    CancellationToken? ct = null;
                                    if (progressInfo is not null) ct = progressInfo.GetCancellationToken();
                                    using (Block block2 = reader.EnterBlock())
                                    {
                                        if (block2.Version == CurrentAllDsPagesCacheSerializationVersion)
                                        {
                                            var count = reader.ReadInt32();
                                            if (progressInfo is not null) progressInfo.ProgressBarMaxValue += count;
                                            var allDsPagesCache = new CaseInsensitiveDictionary<DsPageDrawing>();
                                            for (var i = 0; i < count; i += 1)
                                            {
                                                if (ct.HasValue && ct.Value.IsCancellationRequested)
                                                    throw new OperationCanceledException("Cancelled by user");
                                                if (progressInfo is not null && i > 0 && i % 10 == 0)
                                                {
                                                    progressInfo.ProgressBarCurrentValue += 10;
                                                    await progressInfo.SetDescriptionAsync(
                                                        Resources.ProgressInfo_ReadingCache_Description + @" " + i);
                                                    await progressInfo.RefreshProgressBarAsync();
                                                }

                                                string name = reader.ReadString();
                                                string fileFullName = Path.Combine(Instance.DsPagesDirectoryFullName, name + DsPageFileExtension);
                                                var dsPageDrawingInfo =
                                                    new DsPageDrawingInfo(fileFullName);
                                                dsPageDrawingInfo.DeserializeOwnedData(reader,
                                                    SerializationContext.IndexFile);
                                                var dsPageDrawing = new DsPageDrawing(false, false);
                                                dsPageDrawing.SetDrawingInfo(dsPageDrawingInfo);
                                                dsPageDrawing.DeserializeOwnedData(reader,
                                                    SerializationContext.IndexFile);

                                                allDsPagesCache.Add(dsPageDrawing.Name, dsPageDrawing);
                                            }

                                            Instance._allDsPagesCache = allDsPagesCache;

                                            Instance._allDsPagesCacheIsChanged = false;

                                            if (allDsPagesCache.Count > 0)
                                                allDsPagesCacheIsReaded = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LoggersSet.Logger.LogError(ex, "Cannot read dsProject.index file.");
                    }
                    finally
                    {
                        allDsPagesCacheFileStream.Dispose();
                    }
                }                                   

                if (Instance._deserializedVersion < CurrentSerializationVersion && !Instance.IsReadOnly)
                {
                    if (!Instance.SaveFilesToLatestSerializationVersion)
                    {
                        Instance.IsReadOnly = true;
                    }
                    else
                    {
                        //Instance.PrepareOldVersionDsProject();
                        Instance.SaveUnconditionally();
                        Instance.OnDsShapeDrawingsListChanged();
                    }
                }

                if (mode != DsProjectModeEnum.BrowserPlayMode)
                {
                    if (Instance.Mode == DsProjectModeEnum.VisualDesignMode || !allDsPagesCacheIsReaded)
                    {
                        DrawingInfo[] onDriveDsPageDrawingInfos =
                            await Instance.GetDrawingInfosAsync(dsPageFileInfos!, progressInfo);
                        await Instance.AllDsPagesCacheUpdateAsync(onDriveDsPageDrawingInfos, progressInfo);
                        await Instance.AllDsPagesCacheSaveAsync(progressInfo);
                    }
                }   
            }
            catch (OperationCanceledException)
            {
                Instance.Close();

                return true;
            }
            catch (ShowMessageException ex)
            {
                Instance.Close();

                LoggersSet.Logger.LogCritical(ex, @"");
                MessageBoxHelper.ShowError(ex.Message);
                return true;
            }
            catch (Exception ex)
            {
                Instance.Close();

                string message = dsProjectFileFullName + @": " + Resources.DsProjectFileOpenErrorGeneral;
                LoggersSet.Logger.LogCritical(ex, message);
                MessageBoxHelper.ShowError(message + @" " + Resources.SeeErrorLogForDetails);
                return true;
            }

            if (!String.IsNullOrEmpty(constants))
            {
                var parts = CsvHelper.ParseCsvLine(@",", constants);
                foreach (int index in Enumerable.Range(0, parts.Length / 2))
                {
                    string constantName = parts[index * 2] ?? @"";
                    if (!constantName.StartsWith(@"%(") || !constantName.EndsWith(@")"))
                        continue;                    
                    var dsConstant = Instance.GetOrCreateDsConstant(constantName);
                    dsConstant.Value = parts[index * 2 + 1] ?? @"";                    
                }
            }

            Instance.OnDsPageDrawingsListChanged();
            Instance.OnDsShapeDrawingsListChanged();

            if (Instance.Initialized is not null) Instance.Initialized();

            return false;
        }

        public static DirectoryInfo? GetStandardDsPagesAndDsShapesDirectory()
        {
            var exeDirectoryInfo = new DirectoryInfo(AppContext.BaseDirectory);
            return exeDirectoryInfo.EnumerateDirectories("StandardLibrary").FirstOrDefault();
        }

        public static Dictionary<string, List<EntityInfo>> GetEntityInfosDictionary(IEnumerable<EntityInfo> entityInfos)
        {
            var entityInfosDictionary = new Dictionary<string, List<EntityInfo>>();

            foreach (EntityInfo entityInfo in entityInfos)
            {
                List<EntityInfo>? entityInfosList;
                string group;
                if (!string.IsNullOrWhiteSpace(entityInfo.Group)) group = entityInfo.Group;
                else group = "";
                if (!entityInfosDictionary.TryGetValue(group, out entityInfosList))
                {
                    entityInfosList = new List<EntityInfo>();
                    entityInfosDictionary[group] = entityInfosList;
                }

                entityInfosList.Add(entityInfo);
            }

            return entityInfosDictionary;
        }        

        [DsCategory(ResourceStrings.AppInfoCategory)]
        [DsDisplayName(ResourceStrings.DsProjectExeVersionDateTime)]
        //[PropertyOrder(1)]
        public string ExeVersionDateTime => ObsoleteAnyHelper.ConvertTo<string>(GetExeBuildDateTimeUtc().ToLocalTime(), true);

        [DsCategory(ResourceStrings.AppInfoCategory)]
        [DsDisplayName(ResourceStrings.DsProjectDrawingCurrentSerializationVersionDateTime)]
        //[PropertyOrder(2)]
        public string DrawingCurrentSerializationVersionDateTime =>
            ObsoleteAnyHelper.ConvertTo<string>(DrawingBase.CurrentSerializationVersionDateTime, true);

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.DsProject_ConditionalDsCommandsCollection)]
        [LocalizedDescription(ResourceStrings.DsProject_ConditionalDsCommandsCollection_Description)]
        //[Editor(//typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            //typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        //[NewItemTypes(typeof(DsCommand))]
        //[PropertyOrder(1)]
        public ObservableCollection<DsCommand> ConditionalDsCommandsCollection { get; } = new();

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.DsProject_PlayWindowClassOptionsCollection)]
        [LocalizedDescription(ResourceStrings.DsProject_PlayWindowClassOptionsCollection_Description)]
        //[Editor(//typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            //typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        //[NewItemTypes(typeof(PlayWindowClassOptions))]
        //[PropertyOrder(2)]
        public ObservableCollection<PlayWindowClassOptions> PlayWindowClassOptionsCollection { get; } = new();

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProjectFileFullName)]
        //[PropertyOrder(0)]
        public string? DsProjectFileFullName { get; private set; }

        /// <summary>
        ///     Always process first 
        /// </summary>
        public IFileProvider? FileProvider { get; private set; }

        /// <summary>
        ///     Without slash at the end.
        /// </summary>
        [Browsable(false)]
        public string DsProjectPath => Path.GetDirectoryName(DsProjectFileFullName) ?? @"";           

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProjectName)]
        //[PropertyOrder(1)]
        public string Name
        {
            get => _name ?? "";
            private set => _name = value;
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProjectDesc)]
        //[PropertyOrder(2)]
        public string Desc
        {
            get => _desc ?? "";
            set
            {
                _desc = value;

                var action = DescChanged;
                if (action is not null) action();
            }
        }

        [Browsable(false)] 
        public bool IsInitialized => Mode != DsProjectModeEnum.Uninitialized;

        [Browsable(false)] 
        public DsProjectModeEnum Mode { get; private set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DefaultDsPageSize)]
        //[PropertyOrder(3)]
        public Size DefaultDsPageSize { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DsPageStretchMode)]
        [LocalizedDescription(ResourceStrings.DsProject_DsPageStretchMode_Description)]
        //[PropertyOrder(4)]
        public DsPageStretchMode DsPageStretchMode { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DsPageHorizontalAlignment)]
        [LocalizedDescription(ResourceStrings.DsProject_DsPageHorizontalAlignment_Description)]
        //[PropertyOrder(5)]
        public DsPageHorizontalAlignment DsPageHorizontalAlignment { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DsPageVerticalAlignment)]
        [LocalizedDescription(ResourceStrings.DsProject_DsPageVerticalAlignment_Description)]
        //[PropertyOrder(6)]
        public DsPageVerticalAlignment DsPageVerticalAlignment { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DsPageBackground)]
        [LocalizedDescription(ResourceStrings.DsProject_DsPageBackground_Description)]
        //[Editor(typeof(SolidBrushOrNullTypeEditor), typeof(SolidBrushOrNullTypeEditor))]
        //[PropertyOrder(7)]
        public SolidDsBrush? DsPageBackground { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_TryConvertXamlToDsShapes)]
        //[PropertyOrder(8)]
        public bool TryConvertXamlToDsShapes { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DataEngineGuidAndName)]
        //[PropertyOrder(9)]
        //[ItemsSource(typeof(DataEngineInfoItemsSource))]
        public GuidAndName DataEngineGuidAndName
        {
            get => _dataEngineGuidAndName;
            set
            {
                if (Equals(value, _dataEngineGuidAndName)) return;

                _cacheDataEnginesDictionary[_dataEngineGuidAndName.Guid] = DataEngine;

                _dataEngineGuidAndName = value;

                DataEngineBase? existingDataEngineObject;
                if (_cacheDataEnginesDictionary.TryGetValue(_dataEngineGuidAndName.Guid,
                    out existingDataEngineObject))
                {
                    DataEngine = existingDataEngineObject;
                    _cacheDataEnginesDictionary.Remove(_dataEngineGuidAndName.Guid);
                }
                else
                {
                    DataEngine = AddonsManager.NewDataEngineObject(_dataEngineGuidAndName.Guid) ?? GenericDataEngine.Instance;                    
                }

                _dataEngineGuidAndName.Name = DataEngine.NameToDisplay;
            }
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DataEngine)]
        //[PropertyOrder(10)]
        //[Editor(typeof(CloneableObjectTypeEditor), typeof(CloneableObjectTypeEditor))]
        public DataEngineBase DataEngine
        {
            get => _dataEngine;
            set => SetValueReferenceCompare(ref _dataEngine, value);
        }

        [Browsable(false)] 
        public DrawingsStoreModeEnum DrawingsStoreMode { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProjectDrawingsStoreMode)]
        //[PropertyOrder(11)]
        //[ItemsSource(typeof(DsProjectDrawingsStoreModeItemsSource))]
        public DrawingsStoreModeEnum DrawingsStoreModeForUser
        {
            get => DrawingsStoreMode;
            set
            {
                if (value != DrawingsStoreMode)
                {
                    DrawingsStoreMode = value;

                    //var messageBoxResult = WpfMessageBox.Show(MessageBoxHelper.GetRootWindow(),
                    //    Resources.ConvertAllDrawingsToNewDrawingsStoreModeQuestion,
                    //    Resources.QuestionMessageBoxCaption,
                    //    WpfMessageBoxButton.YesNoCancel,
                    //    MessageBoxImage.Question);
                    //switch (messageBoxResult)
                    //{
                    //    case WpfMessageBoxResult.Yes:
                    //        ReSaveAllDsPageDrawingsUnconditionally();
                    //        break;
                    //    case WpfMessageBoxResult.No:
                    //        break;
                    //    case WpfMessageBoxResult.Cancel:
                    //        return;
                    //}

                    OnPropertyChangedAuto();
                }
            }
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProject_OperatorUICultureName)]
        //[PropertyOrder(12)]
        public string OperatorUICultureName
        {
            get => _operatorUICultureName;
            set
            {
                if (_operatorUICultureName != value)
                {
                    _operatorUICultureName = value;

                    if (string.IsNullOrWhiteSpace(_operatorUICultureName))
                        OperatorUIResources.Culture = CultureInfo.CurrentCulture;
                    else
                        try
                        {
                            OperatorUIResources.Culture = new CultureInfo(_operatorUICultureName);
                        }
                        catch (Exception)
                        {
                            OperatorUIResources.Culture = CultureInfo.CurrentCulture;
                        }
                }
            }
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProjectRootWindowProps)]
        [LocalizedDescription(ResourceStrings.DsProjectRootWindowPropsDescription)]
        //[PropertyOrder(13)]
        //[Editor(typeof(CloneableObjectTypeEditor), typeof(CloneableObjectTypeEditor))]
        public WindowProps RootWindowProps
        {
            get => _rootWindowProps;
            set => SetValueReferenceCompare(ref _rootWindowProps, value);            
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsProjectStartDsPageFileRelativePath)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(14)]
        public string StartDsPageFileRelativePath
        {
            get => RootWindowProps.FileRelativePath;
            set
            {
                RootWindowProps.FileRelativePath = value;
                OnPropertyChanged(nameof(RootWindowProps));
            }
        }        

        [Browsable(false)] 
        public Settings Settings { get; } = new();

        [Browsable(false)] 
        public string UserName { get; set; } = @"";

        [Browsable(false)] public string UserRole { get; set; } = @"";

        [Browsable(false)] public bool Review { get; set; }

        [Browsable(false)] public bool NoSound { get; set; }

        [Browsable(false)] public bool NoAlarmsSound => Review || NoSound;

        [Browsable(false)] public bool AutoConvert { get; set; }        

        [Browsable(false)] public CaseInsensitiveDictionary<string?> AddonsCommandLineOptions { get; set; }

        [Browsable(false)]
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (value == _isReadOnly) return;
                _isReadOnly = value;
                var isReadOnlyChanged = IsReadOnlyChanged;
                if (isReadOnlyChanged is not null) isReadOnlyChanged();
            }
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.DsProjectDsConstantsCollection)]
        [LocalizedDescription(ResourceStrings.DsConstantsCollectionDescription)]
        //[PropertyOrder(1)]
        //[Editor(//typeof(CollectionWithAddRemoveTypeEditor),
            //typeof(CollectionWithAddRemoveTypeEditor))]
        public ObservableCollection<DsConstant> DsConstantsCollection { get; } = new();

        [Browsable(false)]
        public DsConstant[] HiddenDsConstantsCollection
        {
            get
            {
                _hiddenDsConstantsCollection[0] = new DsConstant(@"%(DsProjectName)", Name);
                _hiddenDsConstantsCollection[1] =
                    new DsConstant(@"%(DsProjectDesc)", !string.IsNullOrEmpty(Desc) ? Desc : " ");
                return _hiddenDsConstantsCollection;
            }
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DefaultServerAddress)]
        //[PropertyOrder(2)]
        //[ItemsSource(typeof(DefaultServerAddress_ItemsSource), true)]
        public string DefaultServerAddress { get; set; } = @"";

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.DsProject_DefaultSystemNameToConnect)]
        //[PropertyOrder(3)]
        //[ItemsSource(typeof(DefaultSystemNameToConnect_ItemsSource), true)]
        public string DefaultSystemNameToConnect { get; set; } = @"";

        /// <summary>        
        ///     Returns existing or created directory or null.
        /// </summary>
        [Browsable(false)]
        public string CsvDbDirectoryFullName => Path.Combine(DsProjectPath, @"CsvDb");

        [Browsable(false)]
        public string AddonsDirectoryFullName => Path.Combine(DsProjectPath, @"Addons");        
        
        [Browsable(false)]
        public string DsPagesDirectoryFullName => Path.Combine(DsProjectPath, @"Pages");        

        [Browsable(false)]
        public string DsShapesDirectoryFullName => Path.Combine(DsProjectPath, @"Shapes");

        [Browsable(false)]
        public bool SaveFilesToLatestSerializationVersion
        {
            get
            {
                if (_saveFilesToLatestSerializationVersion is null)
                {
                    if (IsReadOnly)
                    {
                        _saveFilesToLatestSerializationVersion = false;
                    }
                    else if (AutoConvert)
                    {
                        _saveFilesToLatestSerializationVersion = true;
                    }
                    else
                    {
                        if (Mode == DsProjectModeEnum.VisualDesignMode)
                        {
                            //if (WpfMessageBox.Show(Application.Current.MainWindow,
                            //    Resources.SaveFilesToLatestSerializationVersionQuestion,
                            //    Resources.QuestionMessageBoxCaption,
                            //    WpfMessageBoxButton.YesNo,
                            //    MessageBoxImage.Question) == WpfMessageBoxResult.Yes)
                            //    _saveFilesToLatestSerializationVersion = true;
                            //else
                            //    _saveFilesToLatestSerializationVersion = false;
                            _saveFilesToLatestSerializationVersion = false;
                        }
                        else
                        {
                            _saveFilesToLatestSerializationVersion = false;
                        }
                    }
                }

                return _saveFilesToLatestSerializationVersion.Value;
            }
        }

        [Browsable(false)]
        public List<GuidAndName> DesiredAdditionalAddonsInfo
        {
            get => _desiredAdditionalAddonsInfo ?? new List<GuidAndName>();
            set
            {
                if (_desiredAdditionalAddonsInfo is null ||
                    !_desiredAdditionalAddonsInfo.SequenceEqual(value))
                {
                    _desiredAdditionalAddonsInfo = value;

                    AddonsManager.Initialize(_desiredAdditionalAddonsInfo.Select(i => i.Guid).ToArray());

                    var action = DesiredAdditionalAddonsInfoChanged;
                    if (action is not null) action();
                }
            }
        }

        [DsCategory(ResourceStrings.AddonsCategory)]
        [DsDisplayName(ResourceStrings.DsProjectAddonsCollection)]
        //[PropertyOrder(1)]
        //[ExpandableObject]
        public AddonsCollection AddonsCollection => AddonsManager.AddonsCollection;

        //[DsCategory(ResourceStrings.AddonsCategory),
        // DsDisplayName(ResourceStrings.DsProjectActuallyUsedAddons)]
        ////[PropertyOrder(2)]        
        //public string ActuallyUsedAddons
        //{
        //    get
        //    {
        //        var dsProjectAndDrawingsActuallyUsedAddonsInfo = new List<GuidAndName>();

        //        List<GuidAndName> actuallyUsedAddonsInfo = _actuallyUsedAddonsInfo;
        //        if (actuallyUsedAddonsInfo is not null)
        //        {
        //            dsProjectAndDrawingsActuallyUsedAddonsInfo.AddRange(actuallyUsedAddonsInfo);
        //        }

        //        foreach (DsPageDrawing dsPageDrawing in AllDsPagesCache.Values)
        //        {
        //            dsProjectAndDrawingsActuallyUsedAddonsInfo.AddRange(dsPageDrawing.ActuallyUsedAddonsInfo);
        //        }
        //        //return Resources.NoDataMessage;

        //        return String.Join("; ",
        //            dsProjectAndDrawingsActuallyUsedAddonsInfo.GroupBy(i => i.Guid).Select(g => g.First().NameToDisplay));
        //    }
        //}

        [Browsable(false)]
        public DsShapeBase[] DsShapes
        {
            get => new DsShapeBase[0];
            set
            {
            }
        }

        [Browsable(false)]
        public IDsItem? ParentItem
        {
            get => null;
            set { }
        }

        [Browsable(false)] public IPlayWindowBase? PlayWindow => null;

        [Browsable(false)]
        public int GlobalUITimerPhase
        {
            get => _globalUITimerPhase;
            set => SetValue(ref _globalUITimerPhase, value);
        }

        [Browsable(false)] public int CurrentTimeSeconds { get; private set; }

        [Browsable(false)] public bool RefreshForPropertyGridIsDisabled { get; set; }

        [Browsable(false)] public CaseInsensitiveDictionary<List<object?>> GlobalVariables { get; } = new();

        [Browsable(false)]
        public ElementIdsMap ElementIdsMap { get; } = new();

        [Browsable(false)]
        public ElementIdsMap ReverseElementIdsMap { get; } = new();

        [Browsable(false)]
        public ElementIdsMap AlarmMessages_ElementIdsMap { get; } = new();

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(this);
        }

        public void EndEditInPropertyGrid()
        {
        }

        public event Action? OnInitializing;
        public event Action? Initialized;
        public event Action? DsPageDrawingsListChanged;
        public event Action? DsShapeDrawingsListChanged;
        public event Action? DsProjectFileInfoChanged;
        public event Action? IsReadOnlyChanged;
        public event Action? DescChanged;
        public event Action? DesiredAdditionalAddonsInfoChanged;        

        public event Action<int>? GlobalUITimerEvent;

        public override string ToString()
        {
            return Resources.DsProject;
        }

        public void Close()
        {
            if (!IsInitialized) return;

            if (!IsReadOnly)
            {
                string globalVariablesFileName = DataEngine.GlobalVariablesFileName;
                foreach (var kvp in GlobalVariables)
                {
                    foreach (var i in Enumerable.Range(0, kvp.Value.Count))
                    {
                        var column = i + 1;

                        var v = kvp.Value[i];
                        string? sValue;
                        if (v is null) sValue = null;
                        else sValue = ObsoleteAnyHelper.ConvertTo<string>(v, false);

                        CsvDb.SetValue(globalVariablesFileName, kvp.Key, column, sValue);
                    }
                }

                CsvDb.SaveData();
            }

            if (Mode == DsProjectModeEnum.VisualDesignMode) 
                this.TryToUnlockDsProjectDirectory();

            Mode = DsProjectModeEnum.Uninitialized;

            DsProjectFileFullName = null;
            _name = null;
            _desc = null;

            if (DsProjectFileInfoChanged is not null) DsProjectFileInfoChanged();

            _desiredAdditionalAddonsInfo = null;
            _actuallyUsedAddonsInfo = null;
            _dataEngineGuidAndName.Guid = Guid.Empty;
            _dataEngineGuidAndName.Name = null;
            IsReadOnly = false;
            _operatorUICultureName = @"";
            DsConstantsCollection.Clear();
            ConditionalDsCommandsCollection.Clear();
            PlayWindowClassOptionsCollection.Clear();

            AllDsPagesCacheDelete();
            AllComplexDsShapesCacheDelete();

            _constantValuesDictionary.Clear();
            GlobalVariables.Clear();
            CsvDb.Clear();

            AddonsManager.Close();
        }

        public void FindConstants(HashSet<string> constants)
        {
            throw new NotImplementedException();
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            throw new NotImplementedException();
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.OnPropertyGridRefreshInFields(this, container);

            foreach (DsCommand dsCommand in ConditionalDsCommandsCollection)
                ItemHelper.RefreshForPropertyGrid(dsCommand, this);
        }

        public T GetAddon<T>()
            where T : AddonBase
        {
            var addon =
                AddonsManager.AddonsCollection.ObservableCollection.FirstOrDefault(p => p.GetType() == typeof(T)) as T;
            if (addon is null)
            {
                if (Review) MessageBoxHelper.ShowError(string.Format(Resources.AddonUnavailableMessage, typeof(T)));
                return Activator.CreateInstance<T>();
            }

            return addon;
        }        

        public T GetDataEngine<T>()
            where T : DataEngineBase
        {
            var dataEngine = DataEngine as T;
            if (dataEngine is null)
                try
                {
                    dataEngine = Activator.CreateInstance<T>();

                    string message = string.Format(Resources.DataEngineUnavailableDefaultWillBeUsedMessage,
                        dataEngine.NameToDisplay);
                    LoggersSet.Logger.LogError(message);
                    if (Review) MessageBoxHelper.ShowError(message);
                }
                catch (Exception)
                {
                    string message = string.Format(Resources.DataEngineUnavailableMessage, typeof(T));
                    LoggersSet.Logger.LogError(message);
                    if (Review) MessageBoxHelper.ShowError(message);
                }

            if (dataEngine is null) throw new InvalidOperationException();
            return dataEngine;
        }

        public void OnDsPageDrawingsListChanged()
        {
            _constantValuesDictionary.Remove("page");

            var handler = DsPageDrawingsListChanged;
            if (handler is not null) handler();
        }

        public void OnDsShapeDrawingsListChanged()
        {
            _constantValuesDictionary.Remove("shape");

            var handler = DsShapeDrawingsListChanged;
            if (handler is not null) handler();
        }

        public bool SaveUnconditionally()
        {
            if (!IsInitialized) return false;

            if (IsReadOnly)
            {
                LoggersSet.Logger.LogWarning("DsProject cannot be saved in ReadOnly mode");
                return false;
            }

            try
            {
                using (var memoryStream = new MemoryStream(1024 * 1024))
                {
                    using (var writer = new SerializationWriter(memoryStream, true))
                    {
                        SerializeOwnedData(writer, this);
                    }

                    if (DsProjectFileFullName is null) throw new InvalidOperationException();
                    using (FileStream fileStream = File.Create(DsProjectFileFullName))
                    {
                        memoryStream.WriteTo(fileStream);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBoxHelper.ShowError(Resources.DsProjectSavingUnauthorizedAccessError + @" " +
                                           Resources.SeeErrorLogForDetails);
                LoggersSet.Logger.LogError(ex, @"");
                return false;
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowError(Resources.DsProjectSavingError + @" " + Resources.SeeErrorLogForDetails);
                LoggersSet.Logger.LogError(ex, @"");
                return false;
            }

            return true;
        }

        public async Task CheckDrawingsBinSerializationVersionAsync(DrawingInfo[] onDriveDrawingInfos,
            IProgressInfo? progressInfo)
        {
            if (onDriveDrawingInfos is null || onDriveDrawingInfos.Length == 0 || IsReadOnly) return;

            var saveFilesToLatestSerializationVersion = _saveFilesToLatestSerializationVersion;
            if (saveFilesToLatestSerializationVersion.HasValue && !saveFilesToLatestSerializationVersion.Value)
                return;

            var obsoleteDrawingInfosIndexes = new List<int>();
            for (var index = 0; index < onDriveDrawingInfos.Length; index += 1)
            {
                DrawingInfo drawingInfo = onDriveDrawingInfos[index];
                if (drawingInfo.SerializationVersionDateTime <
                    DrawingBase.CurrentSerializationVersionDateTime)
                    obsoleteDrawingInfosIndexes.Add(index);
            }

            if (obsoleteDrawingInfosIndexes.Count == 0) return;

            if (!SaveFilesToLatestSerializationVersion) return;

            var errorMessages = new List<string>();

            var i = 0;
            var cancellationToken = progressInfo is not null
                ? progressInfo.GetCancellationToken()
                : new CancellationToken();
            foreach (var index in obsoleteDrawingInfosIndexes)
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (progressInfo is not null && i > 0 && i % 10 == 0)
                {
                    progressInfo.ProgressBarCurrentValue += 10;
                    await progressInfo.SetDescriptionAsync(Resources.CheckDrawingsBinSerializationVersion + @" " + i);
                    await progressInfo.RefreshProgressBarAsync();
                }

                i += 1;

                DrawingInfo drawingInfo = onDriveDrawingInfos[index];

                var drawing = await ReadDrawingAsync(drawingInfo.FileFullName, false, true);
                if (drawing is null)
                {
                    errorMessages.Add(drawingInfo.FileName + @": " + Resources.DrawingFileOpenErrorGeneral + @" " +
                                      Resources.SeeErrorLogForDetails);
                    continue;
                }

                string[] unSupportedAddonsNameToDisplays =
                    AddonsManager.GetNotInAddonsCollection(drawing.ActuallyUsedAddonsInfo);
                if (unSupportedAddonsNameToDisplays.Length > 0)
                {
                    errorMessages.Add(drawingInfo.FileName + @": " + Resources.DrawingSaveErrorUnSupportedAddons +
                                      @" (" +
                                      string.Join(@", ", unSupportedAddonsNameToDisplays) + @") " +
                                      Resources.SeeErrorLogForDetails);
                    continue;
                }

                var generateNewGuid = false;
                if (drawing is DsPageDrawing) generateNewGuid = true;

                foreach (DsShapeBase dsShape in drawing.DsShapes)
                    dsShape.RefreshForPropertyGrid(dsShape.Container);

                if (!(await Instance.SaveUnconditionallyAsync(drawing,
                    IfFileExistsActions.CreateBackup,
                    generateNewGuid,
                    errorMessages)))
                    onDriveDrawingInfos[index] = drawing.GetDrawingInfo();

                drawing.Dispose();
            }

            if (errorMessages.Count > 0 && Mode == DsProjectModeEnum.VisualDesignMode)
                MessageBoxHelper.ShowWarning(string.Join("\n", errorMessages));
        }

        public IEnumerable<Guid> GetUsedAddonGuids()
        {
            if (DataEngine is not null)
                foreach (var guid in DataEngine.GetUsedAddonGuids())
                    yield return guid;

            foreach (DsCommand dsCommand in ConditionalDsCommandsCollection)
            foreach (var guid in dsCommand.GetUsedAddonGuids())
                yield return guid;
        }

        public async Task<CaseInsensitiveDictionary<DsPageDrawingInfo>> GetAllDsPageDrawingInfosAsync(
            List<string>? errorMessages = null)
        {
            if (!IsInitialized) return new CaseInsensitiveDictionary<DsPageDrawingInfo>();

            var drawingInfos = new CaseInsensitiveDictionary<DsPageDrawingInfo>();

            if (_allDsPagesCache is not null)
            {
                foreach (DsPageDrawing dsPageDrawing in _allDsPagesCache.Values)
                {
                    var drawingInfo = (DsPageDrawingInfo) dsPageDrawing.GetDrawingInfo();
                    drawingInfos.Add(drawingInfo.Name, drawingInfo);
                }
            }
            else
            {                
                foreach (FileInfo fi in new DirectoryInfo(DsPagesDirectoryFullName!).GetFiles(@"*" + DsProject.DsPageFileExtension, SearchOption.TopDirectoryOnly))
                {
                    var drawingInfo = await ReadDrawingInfoAsync(fi.FullName, false, errorMessages) as DsPageDrawingInfo;
                    if (drawingInfo is not null) drawingInfos.Add(drawingInfo.Name, drawingInfo);
                }
            }

            return drawingInfos;
        }

        public async Task<CaseInsensitiveDictionary<DsShapeDrawingInfo>> GetAllComplexDsShapesDrawingInfosAsync(
            List<string>? errorMessages = null)
        {
            if (!IsInitialized) return new CaseInsensitiveDictionary<DsShapeDrawingInfo>();

            if (_allDsShapeDrawingInfos is null)
            {
                _allDsShapeDrawingInfos = new CaseInsensitiveDictionary<DsShapeDrawingInfo>();

                var dsShapesDirectoryInfo = new DirectoryInfo(DsShapesDirectoryFullName);
                foreach (FileInfo fi in dsShapesDirectoryInfo.GetFiles(@"*" + DsShapeFileExtension,
                        SearchOption.TopDirectoryOnly))
                {
                    var drawingInfo = await ReadDrawingInfoAsync(fi.FullName, false, errorMessages) as DsShapeDrawingInfo;
                    if (drawingInfo is not null) _allDsShapeDrawingInfos.Add(drawingInfo.Name, drawingInfo);
                }
            }

            return _allDsShapeDrawingInfos;
        }

        #endregion        

        #region private functions

        private static DateTime GetExeBuildDateTimeUtc()
        {
            var exeDirectory = AppContext.BaseDirectory;
            var result = DateTime.MinValue;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                try
                {
                    var assemblyFileFullName = AddonsManager.GetAssemblyFileInfo(assembly)?.FullName;
                    if (assemblyFileFullName is not null)
                    {
                        var assemblyDirectory = Path.GetDirectoryName(assemblyFileFullName);
                        if (!StringHelper.CompareIgnoreCase(assemblyDirectory, exeDirectory))
                            continue;
                        var creationTime = File.GetLastWriteTime(assemblyFileFullName);
                        if (creationTime > result) result = creationTime;
                    }
                }
                catch (Exception)
                {
                }

            return result.ToUniversalTime();
        }        

        private async Task InitializeAsync(
            string dsProjectFileFullName, 
            DsProjectModeEnum mode, 
            bool isReadOnly,
            bool autoConvert,
            IFileProvider? fileProvider = null)
        {
            if (IsInitialized) 
                throw new InvalidOperationException();            

            if (OnInitializing is not null) 
                OnInitializing();

            DsProjectFileFullName = dsProjectFileFullName;
            FileProvider = fileProvider;

            if (FileProvider is null)
            {
                // Fix some nework-related issues.
                string localhostNetwork = @"\\localhost";
                if (StringHelper.StartsWithIgnoreCase(DsProjectFileFullName, localhostNetwork))
                    DsProjectFileFullName = @"\\" + Environment.MachineName +
                                             DsProjectFileFullName.Substring(localhostNetwork.Length);
            }

            Name = Path.GetFileNameWithoutExtension(DsProjectFileFullName);

            if (FileProvider is not null)
            {
                CsvDb = new CsvDb(NullLogger<CsvDb>.Instance,
                    LoggersSet.UserFriendlyLogger,
                    CsvDbDirectoryFullName,
                    FileProvider,
                    null);
                await CsvDb.EnsureCsvFilesDataIsLoadedAsync();
            }
            else
            {
                CsvDb = new CsvDb(NullLogger<CsvDb>.Instance,
                    LoggersSet.UserFriendlyLogger,
                    CsvDbDirectoryFullName,
                    null,
                    new WrapperDispatcher(Dispatcher.UIThread));
            }

            ElementIdsMap.Initialize(CsvDb.GetData(DataEngine.ElementIdsMapFileName), CsvDb.GetData(DataEngine.TagsFileName), CsvDb);
            ReverseElementIdsMap.Initialize(CsvDb.GetData(DataEngine.ReverseElementIdsMapFileName), CsvDb.GetData(DataEngine.TagsFileName), CsvDb);
            AlarmMessages_ElementIdsMap.Initialize(CsvDb.GetData(DataEngine.AlarmMessages_ElementIdsMapFileName), CsvDb.GetData(DataEngine.TagsFileName), CsvDb);
            
            foreach (var kvp in CsvDb.GetData(DataEngine.GlobalVariablesFileName))
                if (kvp.Key != @"")
                    GlobalVariables.Add(kvp.Key, kvp.Value.Skip(1).Select(GetVariableValue).ToList());
            
            AddonsManager.ResetAvailableAdditionalAddonsCache();

            Mode = mode;

            if (FileProvider is not null)
            {
                IsReadOnly = true;
                AutoConvert = false;
            }
            else
            {
                IsReadOnly = isReadOnly;
                AutoConvert = autoConvert;                
            }

            DsProjectFileInfoChanged?.Invoke();

            DesiredAdditionalAddonsInfo = new List<GuidAndName>();
            _actuallyUsedAddonsInfo = null;
            DefaultDsPageSize = new Size(800, 600);
            DsPageStretchMode = DsPageStretchMode.Default;
            DsPageHorizontalAlignment = DsPageHorizontalAlignment.Default;
            DsPageVerticalAlignment = DsPageVerticalAlignment.Default;
            DsPageBackground = null;
            TryConvertXamlToDsShapes = true;
            RootWindowProps = new WindowProps();
            _cacheDataEnginesDictionary.Clear();
            DataEngineGuidAndName = new GuidAndName
            {
                Guid = GenericDataEngine.DataEngineGuid
            };
            Desc = "";
            DrawingsStoreMode = DrawingsStoreModeEnum.BinMode;
            DefaultServerAddress = "https://localhost:60060/";
            DefaultSystemNameToConnect = @"PLATFORM";
            OperatorUICultureName = "";

            PlayWindowClassOptionsCollection.Add(new PlayWindowClassOptions());

            _saveFilesToLatestSerializationVersion = null;

            if (Mode == DsProjectModeEnum.VisualDesignMode) Instance.TryToLockDsProjectDirectory();
        }

        private void GlobalUITimerTimerCallback(object? sender, EventArgs e)
        {
            CurrentTimeSeconds = (int) DateTime.Now.TimeOfDay.TotalSeconds;

            GlobalUITimerPhase = GlobalUITimerPhase == 0 ? 1 : 0;
            var globalUITimerEvent = GlobalUITimerEvent;
            if (globalUITimerEvent is not null)
                globalUITimerEvent(_globalUITimerPhase);
        }

        private void DsConstantsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    DsConstant[] addedDsConstants = (e.NewItems ?? throw new InvalidOperationException())
                        .OfType<DsConstant>().ToArray();
                    foreach (DsConstant dsConstant in addedDsConstants) dsConstant.IsDsProjectDsConstant = true;
                    break;
            }
        }

        private async void ReSaveAllDsPageDrawingsUnconditionally()
        {
            var errorMessages = new List<string>();

            foreach (DsPageDrawing dsPageDrawing in AllDsPagesCache.Values.ToArray())
            {
                var drawing = await ReadDrawingAsync(dsPageDrawing.FileFullName, false, true);
                if (drawing is null) continue;
                    await SaveUnconditionallyAsync(drawing, IfFileExistsActions.CreateBackup, false, errorMessages);
            }

            if (errorMessages.Count > 0)
                MessageBoxHelper.ShowWarning(string.Join("\n", errorMessages));
            else
                MessageBoxHelper.ShowInfo(Resources.Done);
        }

        #endregion

        #region private fields        

        private bool? _saveFilesToLatestSerializationVersion;

        private string? _name;

        private string? _desc;

        private readonly DispatcherTimer _globalUITimer;

        private int _globalUITimerPhase;

        private List<GuidAndName>? _desiredAdditionalAddonsInfo;

        private List<GuidAndName>? _actuallyUsedAddonsInfo;

        private readonly DsConstant[] _hiddenDsConstantsCollection = new DsConstant[2];

        private readonly CaseInsensitiveDictionary<ConstantValueViewModel[]> _constantValuesDictionary =
            new();        

        private GuidAndName _dataEngineGuidAndName = new();

        private readonly Dictionary<Guid, DataEngineBase> _cacheDataEnginesDictionary =
            new();

        private DataEngineBase _dataEngine = GenericDataEngine.Instance;

        private WindowProps _rootWindowProps = new();

        private string _operatorUICultureName = @"";

        private bool _isReadOnly;

        #endregion

        public enum DsProjectModeEnum
        {
            Uninitialized,
            VisualDesignMode,
            DesktopPlayMode,
            BrowserPlayMode,
            OtherMode
        }

        public enum IfFileExistsActions
        {
            AskNewFileName,
            CreateBackup,
            CreateBackupAndWarn
        }

        //public class DefaultServerAddress_ItemsSource : IItemsSource
        //{
        //    #region public functions

        //    public ItemCollection GetValues()
        //    {
        //        return new()
        //        {
        //            "https://localhost:60060/"                    
        //        };
        //    }

        //    #endregion
        //}

        //public class DefaultSystemNameToConnect_ItemsSource : IItemsSource
        //{
        //    #region public functions

        //    public ItemCollection GetValues()
        //    {
        //        return new()
        //        {
        //            @"",
        //            @"MODEL",
        //            @"PLATFORM",
        //            @"USO",
        //        };
        //    }

        //    #endregion
        //}

        public enum DrawingsStoreModeEnum
        {
            BinMode = 0,
            XamlMode = 1
        }

        //public class DsProjectDrawingsStoreModeItemsSource : IItemsSource
        //{
        //    #region public functions

        //    public ItemCollection GetValues()
        //    {
        //        var result = new ItemCollection();
        //        result.Add(DrawingsStoreModeEnum.BinMode, Resources.DrawingsStoreBinMode);
        //        result.Add(DrawingsStoreModeEnum.XamlMode, Resources.DrawingsStoreXamlMode);
        //        return result;
        //    }

        //    #endregion
        //}

        //public class DataEngineInfoItemsSource : IItemsSource
        //{
        //    #region public functions

        //    public ItemCollection GetValues()
        //    {
        //        var itemCollection = new ItemCollection();
        //        foreach (DataEngineBase dataEngine in AddonsHelper.GetDataEngines())
        //            itemCollection.Add(new GuidAndName
        //                {
        //                    Guid = dataEngine.Guid
        //                },
        //                dataEngine.NameToDisplay);
        //        return itemCollection;
        //    }

        //    #endregion
        //}
    }
}