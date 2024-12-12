using System;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Ssz.Operator.Core.Utils; 
using Ssz.Utils; 
using Ssz.Utils.Wpf.WpfMessageBox;
using Ssz.Operator.Core;
using Ssz.Utils.Wpf;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;
using Microsoft.Extensions.DependencyInjection;
using Ssz.Operator.Core.DataAccess;

namespace Ssz.Operator.Design
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        #region construction and destruction

        public App()
        {
            InitializeComponent();     

            DispatcherUnhandledException += OnUnhandledException;
        }

        #endregion

        #region public functions

        public static IHost Host = null!;

        public static Options CommandLineOptions { get; private set; } = null!;

        /// <summary>
        ///     A flag to indicate if Ssz.Operator.Play is licensed
        /// </summary>
        private bool _hasSszOperatorLicense { get; set; }

        /// <summary>
        ///     A flag to indicate if we have a license token which will allow the user to make
        ///     engineering changes to the Ssz.Operator.Play dsProject
        /// </summary>
        public static bool HasMaintenanceLicense { get; private set; }

        #endregion

        #region private functions

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                { "-s", "SolutionFile" },
                { "-a", "AutoConvert" },
                { "-o", "Options" },               
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
                    )
                //.ConfigureServices((hostContext, services) =>
                //{
                //    services.AddSingleton<InstructorViewModel>();
                //    services.AddSingleton<GrpcDataAccessProvider>();
                //})
                ;
        }

        private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            DsProject.LoggersSet.Logger.LogCritical(e.Exception, @"App on UnhandledException");

            MessageBoxHelper.ShowError(Design.Properties.Resources.AppUnhandledExceptionMessage);

            e.Handled = true;
        }

        private async void AppOnStartup(object sender, StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Host = CreateHostBuilder(e.Args).Build();

            var hostRunTask = Host.RunAsync();
            
            DsProject.LoggersSet = new LoggersSet(
                Host.Services.GetRequiredService<ILogger<App>>(),
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

            DsProject.LoggersSet.Logger.LogDebug("App starting with args: " + String.Join(" ", e.Args));

            IConfiguration configuration = Host.Services.GetRequiredService<IConfiguration>();            

            #region ParseArguments

            var options = new Options(configuration);            
            CommandLineOptions = options;
            DsProject.Instance.AddonsCommandLineOptions = NameValueCollectionHelper.Parse(options.Options_);

            #endregion

            #region CheckLicense

#if NO_LICENSE_CHECK
            //NO_LICENSE_CHECK builds don't require licensing
            _hasSszOperatorLicense = true;
            HasMaintenanceLicense = true;
#else
            DataServer.Content.Connect("USO", null);
            _hasSszOperatorLicense = DataServer.Content.Vector.Add(SszOperatorUlmName);
            HasMaintenanceLicense = DataServer.Content.Vector.Add(MaintenanceUlmName);
#endif

            if (!_hasSszOperatorLicense)
            {
                WpfMessageBox.Show(Design.Properties.Resources.NoFVLicense + "\n\n" +
                                Design.Properties.Resources.AMSSupportInfo + "\n\n" +
                                Design.Properties.Resources.OkToExit,
                    Core.Properties.Resources.ErrorMessageBoxCaption,
                    WpfMessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-3); // Exit code No License
                return;
            }

            if (!HasMaintenanceLicense)
            {
                if (WpfMessageBoxResult.No ==
                    WpfMessageBox.Show(Design.Properties.Resources.NoMaintLicense,
                        Core.Properties.Resources.QuestionMessageBoxCaption,
                        WpfMessageBoxButton.YesNo,
                        MessageBoxImage.Question))
                {
                    Shutdown(-4); // Exit code no Maint Lic
                    return;
                }
            }

            #endregion

            var dispatcherWrapper = new WrapperDispatcher(Dispatcher);
            await DsDataAccessProvider.StaticInitialize(               
                null,                
                DsProject.Instance.DefaultServerAddress,
                @"Ssz.Operator",
                DsProject.Instance.DefaultSystemNameToConnect,
                new CaseInsensitiveDictionary<string?>(),
                dispatcherWrapper);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            CultureHelper.InitializeUICulture(configuration, DsProject.LoggersSet.Logger);

            MainWindow = new MainWindow();            
            MainWindow.Show();
        }

        private void DataAccessProviderOnInitiateConnectionException(Exception obj)
        {            
        }

        private void AppOnExit(object sender, ExitEventArgs e)
        {
#if !NO_LICENSE_CHECK

            if (_hasSszOperatorLicense)
            {
                DataServer.Content.Vector.Remove(SszOperatorUlmName);
                _hasSszOperatorLicense = false;
            }
            if (HasMaintenanceLicense)
            {
                DataServer.Content.Vector.Remove(MaintenanceUlmName);
                HasMaintenanceLicense = false;
            }
            DataServer.Content.Disconnect();
#endif
        }

        #endregion

        #region private fields

        /// <summary>The name of the Ssz.Operator.Play license token that we are attempting to consume</summary>
        private const string SszOperatorUlmName = "SszOperator";

        /// <summary>Then name of the Maintenance license token that we are attempting to consume</summary>
        private const string MaintenanceUlmName = "Maintenance";

        #endregion

        public sealed class Options
        {
            #region construction and destruction

            public Options(IConfiguration configuration)
            {
                DsProjectFile = ConfigurationHelper.GetValue<string>(configuration, "SolutionFile", @"");
                AutoConvert = ConfigurationHelper.GetValue<bool>(configuration, "AutoConvert", false);
                ToolkitOperation = ConfigurationHelper.GetValue<string>(configuration, "ToolkitOperation", @"");
                ToolkitOperationsSilent = ConfigurationHelper.GetValue<bool>(configuration, "ToolkitOperationsSilent", false);
                Options_ = ConfigurationHelper.GetValue<string>(configuration, "Options", @"");                
            }

            #endregion

            #region public functions

            public string DsProjectFile { get; set; }
            
            public bool AutoConvert { get; set; }
            
            public string ToolkitOperation { get; set; }
            
            public bool ToolkitOperationsSilent { get; set; }
            
            public string Options_ { get; set; }

            #endregion
        }
    }
}