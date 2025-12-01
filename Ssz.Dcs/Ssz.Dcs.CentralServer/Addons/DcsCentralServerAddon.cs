using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer
{
    public partial class DcsCentralServerAddon : AddonBase
    {
        #region public functions        

        public static readonly Guid AddonGuid = new Guid(@"78B739D5-5814-4B05-87CC-C369371A5003");

        public static readonly string AddonIdentifier = @"DcsCentralServer";

        public static readonly string ClientsCsvFileName = @"clients.csv";

        public override Guid Guid => AddonGuid;

        public override string Identifier => AddonIdentifier;

        public override string Desc => Properties.Resources.DcsCentralServerAddon_Desc;

        public override string Version => "1.0";

        public override bool IsAlwaysSwitchedOn => true;        

        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
        };

        public override void Initialize(CancellationToken cancellationToken)
        {
            base.Initialize(cancellationToken);
        }

        public override void Close()
        {
            base.Close();
        }

        public override async Task<AddonStatus> GetAddonStatusAsync()
        {
            AddonStatus addonStatus = await base.GetAddonStatusAsync();

            foreach (var kvp in await ComputerInfoHelper.GetSystemParamsAsync())
            {
                addonStatus.Params[kvp.Key] = kvp.Value;
            }
            addonStatus.Params[ParamName_IsResourceMonitoringAddon] = new Any(true);

            return addonStatus;
        }

        #endregion
    }
}
