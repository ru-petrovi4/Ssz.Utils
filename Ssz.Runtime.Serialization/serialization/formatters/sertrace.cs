// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
 **
 ** Class: SerTrace
 **
 **
 ** Purpose: Routine used for Debugging
 **
 **
 ===========================================================*/

namespace Ssz.Runtime.Serialization.Formatters {
    using System;    
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Reflection;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

#if FEATURE_PAL
    // To turn on tracing, add the following to the per-machine
    // rotor.ini file, inside the [Rotor] section:
    //     ManagedLogFacility=0x32
    // where:
#else
    // To turn on tracing the set registry
    // HKEY_CURRENT_USER -> Software -> Microsoft -> .NETFramework
    // new DWORD value ManagedLogFacility 0x32 where
#endif
    // 0x2 is System.Runtime.Serialization
    // 0x10 is Binary Formatter
    // 0x20 is Soap Formatter
    //
    // Turn on Logging in the jitmgr


    // remoting Wsdl logging
    /// <internalonly/>
   //[System.Security.SecurityCritical]  // auto-generated_required
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class InternalRM
    {
        /// <internalonly/>
        [System.Diagnostics.Conditional("_LOGGING")]
        public static void InfoSoap(params Object[]messages)
        {
            BCLDebug.Trace("SOAP", messages);
        }

        //[System.Diagnostics.Conditional("_LOGGING")]        
        /// <internalonly/>
        public static bool SoapCheckEnabled()
        {
            return BCLDebug.CheckEnabled("SOAP");
        }
    }

    /// <internalonly/>
   //[System.Security.SecurityCritical]  // auto-generated_required
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class InternalST
    {
        private InternalST()
        {
        }

        /// <internalonly/>
        [System.Diagnostics.Conditional("_LOGGING")]
        public static void InfoSoap(params Object[]messages)
        {
            BCLDebug.Trace("SOAP", messages);
        }

        //[System.Diagnostics.Conditional("_LOGGING")]        
        /// <internalonly/>
        public static bool SoapCheckEnabled()
        {
            return BCLDebug.CheckEnabled("Soap");
        }

        /// <internalonly/>
        [System.Diagnostics.Conditional("SER_LOGGING")]        
        public static void Soap(params Object[]messages)
        {
            if (!(messages[0] is String))
                messages[0] = (messages[0].GetType()).Name+" ";
            else
                messages[0] = messages[0]+" ";                

            BCLDebug.Trace("SOAP",messages);                                
        }

        /// <internalonly/>
        [System.Diagnostics.Conditional("_DEBUG")]        
        public static void SoapAssert(bool condition, String message)
        {
            Contract.Assert(condition, message);
        }

        /// <internalonly/>
        public static void SerializationSetValue(FieldInfo fi, Object target, Object value)
        {
            if (fi == null)
                throw new ArgumentNullException("fi");

            if (target == null)
                throw new ArgumentNullException("target");

            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();

            SszFormatterServices.SerializationSetValue(fi, target, value);
        }

        /// <internalonly/>
        public static Assembly LoadAssemblyFromString(String assemblyString)
        {
            return SszFormatterServices.LoadAssemblyFromString(assemblyString);
        }
    }

    internal static class SerTrace
    {
        [Conditional("_LOGGING")]
        internal static void InfoLog(params Object[]messages)
        {
            BCLDebug.Trace("BINARY", messages);
        }

        [Conditional("SER_LOGGING")]            
        internal static void Log(params Object[]messages)
        {
            if (!(messages[0] is String))
                messages[0] = (messages[0].GetType()).Name+" ";
            else
                messages[0] = messages[0]+" ";                                
            BCLDebug.Trace("BINARY",messages);
        }
    }
}
