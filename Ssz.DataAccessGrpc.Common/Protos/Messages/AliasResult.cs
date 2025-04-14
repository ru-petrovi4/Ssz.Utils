using Google.Protobuf.WellKnownTypes;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Common
{
    public sealed partial class AliasResult
    {
        #region construction and destruction

        public AliasResult(Ssz.Utils.DataAccess.AliasResult aliasResult)
        {
            StatusCode = aliasResult.StatusCode;
            Info = aliasResult.Info;
            Label = aliasResult.Label;
            Details = aliasResult.Details;
            ClientAlias = aliasResult.ClientAlias;
            ServerAlias = aliasResult.ServerAlias;
        }

        #endregion

        #region public functions

        public Ssz.Utils.DataAccess.ResultInfo GetResultInfo()
        {
            return new Ssz.Utils.DataAccess.ResultInfo
            {
                StatusCode = this.StatusCode,
                Info = this.Info,
                Label = this.Label,
                Details = this.Details                
            };
        }

        #endregion
    }
}
