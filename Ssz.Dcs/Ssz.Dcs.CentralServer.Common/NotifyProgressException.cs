using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class NotifyProgressException : Exception
    {
        #region construction and destruction

        public NotifyProgressException()
        {
        }

        public NotifyProgressException(string? message) : base(message)
        {
        }

        public NotifyProgressException(string? message, Exception? innerException) : base(message, innerException)
        {
        }        

        #endregion

        #region public functions

        public string? PprogressLabelResourceName { get; set; }

        public string? ProgressDetails { get; set; }

        #endregion
    }
}
