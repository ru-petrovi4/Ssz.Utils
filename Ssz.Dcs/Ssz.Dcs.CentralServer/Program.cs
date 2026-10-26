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

        /// <summary>
        ///     Имя файла сертификата .pfx из которого используются закрытый и открытый ключи для шифрования и расшифровки секций конфигурационного файла, которые не должны храниться в открытом виде
        /// </summary>
        public const string ConfigurationCrypterCertificateFileName = @"appsettings.pfx";

        public static IHost Host { get; private set; } = null!;

        public static string EnvironmentName { get; private set; } = null!;

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
            ConfigurationHelper.SetCurrentDirectory(args);

            Host = CreateHostBuilder(args).Build();                       

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var logger = Host.Services.GetRequiredService<ILogger<Program>>();
            IConfiguration configuration = Host.Services.GetRequiredService<IConfiguration>();
            CultureHelper.InitializeUICulture(configuration, logger);

            ConfigurationHelper.LogProgramInformation(logger, configuration, args, EnvironmentName);

            try
            {                
                if (args.Any(a => a == @"-e"))
                {
                    ConfigurationHelper.EncryptAppsettings(ConfigurationCrypterCertificateFileName, GetKeysToEncrypt().ToHashSet(), logger);
                }
                else if (args.Any(a => a == @"-d"))
                {
                    ConfigurationHelper.DecryptAppsettings(ConfigurationCrypterCertificateFileName, GetKeysToEncrypt().ToHashSet(), logger);
                }
                else if (args.Any(a => a == @"-u"))
                {
                    DcsCentralServerDbHelper.InitializeOrUpdateDb(Host.Services, configuration, logger);
                }
                else
                {
                    if (ConfigurationHelper.IsMainProcess(configuration))
                        DcsCentralServerDbHelper.InitializeOrUpdateDb(Host.Services, configuration, logger);
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
            var switchMappings = new Dictionary<string, string>()
            {
                { ConfigurationConstants.ConfigurationKeyMapping_CurrentDirectory, ConfigurationConstants.ConfigurationKey_CurrentDirectory }                
            };

            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    EnvironmentName = ConfigurationHelper.GetEnvironmentName(hostingContext.HostingEnvironment);

                    config.Sources.Clear();

                    config.AddEncryptedAppSettings(hostingContext.HostingEnvironment, crypter =>
                    {
                        crypter.CertificatePath = ConfigurationCrypterCertificateFileName;
                        crypter.KeysToDecrypt = GetKeysToEncrypt();
                    });

                    config.AddCommandLine(args, switchMappings);
                })
                .ConfigureLogging(
                    builder =>
                        builder.ClearProviders()
                            .AddSszLogger()
                    )
                .UseSystemd()
                .UseWindowsService()
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    })
                .ConfigureServices((hostContext, services) =>
                {
                    //services.Configure<HostOptions>(o =>
                    //{
                    //    o.ShutdownTimeout = TimeSpan.FromSeconds(30);
                    //});
                    //services.Configure<HostOptions>(hostOptions =>
                    //{
                    //    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                    //});
                    services.AddHostedService<MainBackgroundService>();
                });
        }        

        private static List<string> GetKeysToEncrypt()
        {
            return new()
            {
                $"Kestrel:Certificates:Default:Password",
                $"ConnectionStrings:MainDbConnection",
                $"Encrypted"
            };
        }

        #endregion
    }
}
