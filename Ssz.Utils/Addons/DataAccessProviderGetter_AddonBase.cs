using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using System;

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

        public IDataAccessProvider? DataAccessProvider { get; protected set; }

        /// <summary>
        ///     Creates initialized IDataAccessProvider or throws. 
        ///     Addon must be initialized.
        /// </summary>
        /// <returns></returns>
        public abstract void InitializeDataAccessProvider(IDispatcher callbackDispatcher);

        /// <summary>
        ///     Closes DataAccessProvider.
        ///     Addon must be initialized.
        /// </summary>
        public virtual void CloseDataAccessProvider()
        {
            if (DataAccessProvider is not null)
            {
                var t = DataAccessProvider.CloseAsync();
                DataAccessProvider = null;
            }
        }

        public override AddonStatus GetAddonStatus()
        {
            if (!IsInitialized)
                return new AddonStatus
                {
                    AddonGuid = Guid,
                    AddonIdentifier = Identifier,
                    AddonInstanceId = InstanceId,
                    StateCode = AddonStateCodes.STATE_INITIALIZING,
                    Label = Properties.Resources.Addon_STATE_INITIALIZING
                };

            if (DataAccessProvider is null)
                return new AddonStatus
                {
                    AddonGuid = Guid,
                    AddonIdentifier = Identifier,
                    AddonInstanceId = InstanceId,
                    StateCode = AddonStateCodes.STATE_NOT_OPERATIONAL,
                    Label = Properties.Resources.Addon_STATE_NOT_OPERATIONAL_DataAccessProviderIsNull
                };

            if (!DataAccessProvider.IsConnected)
                return new AddonStatus
                {
                    AddonGuid = Guid,
                    AddonIdentifier = Identifier,
                    AddonInstanceId = InstanceId,
                    StateCode = AddonStateCodes.STATE_NOT_OPERATIONAL,
                    Label = Properties.Resources.Addon_STATE_NOT_OPERATIONAL_DataAccessProviderIsNotConnected
                };

            return new AddonStatus
            {
                AddonGuid = Guid,
                AddonIdentifier = Identifier,
                AddonInstanceId = InstanceId,
                LastWorkTimeUtc = LastWorkTimeUtc,
                StateCode = AddonStateCodes.STATE_OPERATIONAL,
                Label = Properties.Resources.Addon_STATE_OPERATIONAL_DataAccessProviderIsConnected
            };
        }        
    }
}
