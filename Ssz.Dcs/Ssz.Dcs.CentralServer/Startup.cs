using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.Dcs.CentralServer;
using Ssz.DataAccessGrpc.ServerBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ssz.Utils;
using Ssz.Utils.Addons;
using System.IO.Compression;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using JsonApiDotNetCore.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.OpenApi;

namespace Ssz.Dcs.CentralServer
{
    public class Startup
    {
        #region public functions

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
                //options.MaxReceiveMessageSize = null; // Default value
                //options.MaxSendMessageSize = 4 * 1024 * 1024; // 4 MB // Default value
                options.ResponseCompressionLevel = CompressionLevel.Fastest;
                options.ResponseCompressionAlgorithm = "gzip";
            });

            services.AddDbContextFactory<DcsCentralServerDbContext>();
            services.AddDbContextFactory<SqliteDcsCentralServerDbContext>();
            services.AddDbContextFactory<NpgsqlDcsCentralServerDbContext>();            

            IMvcCoreBuilder mvcBuilder = services.AddMvcCore(options =>
            {
                options.UseCentralRoutePrefix(new RouteAttribute(RouteNamespace));
            }).AddApiExplorer(); // For Swagger
            //services.AddJsonApi<DcsCentralServerDbContext>(
            //    options =>
            //    {
            //        options.Namespace = RouteNamespace; // CentralRoutePrefix not added here, because already root prefix
            //        options.DefaultPageSize = null;
            //        options.IncludeTotalResourceCount = true;
            //        options.IncludeExceptionStackTraceInErrors = true;
            //        options.AllowClientGeneratedIds = true;
            //        options.SerializerOptions.DictionaryKeyPolicy = null; // Fix first letter lower-case
            //    },
            //    discover => discover.AddCurrentAssembly(),
            //    mvcBuilder: mvcBuilder);

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ssz DCS CentralServer API", Version = "v1.0.0" });

                //options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, @"Ssz.Dcs.CentralServer.Common.xml"));
                //options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, @"Ssz.Dcs.CentralServer.xml"));

                options.OperationFilter<SwaggerOperationFilter>();
            });

            services.AddSingleton<DataAccessServerWorkerBase, ServerWorker>();
            services.AddSingleton<AddonsManager>();

            services.AddTransient<DcsCentralServer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            //app.UseJsonApi();

#if DEBUG
            app.UseSwagger(); // http://localhost:60060/swagger/v1/swagger.json
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                //options.RoutePrefix = string.Empty;
            }); // http://localhost:60060/swagger    
#endif

            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true }); // For NETSTANDARD2.0 Clients

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); //.RequireAuthorization(@"MainPolicy");

                endpoints.MapGrpcService<DataAccessService>();
                endpoints.MapGrpcService<ProcessModelingSessionsManagementService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }

        #endregion

        private const string RouteNamespace = @"/api/v1";
    }
}


//private readonly ILogger<Startup> _logger;

//public Startup(ILogger<Startup> logger)
//{
//    _logger = logger;
//}