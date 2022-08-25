using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;

namespace Ssz.Utils.Addons
{
    public abstract class DataAccessProviderGetter_AddonBase : AddonBase
    {
        public static readonly string DataAccessClient_ServerAddress_OptionName = @"%(DataAccess_ServerAddress)";

        public static readonly string DataAccessClient_SystemNameToConnect_OptionName = @"%(DataAccess_SystemNameToConnect)";

        public static readonly string DataAccessClient_ContextParams_OptionName = @"%(DataAccess_ContextParams)";

        /// <summary>
        ///     Gets initialized IDataAccessProvider or writes to log and returns null.        
        /// </summary>
        /// <returns></returns>
        public abstract IDataAccessProvider? GetDataAccessProvider(IDispatcher callbackDispatcher);
    }
}