using System;

namespace Ssz.Operator.Core.CustomExceptions
{
    public class ShowMessageException : Exception
    {
        #region construction and destruction

        public ShowMessageException(string message) :
            base(message)
        {
        }

        #endregion
    }
}