using Ssz.Utils.ConfigurationCrypter;
using Ssz.Utils.ConfigurationCrypter.CertificateLoaders;
using Ssz.Utils.ConfigurationCrypter.ConfigurationCrypters;
using Ssz.Utils.ConfigurationCrypter.ConfigurationCrypters.Yaml;
using Ssz.Utils.ConfigurationCrypter.Crypters;
using Ssz.Utils.ConfigurationCrypter.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ssz.Utils.Logging;
using Microsoft.AspNetCore.Hosting;
using Ssz.DataAccessGrpc.ServerBase;
using Microsoft.Extensions.Configuration;
using Ssz.Utils;
using System.IO;
using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.Text;
using Ssz.Dcs.CentralServer.Common.Helpers;

namespace Ssz.Dcs.CentralServer
{
    public class Program
    {
        #region public functions 

        public static IHost Host { get; private set; } = null!;

        #endregion

        //#region public functions

        //public static void SafeShutdown()
        //{
        //    var task = Host.StopAsync();
        //}

        //#endregion

        #region private functions

        private static void Main(string[] args)
        {
            if (WindowsServiceHelpers.IsWindowsService())
            {
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            }

            Host = CreateHostBuilder(args).Build();

            var logger = Host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("App starting with args: " + String.Join(" ", args));

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            IConfiguration configuration = Host.Services.GetRequiredService<IConfiguration>();
            CultureHelper.InitializeUICulture(configuration, logger);

            IHostEnvironment hostEnvironment = Host.Services.GetRequiredService<IHostEnvironment>();
            logger.LogDebug($"hostEnvironment.EnvironmentName: {hostEnvironment.EnvironmentName}");

            try
            {
                ILoggersSet loggersSet = new LoggersSet(new UserFriendlyLogger(s => Console.WriteLine(s)), null);
                if (args.Any(a => a == @"-e"))
                    EncryptAppsettings(loggersSet);
                else if (args.Any(a => a == @"-d"))
                    DecryptAppsettings(loggersSet);
                else if (args.Any(a => a == @"-u"))
                    DcsCentralServerDbHelper.InitializeOrUpdateDb(Host.Services, configuration, loggersSet);
                else
                {
                    if (ConfigurationHelper.IsMainProcess(configuration))
                        DcsCentralServerDbHelper.InitializeOrUpdateDb(Host.Services, configuration, loggersSet);
                    Host.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.Sources.Clear();                                        

                    string cryptoCertificateValidFileName = ConfigurationCrypterHelper.GetConfigurationCrypterCertificateFileName(LoggersSet<Program>.Empty);
                    if (!String.IsNullOrEmpty(cryptoCertificateValidFileName))
                        config.AddEncryptedAppSettings(hostingContext.HostingEnvironment, crypter =>
                        {
                            crypter.CertificatePath = cryptoCertificateValidFileName;
                            crypter.KeysToDecrypt = GetKeysToEncrypt().ToList();
                        });

                    config.AddCommandLine(args);
                })
                .ConfigureLogging(
                    builder =>
                        builder.ClearProviders()
                            .AddSszLogger()
                    )
                .UseSystemd()
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    })
                .ConfigureServices((hostContext, services) =>
                {
                    //services.Configure<HostOptions>(hostOptions =>
                    //{
                    //    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                    //});
                    services.AddHostedService<MainBackgroundService>();
                })                
                .UseWindowsService();
        }

        private static void EncryptAppsettings(ILoggersSet loggersSet)
        {
            string cryptoCertificateValidFileName = ConfigurationCrypterHelper.GetConfigurationCrypterCertificateFileName(loggersSet);
            if (String.IsNullOrEmpty(cryptoCertificateValidFileName))
                return;

            //ICertificateLoader certificateLoader = new FilesystemCertificateLoader(Path.Combine(Directory.GetCurrentDirectory(), AppsettingsCertificateFileName));
            ICertificateLoader certificateLoader = new FilesystemCertificateLoader(cryptoCertificateValidFileName);
            IConfigurationCrypter ConfigurationCrypter = new YamlConfigurationCrypter(new RSACrypter(certificateLoader));
            ConfigurationCrypter.EncryptKeys(@"appsettings.yml", GetKeysToEncrypt());
        }

        private static void DecryptAppsettings(ILoggersSet loggersSet)
        {
            string cryptoCertificateValidFileName = ConfigurationCrypterHelper.GetConfigurationCrypterCertificateFileName(loggersSet);
            if (String.IsNullOrEmpty(cryptoCertificateValidFileName))
                return;

            ICertificateLoader certificateLoader = new FilesystemCertificateLoader(cryptoCertificateValidFileName);
            IConfigurationCrypter ConfigurationCrypter = new YamlConfigurationCrypter(new RSACrypter(certificateLoader));
            ConfigurationCrypter.DecryptKeys(@"appsettings.yml", GetKeysToEncrypt());
        }

        private static HashSet<string> GetKeysToEncrypt()
        {
            string separator = @":";            
            HashSet<string> keysToEncrypt = new()
            {
                $"Kestrel{separator}Certificates{separator}Default{separator}Password",
                $"ConnectionStrings{separator}MainDbConnection",
            };            
            return keysToEncrypt;
        }

        #endregion
    }
}
