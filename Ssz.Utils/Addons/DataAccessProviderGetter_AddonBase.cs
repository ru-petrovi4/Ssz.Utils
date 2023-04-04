using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;

namespace Ssz.Utils.Addons
{
    public abstract class DataAccessProviderGetter_AddonBase : AddonBase
    {
        /// <summary>
        ///     Standard option name, that implementers can use
        /// </summary>
        public static readonly string DataAccessProviderGetter_CommonEventMessageFieldsToAdd_OptionName = @"%(CommonEventMessageFieldsToAdd)";

        /// <summary>
        ///     Standard option name, that implementers can use
        /// </summary>
        public static readonly string DataAccessClient_ServerAddress_OptionName = @"%(DataAccessClient_ServerAddress)";

        /// <summary>
        ///     Standard option name, that implementers can use
        /// </summary>
        public static readonly string DataAccessClient_SystemNameToConnect_OptionName = @"%(DataAccessClient_SystemNameToConnect)";

        /// <summary>
        ///     Standard option name, that implementers can use
        /// </summary>
        public static readonly string DataAccessClient_ContextParams_OptionName = @"%(DataAccessClient_ContextParams)";

        /// <summary>
        ///     Standard option name, that implementers can use
        /// </summary>
        public static readonly string DataAccessClient_SystemNameToConnect_ToDisplay_OptionName = @"%(DataAccessClient_SystemNameToConnect_ToDisplay)";

        /// <summary>
        ///     Gets initialized IDataAccessProvider or writes to log and returns null.        
        /// </summary>
        /// <returns></returns>
        public abstract IDataAccessProvider? GetDataAccessProvider(IDispatcher callbackDispatcher);
    }
}
