// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: SerializationFieldInfo
**
**
** Purpose: Provides a methods of representing imaginary fields
** which are unique to serialization.  In this case, what we're
** representing is the private members of parent classes.  We
** aggregate the FieldInfo associated with this member 
** and return a managled form of the name.  The name that we
** return is .parentname.fieldname
**
**
============================================================*/

namespace Ssz.Runtime.Serialization {

    using System;
    using System.Reflection;
    using System.Globalization;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Diagnostics.Metrics;
#if FEATURE_REMOTING
    using System.Runtime.Remoting.Metadata;
#endif //FEATURE_REMOTING

    internal sealed class SerializationFieldInfo : System.Reflection.FieldInfo {

        internal const String FakeNameSeparatorString = "+";

        private System.Reflection.FieldInfo m_field;
        private String           m_serializationName;

        public override Module Module { get { return m_field.Module; } }
        public override int MetadataToken { get { return m_field.MetadataToken; } } 

        internal SerializationFieldInfo(System.Reflection.FieldInfo field, String namePrefix) {
            Contract.Assert(field!=null,      "[SerializationFieldInfo.ctor]field!=null");
            Contract.Assert(namePrefix!=null, "[SerializationFieldInfo.ctor]namePrefix!=null");
            
            m_field = field;

            //VALFIX
            var name = m_field.Name;
            //if (Ssz.Runtime.Serialization.Settings.IsDeserializingFromNet4)
            {
                if (name == "_list")
                    name = "list";                
            }

            m_serializationName = String.Concat(namePrefix, FakeNameSeparatorString, name);
        }

        //
        // MemberInfo methods
        //
        public override String Name {
            get {
                return m_serializationName;
            }
        }

        public override Type DeclaringType {
            get {
                return m_field.DeclaringType;
            }
        }

        public override Type ReflectedType {
            get {
                return m_field.ReflectedType;
            }
        }

        public override Object[] GetCustomAttributes(bool inherit) {
            return m_field.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit) {
            return m_field.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            return m_field.IsDefined(attributeType, inherit);
        }

        //
        // FieldInfo methods
        //
        public override Type FieldType {
            get {
                return m_field.FieldType;
            }
        }
        
        public override Object GetValue(Object obj) {
            return m_field.GetValue(obj);
        }

       //[System.Security.SecurityCritical]
        internal Object InternalGetValue(Object obj) {
            FieldInfo field = m_field as FieldInfo;
            if (field != null)
            {
                field.CheckConsistency(obj);
                return field.UnsafeGetValue(obj);
            }
            else
                return m_field.GetValue(obj);
        }

        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            m_field.SetValue(obj, value, invokeAttr, binder, culture);
        }

       //[System.Security.SecurityCritical]
        internal void InternalSetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            FieldInfo field = m_field as FieldInfo;
            if (field != null)
            {
                field.CheckConsistency(obj);
                field.UnsafeSetValue(obj, value, invokeAttr, binder, culture);
            }
            else
                m_field.SetValue(obj, value, invokeAttr, binder, culture);
        }

        internal System.Reflection.FieldInfo FieldInfo {
            get {
                return m_field;
            }
        }

        public override RuntimeFieldHandle FieldHandle {
            get {
                return m_field.FieldHandle;
            }
        }

        public override FieldAttributes Attributes {
            get {
                return m_field.Attributes;
            }
        }

#if FEATURE_REMOTING
        #region Legacy Remoting Cache
        private RemotingFieldCachedData m_cachedData;

        internal RemotingFieldCachedData RemotingCache
        {
            get
            {
                // This grabs an internal copy of m_cachedData and uses
                // that instead of looking at m_cachedData directly because
                // the cache may get cleared asynchronously.  This prevents
                // us from having to take a lock.
                RemotingFieldCachedData cache = m_cachedData;
                if (cache == null)
                {
                    cache = new RemotingFieldCachedData(this);
                    RemotingFieldCachedData ret = Interlocked.CompareExchange(ref m_cachedData, cache, null);
                    if (ret != null)
                        cache = ret;
                }
                return cache;
            }
        }
        #endregion
#endif //FEATURE_REMOTING
    }
}
