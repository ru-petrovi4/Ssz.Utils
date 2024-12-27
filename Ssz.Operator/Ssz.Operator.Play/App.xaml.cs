using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Ssz.Operator.Core.Utils; 
using Ssz.Utils; 
using Ssz.Operator.Core.Utils.WinApi;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Utils.Wpf.WpfMessageBox;
using Ssz.Operator.Core;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Addons;
using Microsoft.Win32;
using Ssz.Utils.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Ssz.Operator.Play
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region construction and destruction

        public App()
        {
            if (!StringHelper.ContainsIgnoreCase(Environment.CommandLine, "--NoSplash"))
            {
                /*
                _splashScreen = new SplashScreen("images/logo.png");
                _splashScreen.Show(false);*/
            }
        }

        #endregion

        #region public functions

        public static IHost Host { get; private set; } = null!;

        public async void SafeShutdown()
        {            
            // Explicitly calls DataAccessProvider.Dispose for correct diposing.
            // Shutdown(0) occurs too early, otherwise.
            if (_sszOperator32Process != null && !_sszOperator32Process.HasExited)
            {
                ProcessHelper.CloseAllWindows(_sszOperator32Process);
                _sszOperator32Process = null;
            }

            if (_disableTouchConversionToMouse != null)
            {
                _disableTouchConversionToMouse.Dispose();
            }

            ReturnConsumedLicenses();
#if !NO_LICENSE_CHECK
            // Disconnect
#endif      

            #region Conditional Commands

            if (_conditionalDsCommandViewsCollection != null)
            {
                foreach (DsCommandView dsCommandView in _conditionalDsCommandViewsCollection)
                {
                    dsCommandView.IsEnabledChanged -= DsCommandViewOnIsEnabledChanged;
                    dsCommandView.Dispose();
                }
                _conditionalDsCommandViewsCollection.Clear();
                _conditionalDsCommandViewsCollection = null;
            }

            #endregion

            WindowsManager.Instance.Close();

            foreach (AddonBase addon in AddonsHelper.AddonsCollection.ObservableCollection)
            {
                addon.CloseInPlayMode();
            }

            await DsDataAccessProvider.StaticDisposeAsync();

            DsProject.Instance.Close();

            await Host.StopAsync();

            Current.Shutdown(0);
        }

        #endregion

        #region private functions

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

                    IHostEnvironment env = hostingContext.HostingEnvironment;
                    config
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);

                    config.AddCommandLine(args, switchMappings);
                })
                .ConfigureLogging(
                    builder =>
                        builder.ClearProviders()
                            .AddSszLogger()
                    );
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            DsProject.LoggersSet.Logger.LogCritical(e.Exception, @"App on DispatcherUnhandledException");

#if DEBUG
            MessageBoxHelper.ShowError(Play.Properties.Resources.AppUnhandledExceptionMessage + "\n\n" + e.Exception.Message);
#endif

            e.Handled = true;
        }

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Host = CreateHostBuilder(e.Args).Build();

            var hostRunTask = Host.RunAsync();

            var logger = Host.Services.GetRequiredService<ILogger<App>>();

            logger.LogDebug("App starting with args: " + String.Join(" ", e.Args));

            IConfiguration configuration = Host.Services.GetRequiredService<IConfiguration>();
            CultureHelper.InitializeUICulture(configuration, logger);

            var options = new Options(configuration);

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

            #region Enhance Touchscreen mode

            if (options.EnhanceTouchscreen)
            {
                if (_splashScreen != null)
                {
                    _splashScreen.Close(TimeSpan.FromSeconds(2));
                    _splashScreen = null;
                }

                MainWindow = new HiddenMainWindow();
                MainWindow.Title = @"Touchscreen Helper";
                MainWindow.Show();
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                _disableTouchConversionToMouse = new DisableTouchConversionToMouse();

                return;
            }

            #endregion

            #region LoadDsProject

#if !NO_LICENSE_CHECK
            //Connect
