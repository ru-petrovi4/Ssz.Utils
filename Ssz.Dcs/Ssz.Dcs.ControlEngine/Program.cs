using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.Dcs.ControlEngine;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.Logging;
using Ssz.Utils.ConfigurationCrypter.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using Ssz.Dcs.CentralServer.Common.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    class Program
    {
        #region public functions 

        public static IHost Host { get; private set; } = null!;

        public static Options Options { get; private set; } = null!;

        #endregion                

        #region public functions

        public static async Task SafeShutdownAsync()
        {
            await Host.StopAsync();
        }

        #endregion

        #region private functions

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                { @"-d", @"ProcessDirectory" },                
                { @"-h", @"CentralServerHost" },
                { @"-s", @"CentralServerSystemName" },
            };

            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.Sources.Clear();

                    config.AddEncryptedAppSettings(hostingContext.HostingEnvironment, crypter =>
                    {
                        crypter.CertificatePath = @"appsettings.pfx";
                        crypter.KeysToDecrypt = GetKeysToEncrypt().ToList();
                    });

                    config.AddCommandLine(args, switchMappings);
                })
                .ConfigureLogging(
                    builder =>
                        builder.ClearProviders()
                            .AddSszLogger()
                    )
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {                        
                        webBuilder.UseStartup<Startup>();
                    })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<DataAccessServerWorkerBase, ServerWorker>();                    
                    services.AddHostedService<MainBackgroundService>();                    
                });
        }

        private static void Main(string[] args)
        {
            Host = CreateHostBuilder(args).Build();

            var logger = Host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("App starting with args: " + String.Join(" ", args));

            IConfiguration configuration = Host.Services.GetRequiredService<IConfiguration>();
            CultureHelper.InitializeUICulture(configuration, logger);
            Options = new Options(configuration);

            var urlSection = configuration.GetSection(@"Kestrel:Endpoints:HttpsDefaultCert:Url");
            if (urlSection.Value == @"*")
                urlSection.Value = Options.ControlEngineServerAddress;

            Host.Run();
        }

        /// <summary>
        ///     Separator ':'
        /// </summary>
        /// <returns></returns>
        private static HashSet<string> GetKeysToEncrypt()
        {   
            return new()
            {
            };
        }

        #endregion        
    }

    public class Options
    {
        #region construction and destruction

        public Options(IConfiguration configuration)
        {
            ProcessDirectory = ConfigurationHelper.GetValue<string>(configuration, @"ProcessDirectory", @"");
            CentralServerAddress = ConfigurationHelper.GetValue<string>(configuration, @"CentralServerAddress", @"");
            CentralServerHost = ConfigurationHelper.GetValue<string>(configuration, @"CentralServerHost", @"");
            CentralServerSystemName = ConfigurationHelper.GetValue<string>(configuration, @"CentralServerSystemName", Guid.Empty.ToString());
            ControlEngineServerAddress = ConfigurationHelper.GetValue<string>(configuration, @"ControlEngineServerAddress", @"");
            EngineSessionId = ConfigurationHelper.GetValue<string>(configuration, @"EngineSessionId", @"");
        }

        #endregion

        #region public functions

        public string ProcessDirectory { get; set; }

        public string CentralServerAddress { get; set; }

        public string CentralServerHost { get; set; }

        public string CentralServerSystemName { get; set; }

        public string ControlEngineServerAddress { get; set; }

        public string EngineSessionId { get; set; }

        public string GetCentralServerAddress()
        {
            if (String.IsNullOrEmpty(CentralServerHost))
            {
                return CentralServerAddress;
            }
            else
            {
                var uri = new UriBuilder(CentralServerAddress);
                uri.Host = CentralServerHost;
                return uri.Uri.OriginalString;
            }
        }

        #endregion        
    }
}
