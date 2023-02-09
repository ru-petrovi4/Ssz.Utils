using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.DataAccessGrpc.ServerBase
{
    internal sealed partial class AliasResult
    {
        #region public functions

        public Ssz.Utils.DataAccess.ResultInfo GetResultInfo()
        {
            return new Ssz.Utils.DataAccess.ResultInfo
            {
                StatusCode = this.StatusCode,
                Info = this.Info,
                Label = this.Label,
                Details = this.Details,
            };
        }

        #endregion
    }
}
