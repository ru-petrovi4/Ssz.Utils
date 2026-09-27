using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ssz.DataAccessGrpc.Client;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Common.Passthrough;
using Ssz.Operator.Core;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.ViewModels;
using Ssz.Operator.Core.Design.Views;
using Ssz.Operator.Core.Views;
using Ssz.Utils;
using Ssz.Utils.ConfigurationCrypter.Extensions;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static Ssz.Operator.Core.DsProject;

namespace Ssz.Operator.Core.Design;

public partial class DesignApp : Application
{
    public static IHost Host { get; private set; } = null!;

    public static string EnvironmentName { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        TypeDescriptor.AddAttributes(typeof(FontFamily), new TypeConverterAttribute(typeof(FontFamilyTypeConverter)));
        //TypeDescriptor.AddAttributes(typeof(SolidColorBrush.), new TypeConverterAttribute(typeof(SolidColorBrushTypeConverter)));
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(ColorTypeConverter)));
        //SolidColorBrush.ColorProperty

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _ = StartAsync();

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    ///     In the browser the job progress drives the HTML loading overlay, so its bar covers
    ///     downloading the project files and not just the application itself.
    /// </summary>
    private async Task StartAsync()
    {
        await AppLoadingInterop.InitializeInteropAsync();

        try
        {
            await OnFrameworkInitializationCompleted_Internal(
                OperatingSystem.IsBrowser() ? AppLoadingJobProgress.Instance : NullJobProgress.Instance);
        }
        catch (Exception ex)
        {
            // Nothing observes this task, so without this a startup failure would leave the
            // loading overlay on screen with no explanation. Outside the browser there is no
            // overlay and the interop does nothing, so the failure is logged as well - otherwise
            // the editor would just never open a window and say nothing.
            AppLoadingInterop.ShowErrorSafe(ex.ToString());

            DsProject.LoggersSet.Logger.LogCritical(ex, @"The editor failed to start.");
            Console.Error.WriteLine(ex.ToString());
        }
    }

    private async Task OnFrameworkInitializationCompleted_Internal(IJobProgress jobProgress)
    {
        Options options;
        DsProject.DsProjectModeEnum mode;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            mode = DsProject.DsProjectModeEnum.DesktopDesignMode;

            Host = CreateHostBuilder(desktop.Args ?? []).Build();

            DsDataAccessProvider.ServiceProvider = Host.Services;            

            var logger = Host.Services.GetRequiredService<ILogger<DesignApp>>();
            IConfiguration configuration = Host.Services.GetRequiredService<IConfiguration>();
            _configuration = configuration;
            CultureHelper.InitializeUICulture(configuration, logger);

            logger.LogInformation($"App starting with args: \"{String.Join(" ", desktop.Args ?? [])}\"; Environment: {EnvironmentName}; Working Directory: \"{Directory.GetCurrentDirectory()}\"; Workstation Name: {ConfigurationHelper.GetWorkstationName(configuration)}");

            _ = Host.RunAsync();

#if !TEST_BROWSER_IN_DESKTOP
            options = new Options(configuration);

            if (mode == DsProjectModeEnum.DesktopDesignMode || mode == DsProjectModeEnum.BrowserDesignMode)
            {
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
            }
            else
            {
                DsProject.LoggersSet = new LoggersSet(
                    logger,
                    new UserFriendlyLogger((logLevel, eventId, line) =>
                    {
                        if (logLevel >= LogLevel.Error)
                        {
                            DebugWindow.Instance.AddLine(line);
                        }
                        else
                        {
                            if (DebugWindow.IsWindowExists)
                                DebugWindow.Instance.AddLine(line);
                        }
                    }));
            }
#else
            options = new Options(null);

            // TEMPCODE
            options.CentralServerAddress = @"https://www.pazchek.ru"; // @"https://localhost:60060";
            options.ProjectFile = "CDT.2024.SaratovPCNiDCS/Operator.Data/SARATOV_POLE_Interface/Saratov.dsproject";            

            DsProject.LoggersSet = new LoggersSet(
                    NullLogger.Instance,
                    null);

            if (mode == DsProject.DsProjectModeEnum.DesktopDesignMode)
                mode = DsProject.DsProjectModeEnum.BrowserDesignMode;
            else if (mode == DsProject.DsProjectModeEnum.DesktopPlayMode)
                mode = DsProject.DsProjectModeEnum.BrowserPlayMode;
#endif
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            mode = DsProject.DsProjectModeEnum.BrowserDesignMode;

            options = new Options(null);

            // wwwroot/main.js passes the page address as the application argument. The page is
            // served by the central server, one path segment deeper, so
            // https://www.pazchek.ru/MySite/SszPlay?ProjectFile=Dir/Sub/My.dsproject gives
            // CentralServerAddress https://www.pazchek.ru/MySite and ProjectFile Dir/Sub/My.dsproject.
            string pageAddress = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault() ?? @"";
            if (!Uri.TryCreate(pageAddress, UriKind.Absolute, out Uri? pageUri))
                throw new InvalidOperationException(
                    @"The page address is not passed to the application: " + pageAddress);

            string pagePath = pageUri.AbsolutePath.TrimEnd('/');
            int pageNameIndex = pagePath.LastIndexOf('/');
            options.CentralServerAddress = pageUri.GetLeftPart(UriPartial.Authority) +
                (pageNameIndex > 0 ? pagePath.Substring(0, pageNameIndex) : @"");
            options.ProjectFile = HttpUtility.ParseQueryString(pageUri.Query)[@"ProjectFile"] ?? @"";
            options.Constants = HttpUtility.ParseQueryString(pageUri.Query)[@"Constants"] ?? @"";
            // Unlike a play application, the editor does not need a ProjectFile: without one it
            // opens empty and the project is chosen in the editor itself.

            DsProject.LoggersSet = new LoggersSet(
                    new BrowserConsoleLogger(),
                    null);
        }
        else
        {
            throw new InvalidOperationException();
        }

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
#if !TEST_BROWSER_IN_DESKTOP
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
            // The editor also starts with no project opened, and then there is no directory to judge by.
            isReadOnly = !String.IsNullOrEmpty(dsProjectFileFullName) &&
                !FileSystemHelper.IsDirectoryWritable(Path.GetDirectoryName(dsProjectFileFullName));
