
using Grpc.Core;
using System;

namespace Ssz.DataGrpc.Server.Data
{
    /// <summary>
    /// Static class used to create FaultException<XiFalultInfo> to support WCF compliant faults.
    /// The FaultException<XiFalultInfo> class is a subclass of the CommunicationException class. 
    /// faults of this class are thrown by the server if they are intended to be communicated back 
    /// to the client.
    /// </summary>
    public static class RpcExceptionHelper
    {
        /// <summary>
        /// This method will return a FaultException with a XiFault
        /// where the ErrorCode will be E_XIMESSAGEFROMEXCEPTION.
        /// The message is from the exception passed into this method.
        /// </summary>
        /// <param name="ex"></param>
        static public RpcException Create(Exception ex)
        {
            if (ex is RpcException)
                return (RpcException)ex;
            return new RpcException(new XiFault(ex.Message), new FaultReason(ex.Message));
        }

        /// <summary>
        /// This method will return a FaultException with a XiFault
        /// where the ErrorCode will be E_XIFAULTMESSAGE.
        /// </summary>
        /// <param name="message">Error string</param>
        static public RpcException Create(string message)
        {
            return new RpcException(new XiFault(message), new FaultReason(message));
        }

        /// <summary>
        /// This throws a new FaultException with the XiFault detail
        /// </summary>
        /// <param name="errorCode">Error string</param>
        static public RpcException Create(uint errorCode)
        {
            string text = FaultStrings.Get(errorCode);
            return new RpcException(new XiFault(errorCode, text), new FaultReason(text));
        }

        /// <summary>
        /// This throws a new FaultException with the XiFault detail
        /// </summary>
        /// <param name="errorCode">Error code</param>
        /// <param name="message">Error string</param>
        static public RpcException Create(uint errorCode, string message)
        {
            return new RpcException(new XiFault(errorCode, message), new FaultReason(message));
        }
    }
}
