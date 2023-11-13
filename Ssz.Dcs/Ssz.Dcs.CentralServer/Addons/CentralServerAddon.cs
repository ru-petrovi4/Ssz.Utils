using Microsoft.Extensions.Configuration;
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

        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
        };

        #endregion

        #region internal functions

        internal static string DescStatic { get; set; } = Properties.Resources.DcsCentralServerAddon_Desc;

        #endregion
    }
}
