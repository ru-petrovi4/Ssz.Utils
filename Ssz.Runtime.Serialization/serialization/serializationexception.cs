// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*=============================================================================
**
** Class: SerializationException
**
**
** Purpose: Thrown when something goes wrong during serialization or 
**          deserialization.
**
**
=============================================================================*/

namespace Ssz.Runtime.Serialization {
    
    using System;
    using Ssz.Runtime.Serialization;

// [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable] public class SerializationException : SystemException {
        
        private static String _nullMessage = Ssz.Runtime.Serialization.Environment.GetResourceString("Arg_SerializationException");
        
        // Creates a new SerializationException with its message 
        // string set to a default message.
        public SerializationException() 
            : base(_nullMessage) {
            //SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }
        
        public SerializationException(String message) 
            : base(message) {
            //SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }

        public SerializationException(String message, Exception innerException) : base (message, innerException) {
            //SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }
        
        // VALFIX
        /*
        protected SerializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base (info, context) {
        }*/
    }
}
