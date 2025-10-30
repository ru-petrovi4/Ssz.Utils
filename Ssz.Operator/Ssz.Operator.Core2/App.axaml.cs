using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Ssz.Operator.Play.ViewModels;
using Ssz.Operator.Play.Views;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Ssz.Operator.Core;
using Ssz.Utils.Logging;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Ssz.Utils.ConfigurationCrypter.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Ssz.Utils;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Commands;
using Avalonia.Platform.Storage;
using System.IO;
using Ssz.Operator.Core.Utils;
using Avalonia.Threading;
using System.Threading.Tasks;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils.DataAccess;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Common.Passthrough;
using Ssz.Utils.Serialization;
using Microsoft.Extensions.FileProviders;
using System.ComponentModel;
using Avalonia.Media;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Platform;

namespace Ssz.Operator.Play;

public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        TypeDescriptor.AddAttributes(typeof(FontFamily), new TypeConverterAttribute(typeof(FontFamilyTypeConverter)));
        //TypeDescriptor.AddAttributes(typeof(SolidColorBrush.), new TypeConverterAttribute(typeof(SolidColorBrushTypeConverter)));
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(ColorTypeConverter)));
        //SolidColorBrush.ColorProperty
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _ = OnFrameworkInitializationCompleted2(NullJobProgress.Instance);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var mainView = new MainView
            {
                DataContext = new MainViewModel()
            };                   

            singleViewPlatform.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public async Task OnFrameworkInitializationCompleted2(IJobProgress jobProgress)
    {
        Options options;
        DsProject.DsProjectModeEnum dsProjectModeEnum;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            Host = CreateHostBuilder(desktop.Args ?? []).Build();

            DsDataAccessProvider.ServiceProvider = Host.Services;

            _ = Host.RunAsync();

            var logger = Host.Services.GetRequiredService<ILogger<App>>();

            logger.LogDebug("App starting with args: " + String.Join(" ", desktop.Args ?? []));                   

            IConfiguration configuration = Host.Services.GetRequiredService<IConfiguration>();
            CultureHelper.InitializeUICulture(configuration, logger);

            options = new Options(configuration);

            if (options.Review)
                DsProject.LoggersSet = new LoggersSet(
                    logger,
                    new UserFriendlyLogger((logLevel, eventId, line) =>
                    {
                        if (logLevel >= LogLevel.Warning)
                        {
                            DebugWindow.Instance.AddLine(line);
                        }
                        else
                        {
                            if (DebugWindow.IsWindowExists)
                                DebugWindow.Instance.AddLine(line);
                        }
                    }));
            else
                DsProject.LoggersSet = new LoggersSet(
                    logger,
                    new UserFriendlyLogger((logLevel, eventId, line) =>
                    {
                        if (DebugWindow.IsWindowExists)
                            DebugWindow.Instance.AddLine(line);
                    }));

            dsProjectModeEnum = DsProject.DsProjectModeEnum.DesktopPlayMode;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            options = new Options(null);

            // TEMPCODE
            options.CentralServerAddress = @"https://www.v3code.ru";
            options.ProjectDirectoryInvariantPathRelativeToRootDirectory = "CDT.2024.SaratovPCNiDCS/Operator.Data/SARATOV_POLE_Interface";
            options.ProjectFile = @"Saratov.dsProject";

            DsProject.LoggersSet = new LoggersSet(
                    NullLogger.Instance,
                    null);

            dsProjectModeEnum = DsProject.DsProjectModeEnum.BrowserPlayMode;
        }
        else
        {
            throw new InvalidOperationException();
        }

        #region LoadDsProject

        //No need to check for FV licensing if we are being launched from the Editor since checks were already made there
        if (!options.Review && !ConsumeSszOperatorLicense())
        {
            //WpfMessageBox.Show(Play.Properties.Resources.NoFVLicense + "\n\n" + Play.Properties.Resources.OkToExit,
            //    Play.Properties.Resources.NoFVLicenseTitle, WpfMessageBoxButton.OK, MessageBoxImage.Error);
            //DsProject.LoggersSet.Logger.LogDebug(Play.Properties.Resources.NoFVLicenseTitle + ". Exited Application.");
            SafeShutdown();
            return;
        }
        
        string dsProjectFileFullName = options.ProjectFile;
        bool isReadOnly;
        IFileProvider? fileProvider;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop2)
        {
            //if (String.IsNullOrEmpty(dsProjectFileFullName))
            //{
            //    var files = await desktop2.MainWindow?.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            //    {
            //        Title = "Выберите файлы",
            //        AllowMultiple = false,
            //        FileTypeFilter = new[]
            //        {
            //            new FilePickerFileType("Project")
            //            {
            //                Patterns = new[] { "*" + DsProject.DsProjectFileExtension }
            //            }
            //        }
            //    });
            //    if (files.Count > 0)
            //    {
            //        dsProjectFileFullName = await files[0].Path;
            //        // Теперь у вас есть поток файла
            //    }
            //}            
            fileProvider = null;
            isReadOnly = !FileSystemHelper.IsDirectoryWritable(Path.GetDirectoryName(dsProjectFileFullName));
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform2)
        {
            fileProvider = await UpdateFilesCacheAsync(options, jobProgress);
            isReadOnly = true;
        }
        else
        {
            throw new InvalidOperationException();
        }

        if (String.IsNullOrEmpty(dsProjectFileFullName))
        {
            SafeShutdown();
        }
        
        bool failed = await DsProject.ReadDsProjectFromBinFileAsync(
            dsProjectFileFullName, 
            dsProjectModeEnum,                    
            isReadOnly, 
            options.AutoConvert, 
            options.Constants,
            null,
            fileProvider);
        if (!failed)
        {
            failed = !ConsumeDcsConsoleLicenseIfRequired();
            if (failed)
            {
                //WpfMessageBox.Show(Play.Properties.Resources.NoDcsConsoleEmulationLicense + "\n\n" + Play.Properties.Resources.OkToExit,
                //    Play.Properties.Resources.NoFVLicenseTitle, WpfMessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        if (failed)
        {
            SafeShutdown();
            return;
        }

        #endregion        

        if (!String.IsNullOrEmpty(options.UserTagsFile))
        {
            // TODO
        }
        DsProject.Instance.Review = options.Review;
        DsProject.Instance.NoSound = options.NoSound;
        DsProject.Instance.AddonsCommandLineOptions = NameValueCollectionHelper.Parse(options.Options_);

        #region StartDataAccessProvider        

        string serverAddress = GetServerAddress(options,
            DsProject.Instance.DefaultServerAddress);
        string systemNameToConnect = GetSystemNameToConnect(options,
            DsProject.Instance.DefaultSystemNameToConnect);
        string operatorSessionId;
        if (options.OperatorSessionId != @"") operatorSessionId = options.OperatorSessionId;
        else operatorSessionId = Guid.NewGuid().ToString();
        
        CaseInsensitiveOrderedDictionary<string?> contextParams;
        if (!String.IsNullOrEmpty(options.ContextParams))
        {
            contextParams = NameValueCollectionHelper.Parse(options.ContextParams);
        }
        else
        {
            contextParams = new CaseInsensitiveOrderedDictionary<string?>();
            contextParams[@"OperatorSessionId"] = operatorSessionId;
        }        
        await DsDataAccessProvider.StaticInitialize(
            dsProjectModeEnum,
            DsProject.Instance.ElementIdsMap,
            serverAddress,
            @"Ssz.Operator",
            systemNameToConnect,
            contextParams,
            DispatcherHelper.GetUiDispatcher()
        );

        DsDataAccessProvider.Instance.PropertyChanged += DataAccessProviderOnPropertyChanged;
        DataAccessProviderOnConnectedOrDisconnected();
        
        #endregion

        PlayDsProjectView.Initialize();

        foreach (AddonBase addon in AddonsManager.AddonsCollection.ObservableCollection)
        {
            addon.InitializeInPlayMode();
        }        

        #region Conditional Commands

        if (DsProject.Instance.ConditionalDsCommandsCollection.Count > 0)
        {
            _conditionalDsCommandViewsCollection = new List<DsCommandView>();

            foreach (DsCommand dsCommand in DsProject.Instance.ConditionalDsCommandsCollection)
            {
                var genericContainer = new GenericContainer();
                genericContainer.ParentItem = DsProject.Instance;
                dsCommand.ParentItem = genericContainer; // For using LastActiveRootPlayWindow as parent window.
                var dsCommandView = new DsCommandView(null,
                    dsCommand,
                    new DataValueViewModel(null, false));
                _conditionalDsCommandViewsCollection.Add(dsCommandView);

                if (dsCommandView.IsEnabled) 
                    dsCommandView.DoCommand();
                dsCommandView.PropertyChanged += (sender, e) =>
                {
                    if (e.Property == DsCommandView.IsEnabledProperty)
                        DsCommandViewOnIsEnabledChanged(sender, e);
                };                
            }
        }

        #endregion

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopFinal)
        {
            WindowsManager.Instance.Initialize(options.StartPageFile, desktopFinal);            
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatformFinal)
        {
            WindowsManager.Instance.Initialize(options.StartPageFile, singleViewPlatformFinal);            
        }

        // TEMPCODE
        //base.OnFrameworkInitializationCompleted();        
        //var x = AvaloniaRuntimeXamlLoader.Parse("<SolidColorBrush xmlns=\"https://github.com/avaloniaui\" Color=\"#FFFFFFFF\"/>");
    }    

    public async void SafeShutdown()
    {
        ReturnConsumedLicenses();

        #region Conditional Commands

        if (_conditionalDsCommandViewsCollection != null)
        {
            foreach (DsCommandView dsCommandView in _conditionalDsCommandViewsCollection)
            {                              
                dsCommandView.Dispose();
            }
            _conditionalDsCommandViewsCollection.Clear();
            _conditionalDsCommandViewsCollection = null;
        }

        #endregion

        WindowsManager.Instance.Close();

        foreach (AddonBase addon in AddonsManager.AddonsCollection.ObservableCollection)
        {
            addon.CloseInPlayMode();
        }

        await DsDataAccessProvider.StaticDisposeAsync();

        DsProject.Instance.Close();

        if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.DesktopPlayMode)
            await Host.StopAsync();

        //Current.Shutdown(0);
    }    

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private bool ConsumeSszOperatorLicense()
    {
        return true;
//#if NO_LICENSE_CHECK
//            return true;
//#else
//        if (!_hasSszOperatorLicense)   //Don't need to grab a second license
//        {
//            //if (CanAdd())
//            {
//                // _hasSszOperatorLicense = Add;
//            }
//        }
//        return _hasSszOperatorLicense;
//#endif
    }

    private bool ConsumeDcsConsoleLicenseIfRequired()
    {
        return true;
    }

    private void ReturnConsumedLicenses()
    {
        
    }

    private async Task<IFileProvider> UpdateFilesCacheAsync(Options options, IJobProgress jobProgress)
    {
        var utilityDsDataAccessProvider = new DsDataAccessProvider(NullLogger<GrpcDataAccessProvider>.Instance);
        utilityDsDataAccessProvider.Initialize(
                null,
                options.CentralServerAddress,
                @"Ssz.Operator",
                Environment.MachineName,
                @"", // Utility context
                new CaseInsensitiveOrderedDictionary<string?>
                {
                },
                new DataAccessProviderOptions
                {
                    DangerousAcceptAnyServerCertificate = false,
                },
                DispatcherHelper.GetUiDispatcher());

        string projectDirectoryInvariantPathRelativeToRootDirectory = options.ProjectDirectoryInvariantPathRelativeToRootDirectory;

        IndexedDBFileProvider fileProvider = await IndexedDBHelper.CreateFileProviderAsync(projectDirectoryInvariantPathRelativeToRootDirectory);

        await Task.Run(() => utilityDsDataAccessProvider.IsConnectedEventWaitHandle.WaitOne());

        var request = new GetDirectoryInfoRequest
        {
            InvariantPathRelativeToRootDirectory = projectDirectoryInvariantPathRelativeToRootDirectory,
            FilesAndDirectoriesIncludeLevel = Int32.MaxValue
        };
        var returnData = await utilityDsDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
            SerializationHelper.GetOwnedData(request));
        DsFilesStoreDirectory? serverProjectDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData,
            () => new DsFilesStoreDirectory());

        JobProgressInfo jobProgressInfo = new(jobProgress, serverProjectDsFilesStoreDirectory.GetFilesCount());        

        await IndexedDBHelper.DownloadFilesStoreDirectoryAsync(
            fileProvider.RootIndexedDBDirectory,            
            serverProjectDsFilesStoreDirectory,
            utilityDsDataAccessProvider,
            projectDirectoryInvariantPathRelativeToRootDirectory,
            @"",
            jobProgressInfo
            );

        await jobProgress.SetJobProgressAsync(100, null, null, StatusCodes.Good);

        //await Task.Delay(0);

        return fileProvider;
    }

    private void DsCommandViewOnIsEnabledChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is bool && !(bool)e.OldValue && e.NewValue is bool && (bool)e.NewValue)
        {
            (sender as DsCommandView)?.DoCommand();
        }
    }

    private void DataAccessProviderOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        switch (args.PropertyName)
        {
            case @"IsConnected":
                DataAccessProviderOnConnectedOrDisconnected();
                break;
        }
    }

    private void DataAccessProviderOnConnectedOrDisconnected()
    {
        //string hostName;
        //if (DsDataAccessProvider.Instance.IsInitialized && !String.IsNullOrWhiteSpace(DsDataAccessProvider.Instance.ServerAddress))
        //{
        //    hostName = new Uri(DsDataAccessProvider.Instance.ServerAddress).Host;
        //}
        //else
        //{
        //    hostName = "";
        //}

        //if (_trayNotifyIcon != null)
        //    if (DsDataAccessProvider.Instance.IsConnected)
        //    {
        //        _trayNotifyIcon.Icon = Play.Properties.Resources.Connected;
        //        _trayNotifyIcon.Text = Play.Properties.Resources.DataAccessProviderConnected + " " + hostName;
        //    }
        //    else
        //    {
        //        _trayNotifyIcon.Icon = Play.Properties.Resources.Disconnected;
        //        _trayNotifyIcon.Text = Play.Properties.Resources.DataAccessProviderDisconnected + " " + hostName;
        //    }
    }

    private static string GetServerAddress(Options options, String defaultUrl)
    {
        if (options.NoConnect) 
            return @"";

        string url = defaultUrl;

        if (!String.IsNullOrEmpty(options.CentralServerAddress))
            url = options.CentralServerAddress;

        if (!String.IsNullOrEmpty(options.CentralServerHost) && !String.IsNullOrEmpty(url))
        {
            var uri = new UriBuilder(url);
            uri.Host = options.CentralServerHost;
            url = uri.Uri.OriginalString;
        }

        return url;
    }

    private static string GetSystemNameToConnect(Options options, string defaultSystemNameToConnect)
    {
        string systemNameToConnect = defaultSystemNameToConnect;

        if (!String.IsNullOrEmpty(options.CentralServerSystemName))
            systemNameToConnect = options.CentralServerSystemName;

        return systemNameToConnect;
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        var switchMappings = new Dictionary<string, string>()
            {
                { @"-p", @"ProjectFile" },
                { @"-start", @"StartPageFile" },
                { @"-e", @"EnhanceTouchscreen" },
                { @"-a", @"AutoConvert" },
                { @"-r", @"Review" },
                { @"-ns", @"NoSound" },
                { @"-nc", @"NoConnect" },
                { @"-address", @"CentralServerAddress" },
                { @"-h", @"CentralServerHost" },
                { @"-sn", @"CentralServerSystemName" },
                { @"-o", @"Options" },
                { @"-c", @"Constants" },
            };

        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();

                config.AddEncryptedAppSettings(hostingContext.HostingEnvironment, crypter =>
                {
                    crypter.CertificatePath = @"appsettings.pfx";                    
                });

                config.AddCommandLine(args, switchMappings);
            })
            .ConfigureLogging(
                builder =>
                    builder.ClearProviders()
                        .AddSszLogger()
                );
    }

    #region private fields

    private List<DsCommandView>? _conditionalDsCommandViewsCollection;

    #endregion    

    public class Options
    {
        #region construction and destruction

        public Options(IConfiguration? configuration)
        {
            ProjectFile = ConfigurationHelper.GetValue<string>(configuration, @"ProjectFile", @"");
            AutoConvert = ConfigurationHelper.GetValue<bool>(configuration, @"AutoConvert", false);
            EnhanceTouchscreen = ConfigurationHelper.GetValue<bool>(configuration, @"EnhanceTouchscreen", false);
            Review = ConfigurationHelper.GetValue<bool>(configuration, @"Review", false);
            NoSound = ConfigurationHelper.GetValue<bool>(configuration, @"NoSound", false);
            NoConnect = ConfigurationHelper.GetValue<bool>(configuration, @"NoConnect", false);
            StartPageFile = ConfigurationHelper.GetValue<string>(configuration, @"StartPageFile", @"");
            CentralServerAddress = ConfigurationHelper.GetValue<string>(configuration, @"CentralServerAddress", @"");
            CentralServerHost = ConfigurationHelper.GetValue<string>(configuration, @"CentralServerHost", @"");
            CentralServerSystemName = ConfigurationHelper.GetValue<string>(configuration, @"CentralServerSystemName", @"");
            OperatorSessionId = ConfigurationHelper.GetValue<string>(configuration, @"OperatorSessionId", @"");
            UserTagsFile = ConfigurationHelper.GetValue<string>(configuration, @"UserTagsFile", @"");
            Options_ = ConfigurationHelper.GetValue<string>(configuration, @"Options", @"");
            Constants = ConfigurationHelper.GetValue<string>(configuration, @"Constants", @"");
            ContextParams = ConfigurationHelper.GetValue<string>(configuration, @"ContextParams", @"");
        }

        #endregion

        #region public functions

        public string ProjectFile { get; set; }

        public string ProjectDirectoryInvariantPathRelativeToRootDirectory { get; set; }

        public string StartPageFile { get; set; }

        public bool EnhanceTouchscreen { get; set; }

        public bool AutoConvert { get; set; }

        /// <summary>
        ///     Launched from Designer
        /// </summary>
        public bool Review { get; set; }

        public bool NoSound { get; set; }

        public bool NoConnect { get; set; }

        public string CentralServerAddress { get; set; }

        public string CentralServerHost { get; set; }

        public string CentralServerSystemName { get; set; }

        public string OperatorSessionId { get; set; }

        public string UserTagsFile { get; set; }

        public string Options_ { get; set; }

        public string Constants { get; set; }

        public string ContextParams { get; set; }

        #endregion
    }
}


//string dataSourceString = @"aaa";
//    CompiledBindingExtension bindingExtension1 =
//        new CompiledBindingExtension(new CompiledBindingPathBuilder(1).Property(
//            new ClrPropertyInfo("Item",
//                obj0 => ((DataValueViewModel)obj0)[dataSourceString],
//                (obj0, obj1) => ((DataValueViewModel)obj0)[dataSourceString] = obj1,
//                typeof(object)),
//            new Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor>(PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)).Build());
//            mainView.TestTextBlock.Bind(TextBlock.TextProperty, bindingExtension1);     