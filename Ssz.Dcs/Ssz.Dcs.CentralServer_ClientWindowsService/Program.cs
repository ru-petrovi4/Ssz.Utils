using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ssz.DataAccessGrpc.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ssz.Utils.Logging;
using Ssz.Utils.ConfigurationCrypter.Extensions;
using Microsoft.Extensions.Configuration;
using Ssz.Utils;
using Ssz.Dcs.CentralServer.Common.Helpers;
using System.IO;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public class Program
    {
        #region private functions

        private static void Main(string[] args)
        {
            ConfigurationHelper.SetCurrentDirectory(args);

            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("App starting with args: " + String.Join(" ", args));

            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
            CultureHelper.InitializeUICulture(configuration, logger);            

            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                { ConfigurationConstants.ConfigurationKeyMapping_CurrentDirectory, ConfigurationConstants.ConfigurationKey_CurrentDirectory }
            };

            return Host.CreateDefaultBuilder(args)
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
                .ConfigureServices((hostContext, services) =>
                    {
                        services.AddTransient<Worker>();
                        services.AddHostedService<MainBackgroundService>();
                    })
                .UseWindowsService();
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
}
