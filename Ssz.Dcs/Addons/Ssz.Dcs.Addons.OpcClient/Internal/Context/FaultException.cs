using System;
using System.Runtime.Serialization;

namespace Ssz.Xi.Client.Internal.Context
{
    [Serializable]
    internal class FaultException<T> : Exception
    {
        public FaultException()
        {
        }

        public FaultException(string? message) : base(message)
        {
        }

        public FaultException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        //protected FaultException(SerializationInfo info, StreamingContext context) : base(info, context)
        //{
        //}
    }
}