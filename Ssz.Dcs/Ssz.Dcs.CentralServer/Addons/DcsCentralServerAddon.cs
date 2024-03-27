using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.Addons;
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

        public override Guid Guid => AddonGuid;

        public override string Identifier => AddonIdentifier;

        public override string Desc => DescStatic;

        public override string Version => "1.0";

        public override bool IsAlwaysSwitchedOn => true;

        /// <summary>
        ///     Globally unique server service identifier.
        /// </summary>
        public string ServiceId { get; private set; } = @"";

        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
        };

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            ServiceId = Guid.NewGuid().ToString();

            _serverWorker = ServiceProvider.GetRequiredService<ServerWorkerBase>();

            await base.InitializeAsync(cancellationToken);
        }

        public override async Task CloseAsync()
        {            
            _serverWorker = null;

            await base.CloseAsync();
        }

        public override Ssz.Utils.Addons.AddonStatus GetAddonStatus()
        {
            Ssz.Utils.Addons.AddonStatus addonStatus = base.GetAddonStatus();

            _serverWorker?.GetAddonStatusParams(addonStatus.Params);

            return addonStatus;
        }

        #endregion

        #region internal functions

        internal static string DescStatic { get; set; } = Properties.Resources.DcsCentralServerAddon_Desc;

        #endregion

        #region private fields

        private ServerWorkerBase? _serverWorker;

        #endregion
    }
}