#endif
            //No need to check for FV licensing if we are being launched from the Editor since checks were already made there
            if (!options.Review && !ConsumeSszOperatorLicense())
            {
                WpfMessageBox.Show(Play.Properties.Resources.NoFVLicense + "\n\n" + Play.Properties.Resources.OkToExit,
                    Play.Properties.Resources.NoFVLicenseTitle, WpfMessageBoxButton.OK, MessageBoxImage.Error);
                DsProject.LoggersSet.Logger.LogDebug(Play.Properties.Resources.NoFVLicenseTitle + ". Exited Application.");
                Shutdown(-1);
                return;
            }

            string? dsProjectFileFullNameNotFromCommandLine = null;
            string dsProjectFileFullName = options.ProjectFile;            
            if (String.IsNullOrEmpty(dsProjectFileFullName))
            {                
                var dlg = new OpenFileDialog
                {
                    Filter = @"Open file (*" + DsProject.DsProjectFileExtension + ")|*" + DsProject.DsProjectFileExtension + "|All files (*.*)|*.*"
                };
                bool? bResult = dlg.ShowDialog();
                if (bResult != true)
                {
                    Shutdown(-1);
                    return;
                }
                dsProjectFileFullName = dlg.FileName;
                dsProjectFileFullNameNotFromCommandLine = dsProjectFileFullName;
            }

            bool isReadOnly = !FileSystemHelper.IsDirectoryWritable(Path.GetDirectoryName(dsProjectFileFullName));                   
            bool failed = await DsProject.ReadDsProjectFromBinFileAsync(dsProjectFileFullName, DsProject.DsProjectModeEnum.WindowsPlayMode,
                        isReadOnly, options.AutoConvert, options.Constants);
            if (!failed)
            {
                failed = !ConsumeDcsConsoleLicenseIfRequired();
                if (failed)
                {
                    WpfMessageBox.Show(Play.Properties.Resources.NoDcsConsoleEmulationLicense + "\n\n" + Play.Properties.Resources.OkToExit,
                        Play.Properties.Resources.NoFVLicenseTitle, WpfMessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            if (failed)
            {
                Shutdown(-1);
                return;
            }

            #endregion            

            //     Check HmiWeb compatibility mode requirements for specified dsProject and run 32 bit version instead of current.
            //     Waiting termination of 32 bit application and close self process after it
            if (Environment.Is64BitProcess && !AddonsHelper.AddonsCollection.ObservableCollection
                    .All(p => p.Is64BitProcessSupported))
            {
                var fi = new FileInfo(Process.GetCurrentProcess().MainModule?.FileName ?? @"");
                
                string arguments = Environment.CommandLine;
                arguments += " --NoSplash"; //Disable the splash on the x32 that we are launching
                if (!String.IsNullOrWhiteSpace(dsProjectFileFullNameNotFromCommandLine))
                {
                    arguments += @" -p """ + dsProjectFileFullNameNotFromCommandLine + @"""";
                }

                var startInfo = new ProcessStartInfo(fi.DirectoryName + @"\Ssz.Operator.Play 32.exe", arguments);
                try
                {
                    _sszOperator32Process = Process.Start(startInfo)!;
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"Failed to start process");
                }
                if (_sszOperator32Process != null)
                {
                    var thread = new Thread(SszOperator32ProcessWaitForExit);
                    thread.IsBackground = true;
                    thread.Start();

                    //Check to see if we need to drop our licenses so that the FV32 process 
                    //can pick them up.
                    ReturnConsumedLicenses();

                    DsProject.Instance.Close();

                    if (_splashScreen != null)
                    {
                        _splashScreen.Close(TimeSpan.FromSeconds(2));
                        _splashScreen = null;
                    }

                    MainWindow = new HiddenMainWindow();
                    //MainWindow.Title = @"";
                    MainWindow.Show();
                    ShutdownMode = ShutdownMode.OnMainWindowClose;

                    return; // run child process
                }
                else
                {
                    DsProject.LoggersSet.Logger.LogError(@"Failed to start 'Ssz.Operator.Play 32.exe'");
                }
            }
            
            if (!String.IsNullOrEmpty(options.UserTagsFile))
            {
                // TODO
            }                
            DsProject.Instance.Review = options.Review;
            DsProject.Instance.NoSound = options.NoSound;            
            DsProject.Instance.AddonsCommandLineOptions = NameValueCollectionHelper.Parse(options.Options_);

            #region StartDataAccessProvider

            _trayNotifyIcon = new System.Windows.Forms.NotifyIcon();
            _trayNotifyIcon.Visible = true;                      

            string serverAddress = GetServerAddress(options,
                DsProject.Instance.DefaultServerAddress);
            string systemNameToConnect = GetSystemNameToConnect(options,
                DsProject.Instance.DefaultSystemNameToConnect);
            string operatorSessionId;
            if (options.OperatorSessionId != @"") operatorSessionId = options.OperatorSessionId;
            else operatorSessionId = Guid.NewGuid().ToString();

            var dispatcherWrapper = new WrapperDispatcher(Dispatcher);
            CaseInsensitiveDictionary<string?> contextParams;
            if (!String.IsNullOrEmpty(options.ContextParams))
            {
                contextParams = NameValueCollectionHelper.Parse(options.ContextParams);
            }
            else
            {
                contextParams = new CaseInsensitiveDictionary<string?>();
                contextParams[@"OperatorSessionId"] = operatorSessionId;
            }
            DsDataAccessProvider.ServiceProvider = Host.Services;
            await DsDataAccessProvider.StaticInitialize(                
                DsProject.Instance.ElementIdsMap,                
                serverAddress, 
                @"Ssz.Operator", 
                systemNameToConnect, 
                contextParams,
                dispatcherWrapper
            );

            DsDataAccessProvider.Instance.PropertyChanged += DataAccessProviderOnPropertyChanged;
            DataAccessProviderOnConnectedOrDisconnected();

            if (!String.IsNullOrWhiteSpace(serverAddress))
                try
                {
                    DsProject.Instance.DataAccessServerHost = new Uri(serverAddress).Host;                    
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogWarning(ex, "DsProject propertis initialization exception");
                }

            #endregion

            PlayDsProjectView.Initialize();

            foreach (AddonBase addon in AddonsHelper.AddonsCollection.ObservableCollection)
            {
                addon.InitializeInPlayMode();
            }

            WindowsManager.Instance.Initialize(options.StartPageFile);            

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

                    if (dsCommandView.IsEnabled) dsCommandView.DoCommand();
                    dsCommandView.IsEnabledChanged += DsCommandViewOnIsEnabledChanged;
                }
            }

            #endregion            

            if (_splashScreen != null)
            {
                _splashScreen.Close(TimeSpan.FromSeconds(2));
                _splashScreen = null;
            }
        }        

        private void OnExit(object sender, ExitEventArgs e)
        {
            if (_splashScreen != null)
            {
                _splashScreen.Close(TimeSpan.FromSeconds(0));
                _splashScreen = null;
            }

            if (_trayNotifyIcon != null)
            {
                _trayNotifyIcon.Dispose();
                _trayNotifyIcon = null;
            }
        }

        private void SszOperator32ProcessWaitForExit()
        {
            if (_sszOperator32Process != null && !_sszOperator32Process.HasExited)
            {
                _sszOperator32Process.WaitForExit();
                _sszOperator32Process = null;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Shutdown(0);
                }));
            }
        }

        private void DsCommandViewOnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is bool && !(bool)e.OldValue && e.NewValue is bool && (bool)e.NewValue)
            {
                ((DsCommandView)sender).DoCommand();
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
            string hostName;
            if (DsDataAccessProvider.Instance.IsInitialized && !String.IsNullOrWhiteSpace(DsDataAccessProvider.Instance.ServerAddress))
            {
                hostName = new Uri(DsDataAccessProvider.Instance.ServerAddress).Host;
            }
            else
            {
                hostName = "";
            }
            
            if (_trayNotifyIcon != null)
                if (DsDataAccessProvider.Instance.IsConnected)
                {
                    _trayNotifyIcon.Icon = Play.Properties.Resources.Connected;
                    _trayNotifyIcon.Text = Play.Properties.Resources.DataAccessProviderConnected + " " + hostName;
                }
                else
                {
                    _trayNotifyIcon.Icon = Play.Properties.Resources.Disconnected;
                    _trayNotifyIcon.Text = Play.Properties.Resources.DataAccessProviderDisconnected + " " + hostName;
                }
        }

        private static string GetServerAddress(Options appOptions, String defaultUrl)
        {
            if (appOptions.NoConnect) return @"";

            string url = defaultUrl;

            if (!String.IsNullOrEmpty(appOptions.CentralServerAddress))
                url = appOptions.CentralServerAddress;            
            
            if (!String.IsNullOrEmpty(appOptions.CentralServerHost) && !String.IsNullOrEmpty(url))
            {
                var uri = new UriBuilder(url);
                uri.Host = appOptions.CentralServerHost;
                url = uri.Uri.OriginalString;
            }

            return url;
        }

        private static string GetSystemNameToConnect(Options appOptions, string defaultSystemNameToConnect)
        {
            string systemNameToConnect = defaultSystemNameToConnect;

            if (!String.IsNullOrEmpty(appOptions.CentralServerSystemName))
                systemNameToConnect = appOptions.CentralServerSystemName;            

            return systemNameToConnect;
        }

        private bool ConsumeSszOperatorLicense()
        {
#if NO_LICENSE_CHECK
            return true;
#else
            if (!_hasSszOperatorLicense)   //Don't need to grab a second license
            {
                //if (CanAdd())
                {
                    // _hasSszOperatorLicense = Add;
                }
            }
            return _hasSszOperatorLicense;
#endif
        }
        
        private bool ConsumeDcsConsoleLicenseIfRequired()
        {
#if NO_LICENSE_CHECK
            return true;
#else
            if (_hasDcsConsoleEmulationLicense) return true;    //We already have the license

            //Check to see if the DCS Console Emulation Support addon is being used by this dsProject.
            //If so, make sure that we are consuming a license.
            bool requiresDcsConsoleLicense = false;
            foreach (var addon in MefDiscovery.AddonsCollection.ObservableCollection)
            {
                //if (addon.Guid == DcsConsoleEmulationAddonGuid)
                {
                    requiresDcsConsoleLicense = true;
                    break;
                }
            }

            if (requiresDcsConsoleLicense)
            {
                //We don't need to connect to the ULM because we already connected earlier 
                //on when requesting the FV main application license.
                //_hasDcsConsoleEmulationLicense = Add;
            }

            //We are good if we either have the license, or don't need to have the license
            return _hasDcsConsoleEmulationLicense || !requiresDcsConsoleLicense;
#endif
        }
        
        private void ReturnConsumedLicenses()
        {
#if !NO_LICENSE_CHECK
            //Remove our ULM FV license if we have consumed one
            if (_hasSszOperatorLicense)
            {
                // Remove
                _hasSszOperatorLicense = false;
            }

            //Remove our ULM DCS Console Emulation license if we have consumed one
            if (_hasDcsConsoleEmulationLicense)
            {
                // Remove 
                _hasDcsConsoleEmulationLicense = false;
            }
#endif
        }

        #endregion

        #region private fields

        private Process? _sszOperator32Process;
        
        private static DisableTouchConversionToMouse? _disableTouchConversionToMouse;

        private SplashScreen? _splashScreen;
        private System.Windows.Forms.NotifyIcon? _trayNotifyIcon;

        private List<DsCommandView>? _conditionalDsCommandViewsCollection;
        
#if !NO_LICENSE_CHECK
        private bool _hasDcsConsoleEmulationLicense = false;
        private bool _hasSszOperatorLicense = false;
#endif
        #endregion

        public class Options
        {
            #region construction and destruction

            public Options(IConfiguration configuration)
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
}