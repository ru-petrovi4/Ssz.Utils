using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ssz.Utils;
using Ssz.Utils.Logging;
using System;
using System.Net;

namespace Ssz.IdentityServer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();

                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("App starting with args: " + String.Join(" ", args));

                IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
                CultureHelper.InitializeUICulture(configuration, logger);

                host.Run();

                return 0;
            }
            catch //(Exception ex)
            {
                //Log.Fatal(ex, "Host terminated unexpectedly.");
                return 1;
            }            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
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
                    services.AddHostedService<MainBackgroundService>();
                })              
                .UseWindowsService();
    }
}