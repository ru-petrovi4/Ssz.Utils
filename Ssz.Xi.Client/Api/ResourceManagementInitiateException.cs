using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Xi.Client.Api
{
    public class ResourceManagementInitiateException : Exception
    {
        public ResourceManagementInitiateException(Exception innerException) :
            base(@"IResourceManagement.Initiate(...) exception", innerException)
        {
        }
    }
}
