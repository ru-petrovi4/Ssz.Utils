using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public class ResourceManagementService : ResourceManagement.ResourceManagementBase
    {
        private readonly ILogger<ResourceManagementService> _logger;
        public ResourceManagementService(ILogger<ResourceManagementService> logger)
        {
            _logger = logger;
        }

        //public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        //{
        //    return Task.FromResult(new HelloReply
        //    {
        //        Message = "Hello " + request.Name
        //    });
        //}
    }
}