#else
            fileProvider = await UpdateFilesCacheAsync(options, jobProgress);
            isReadOnly = true;
#endif
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform2)
        {
            isReadOnly = true;

            if (options.ProjectFile != @"")
            {
                fileProvider = await UpdateFilesCacheAsync(options, jobProgress);

                // options.ProjectFile is a path in the server files store, while the file provider is
                // rooted at the project directory itself: inside it the project is addressed by name
                // alone. Passing the whole path here makes the provider look for the project
                // directory inside itself and find nothing.
                dsProjectFileFullName = options.ProjectFile.Substring(options.ProjectFile.LastIndexOf('/') + 1);
            }
            else
            {
                // The editor opens with no project, and the files of the one chosen later are
                // cached when it is opened.
                fileProvider = null;
            }
        }
        else
        {
            throw new InvalidOperationException();
        }

        // The editor connects to the data server the same way the WPF editor did, so that the
        // shapes of a drawing can show live values while it is being edited.
        await DsDataAccessProvider.StaticInitialize(
            mode,
            // No map yet: the editor connects before a project is opened, exactly as the WPF editor
            // did, and a project that is opened later brings its own.
            null,
            DsProject.Instance.DefaultServerAddress,
            @"Cdt.Operator",
            DsProject.Instance.DefaultSystemNameToConnect,
            new CaseInsensitiveOrderedDictionary<string?>(),
            DispatcherHelper.GetUiDispatcher());

        DesignMainView.CommandLineOptions = new DesignOptions(_configuration)
        {
            ProjectFile = options.ProjectFile,
            AutoConvert = options.AutoConvert
        };

        var designMainView = new DesignMainView();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop3)
        {
            var designMainWindow = new DesignMainWindow();
            designMainWindow.Content = designMainView;
            desktop3.MainWindow = designMainWindow;
            designMainWindow.Show();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = designMainView;
        }

        // The editor is on screen - drop the HTML loading overlay.
        AppLoadingInterop.HideSafe();

        // The project named on the command line is opened by the editor itself, which is what shows
        // the busy indicator while it loads, exactly as the WPF editor did.
        if (!String.IsNullOrEmpty(dsProjectFileFullName))
        {
            AppLoadingInterop.SetStatusSafe(OperatorUIResources.Loading_OpeningProject);

            await designMainView.ReadDsProjectFromBinFileAsync(dsProjectFileFullName);
        }
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
                    DangerousAcceptAnyServerCertificate = false, // needed for Browser security
                },
                DispatcherHelper.GetUiDispatcher());

        int index = options.ProjectFile.LastIndexOf('/');
        string projectDirectoryInvariantPathRelativeToRootDirectory = options.ProjectFile.Substring(0, index);       

        IndexedDBFileProvider fileProvider = await IndexedDBHelper.CreateFileProviderAsync(projectDirectoryInvariantPathRelativeToRootDirectory);

        AppLoadingInterop.SetStatusSafe(OperatorUIResources.Loading_ConnectingToServer);

        // Browser WASM is single threaded: Task.Run() stays on the UI thread, so a blocking
        // WaitOne() freezes the JS event loop and the connection it waits for never happens.
        while (!utilityDsDataAccessProvider.IsConnectedEventWaitHandle.WaitOne(0))
            await Task.Delay(100);

        AppLoadingInterop.SetStatusSafe(OperatorUIResources.Loading_GettingProjectFilesList);

        var request = new GetDirectoryInfoRequest
        {
            InvariantPathRelativeToRootDirectory = projectDirectoryInvariantPathRelativeToRootDirectory,
            FilesAndDirectoriesIncludeLevel = Int32.MaxValue
        };
        var returnData = await utilityDsDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
            SerializationHelper.GetOwnedData(request));
        DsFilesStoreDirectory? serverProjectDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData,
            () => new DsFilesStoreDirectory());

        // GetDirectoryInfo creates the directory on the server when it is missing and answers with
        // an empty one, so a wrong ProjectFile looks like a project with no files: the bar would
        // reach 100% and the application would then hang on a project file that is not there.
        string projectFileName = options.ProjectFile.Substring(index + 1);
        if (!serverProjectDsFilesStoreDirectory.DsFilesStoreFilesCollection.Any(
                f => StringHelper.CompareIgnoreCase(f.Name, projectFileName)))
            throw new InvalidOperationException(
                OperatorUIResources.Loading_ProjectNotFound + @": " + options.ProjectFile);

        JobProgressInfo jobProgressInfo = new(jobProgress, serverProjectDsFilesStoreDirectory.GetFilesCount());

        AppLoadingInterop.SetStatusSafe(OperatorUIResources.Loading_DownloadingProjectFiles);

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
                EnvironmentName = ConfigurationHelper.GetEnvironmentName(hostingContext.HostingEnvironment);

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

    private static IConfiguration? _configuration;

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

            // Designer only
            ToolkitOperation = ConfigurationHelper.GetValue<string>(configuration, "ToolkitOperation", @"");
            ToolkitOperationsSilent = ConfigurationHelper.GetValue<bool>(configuration, "ToolkitOperationsSilent", false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Path to project file
        /// </summary>
        public string ProjectFile { get; set; }

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

        public string ToolkitOperation { get; set; }

        public bool ToolkitOperationsSilent { get; set; }

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