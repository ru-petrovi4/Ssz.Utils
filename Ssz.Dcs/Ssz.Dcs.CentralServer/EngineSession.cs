using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer
{
    public class EngineSession: IDisposable
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataAccessProviderGetter_Addon"></param>       
        public EngineSession(DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon)
        {            
            DataAccessProviderGetter_Addon = dataAccessProviderGetter_Addon;
        }

        public virtual void Dispose()
        {
        }

        #endregion

        #region public functions

        public DataAccessProviderGetter_AddonBase DataAccessProviderGetter_Addon { get; }

        public IDataAccessProvider DataAccessProvider => DataAccessProviderGetter_Addon.DataAccessProvider!;

        #endregion
    }
}
