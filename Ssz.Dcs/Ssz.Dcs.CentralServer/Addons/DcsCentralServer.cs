using System;
using System.Collections.Generic;
using System.Threading;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.ServerListItems;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using Ssz.Utils.Addons;
using System.Linq;
using Ssz.Dcs.CentralServer.Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;

namespace Ssz.Dcs.CentralServer
{
    public partial class DcsCentralServer
    {
        #region construction and destruction

        public DcsCentralServer(
                IResourceMonitor resourceMonitor,
                ILogger<DcsCentralServer> logger,
                IConfiguration configuration,
                IServiceProvider serviceProvider,
                AddonsManager addonsManager)
        {
            _resourceMonitor = resourceMonitor;
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _addonsManager = addonsManager;            
        }

        #endregion

        #region public functions

        public void GetSystemParams(CaseInsensitiveDictionary<Any> systemParams)
        {
            SystemInfoHelper.GetSystemParams(_resourceMonitor, systemParams);
        }

        #endregion

        #region private fields

        private readonly IResourceMonitor _resourceMonitor;

        private readonly ILogger _logger;

        private readonly IConfiguration _configuration;

        private readonly IServiceProvider _serviceProvider;

        private readonly AddonsManager _addonsManager;        

        #endregion
    }
}