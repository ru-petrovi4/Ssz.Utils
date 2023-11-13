using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class FromAddon_DataAccessProviderBase : DataAccessProviderBase
    {
        #region construction and destruction

        public FromAddon_DataAccessProviderBase(AddonBase addon, ILoggersSet loggersSet) :
            base(loggersSet)
        {
            Addon = addon;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Throws if any errors.
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        public override Task<IEnumerable<byte>> PassthroughAsync(string recipientId, string passthroughName, byte[] dataToSend)
        {
            switch (passthroughName)
            {
                case PassthroughConstants.SetAddonVariables:
                    var nameValuesCollectionString = Encoding.UTF8.GetString(dataToSend);
                    var nameValuesCollection = NameValueCollectionHelper.Parse(nameValuesCollectionString);
                    Addon.CsvDb.SetData(AddonBase.VariablesCsvFileName, nameValuesCollection.Select(kvp => new string?[] { kvp.Key, kvp.Value }));
                    Addon.CsvDb.SaveData();
                    break;
            }

            return Task.FromResult<IEnumerable<byte>>(new byte[0]);
        }

        #endregion        

        #region protected functions

        protected AddonBase Addon { get; }

        #endregion
    }
}
