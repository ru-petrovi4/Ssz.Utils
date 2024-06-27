using Microsoft.Extensions.Logging.Abstractions;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpcClient_OpcServer.Common
{
    public static class GrpcDataAccessProviderHelper
    {
        public static IDataAccessProvider GetGrpcDataAccessProvider()
        {
            return new GrpcDataAccessProvider(NullLogger<GrpcDataAccessProvider>.Instance);
        }
    }
}
