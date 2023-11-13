using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class DsFilesStoreItem
    {
        #region construction and destruction

        public DsFilesStoreItem(DsFilesStoreDirectory dsFilesStoreDirectory, DsFilesStoreFile? dsFilesStoreFile)
        {            
            DsFilesStoreDirectory = dsFilesStoreDirectory;
            DsFilesStoreFile = dsFilesStoreFile;
        }

        #endregion

        #region public functions        

        public DsFilesStoreDirectory DsFilesStoreDirectory { get; }

        public DsFilesStoreFile? DsFilesStoreFile { get; }

        #endregion
    }
}
