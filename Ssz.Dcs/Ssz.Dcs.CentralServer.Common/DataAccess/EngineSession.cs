using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using System;

namespace Ssz.Dcs.CentralServer.Common
{
    public class EngineSession
    {
        #region construction and destruction

        public EngineSession(string engineSessionId, DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon)
        {
            EngineSessionId = engineSessionId;
            DataAccessProviderGetter_Addon = dataAccessProviderGetter_Addon;
            DataAccessProvider = DataAccessProviderGetter_Addon.DataAccessProvider!;
        }

        #endregion

        #region public functions

        public string EngineSessionId { get; }

        public DataAccessProviderGetter_AddonBase DataAccessProviderGetter_Addon { get; }

        public IDataAccessProvider DataAccessProvider { get; }        

        #endregion
    }
}
