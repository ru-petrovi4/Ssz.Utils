using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.ServerBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ssz.Dcs.ControlEngine;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace Ssz.Dcs.ControlEngine
{
    public class Startup
    {
        #region public functions

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResourceMonitoring();

            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
                //options.MaxReceiveMessageSize = null;
                //options.MaxSendMessageSize = 4 * 1024 * 1024; // 4 MB
                options.ResponseCompressionLevel = CompressionLevel.Fastest;
                options.ResponseCompressionAlgorithm = "gzip";
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true }); // For NETSTANDARD2.0 Clients

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<DataAccessService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }

        #endregion
    }
}


//private readonly ILogger<Startup> _logger;

//public Startup(ILogger<Startup> logger)
//{
//    _logger = logger;
//}