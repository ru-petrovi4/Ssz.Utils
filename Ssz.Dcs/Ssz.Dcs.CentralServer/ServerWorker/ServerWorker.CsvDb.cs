using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region private functions

        private void CsvDbOnCsvFileChanged(CsvFileChangeAction csvFileChangeAction, string? fileName)
        {
            //if (fileName is null ||
            //    String.Equals(fileName, CsvDbConstants.ControlEnginesCsvFileName, StringComparison.InvariantCultureIgnoreCase))
            //{
            //    RefreshDcs_EngineSessions();
            //}
        }        

        #endregion
    }
}
