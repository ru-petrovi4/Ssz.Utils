using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using System;

namespace Ssz.Dcs.CentralServer
{
    public class EngineSession : IDisposable
    {
        #region construction and destruction

        public EngineSession(DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon)
        {
            DataAccessProviderGetter_Addon = dataAccessProviderGetter_Addon;
        }

        public void Dispose()
        {
            DataAccessProviderGetter_Addon.Close();
        }

        #endregion

        #region public functions

        public DataAccessProviderGetter_AddonBase DataAccessProviderGetter_Addon { get; }

        public IDataAccessProvider DataAccessProvider => DataAccessProviderGetter_Addon.DataAccessProvider!;

        public string ServerAddress => DataAccessProvider.ServerAddress;

        public string ServerHost => DataAccessProvider.ServerHost;

        public string SystemNameToConnect => DataAccessProvider.SystemNameToConnect;

        public CaseInsensitiveDictionary<string?> ContextParams => DataAccessProvider.ContextParams;

        #endregion
    }
}
