using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ssz.DataAccessGrpc.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ssz.Utils.Logging;
using Microsoft.Extensions.Configuration;
using Ssz.Utils;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public class Program
    {
        #region private functions

        private static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("App starting with args: " + String.Join(" ", args));

            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
            CultureHelper.InitializeUICulture(configuration, logger);

            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(
                    builder =>
                        builder.ClearProviders()
                            .AddSszLogger()
                    )
                .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<MainBackgroundService>();                        
                    })
                .UseWindowsService();

        #endregion
    }
}
