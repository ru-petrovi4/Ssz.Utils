﻿// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>Microsoft</OWNER>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

namespace Ssz.Runtime.Serialization
{
    //
    // #SafeSerialization
    // 
    // Types which are serializable via the ISerializable interface have a problem when it comes to allowing
    // transparent subtypes which can allow themselves to serialize since the GetObjectData method is
    // SecurityCritical.
    // 
    // For instance, System.Exception implements ISerializable, however it is also desirable to have
    // transparent exceptions with their own fields that need to be serialized.  (For instance, in transparent
    // assemblies such as the DLR and F#, or even in partial trust application code).  Since overriding
    // GetObjectData requires that the overriding method be security critical, this won't work directly.
    //
    // SafeSerializationManager solves this problem by allowing any partial trust code to contribute
    // individual chunks of serializable data to be included in the serialized version of the derived class. 
    // These chunks are then deserialized back out of the serialized type and notified that they should
    // populate the fields of the deserialized object when serialization is complete.  This allows partial
    // trust or transparent code to participate in serialization of an ISerializable type without having to
    // override GetObjectData or implement the ISerializable constructor.
    //
    // On the serialization side, SafeSerializationManager has an event SerializeObjectState which it will
    // fire in response to serialization in order to gather the units of serializable data that should be
    // stored with the rest of the object during serialization.  Methods which respond to these events
    // create serializable objects which implement the ISafeSerializationData interface and add them to the
    // collection of other serialized data by calling AddSerializedState on the SafeSerializationEventArgs
    // passed into the event.
    // 
    // By using an event rather than a virtual method on the base ISerializable object, we allow multiple
    // potentially untrusted subclasses to participate in serialization, without each one having to ensure
    // that it calls up to the base type in order for the whole system to work.  (For instance Exception :
    // TrustedException : UntrustedException, in this scenario UntrustedException would be able to override
    // the virtual method an prevent TrustedException from ever seeing the method call, either accidentally
    // or maliciously).
    // 
    // Further, by only allowing additions of new chunks of serialization state rather than exposing the
    // whole underlying list, we avoid exposing potentially sensitive serialized state to any of the
    // potentially untrusted subclasses.
    // 
    // At deserialization time, SafeSerializationManager performs the reverse operation.  It deserializes the
    // chunks of serialized state, and then notifies them that the object they belong to is deserialized by
    // calling their CompleteSerialization method. In repsonse to this call, the state objects populate the
    // fields of the object being deserialized with the state that they held.
    // 
    // From a security perspective, the chunks of serialized state can only contain data that the specific
    // subclass itself had access to read (otherwise it wouldn't be able to populate the type with that
    // data), as opposed to having access to far more data in the SerializationInfo that GetObjectData uses.
    // Similarly, at deserialization time, the serialized state can only modify fields that the type itself
    // has access to (again, as opposed to the full SerializationInfo which could be modified).
    // 
    // Individual types which wish to participate in safe serialization do so by containing an instance of a
    // SafeSerializationManager and exposing its serialization event.  During GetObjectData, the
    // SafeSerializationManager is serialized just like any other field of the containing type.  However, at
    // the end of serialization it is called back one last time to CompleteSerialization.
    // 
    // In CompleteSerialization, if the SafeSerializationManager detects that it has extra chunks of
    // data to handle, it substitutes the root type being serialized (formerly the real type hosting the
    // SafeSerializationManager) with itself.  This allows it to gain more control over the deserialization
    // process.  It also saves away an extra bit of state in the serialization info indicating the real type
    // of object that should be recreated during deserialization.
    //
    // At this point the serialized state looks like this:
    //   Data:
    //     realSerializedData1
    //       ...
    //     realSerializedDataN
    //     safeSerializationData     -> this is the serialization data member of the parent type
    //       m_serializedState       -> list of saved serialized states from subclasses responding to the safe
    //                                  serialization event
    //     RealTypeSerializationName -> type which is using safe serialization
    //   Type:
    //     SafeSerializationManager
    //
    //  That is, the serialized data claims to be of type SafeSerializationManager, however contains only the
    //  data from the real object being serialized along with one bit of safe serialization metadata.
    //  
    //  At deserialization time, since the serialized data claims to be of type SafeSerializationManager, the
    //  root object being created is an instance of the SafeSerializationManager class.  However, it detects
    //  that this isn't a real SafeSerializationManager (by looking for the real type field in the metadata),
    //  and simply saves away the SerializationInfo and the real type being deserialized.
    //  
    //  Since SafeSerializationManager implements IObjectReference, the next step of deserialization is the
    //  GetRealObject callback.  This callback is the one responsible for getting the
    //  SafeSerializationManager out of the way and instead creating an instance of the actual type which was
    //  serialized.
    //  
    //  It does this by first creating an instance of the real type being deserialzed (saved away in the
    //  deserialzation constructor), but not running any of its constructors.  Instead, it walks the
    //  inheritance hierarchy (moving toward the most derived type) looking for the last full trust type to
    //  implement the standard ISerializable constructor before any type does not implement the constructor. 
    //  It is this last type's deserialization constructor which is then invoked, passing in the saved
    //  SerializationInfo.  Once the constructors are run, we return this object as the real deserialized
    //  object.
    //  
    //  The reason that we do this walk is so that ISerializable types can protect themselves from malicious
    //  input during deserialization by making their deserialization constructors unavailable to partial
    //  trust code.  By not requiring every type have a copy of this constructor, partial trust code can
    //  participate in safe serialization and not be required to have access to the parent's constructor. 
    //  
    //  It should be noted however, that this heuristic means that if a full trust type does derive from
    //  a transparent or partial trust type using this safe serialization mechanism, that full trust type
    //  will not have its constructor called. Further, the protection of not invoking partial trust
    //  deserialization constructors only comes into play if SafeSerializationManager is in control of
    //  deserialization, which means there must be at least one (even empty) safe serialization event
    //  handler registered.
    //  
    //  Another interesting note is that at this point there are now two SafeSerializationManagers alive for
    //  this deserialization.  The first object is the one which is controlling the deserialization and was
    //  created as the root object of the deserialization.  The second one is the object which contains the
    //  serialized data chunks and is a data member of the real object being deserialized.  For this reason,
    //  the data objects cannot be notified that the deserialization is complete during GetRealObject since
    //  the ISafeSerializationData objects are not members of the active SafeSerializationManager instance.
    //  
    //  The next step is the OnDeserialized callback, which comes to SafeSerializableObject since it was
    //  pretending to be the root object of the deserialization.  It responds to this callback by calling
    //  any existing OnDeserialized callback on the real type that was deserialized.
    //  
    //  The real type needs to call its data member SafeSerializationData object's CompleteDeserialization
    //  method in response to the OnDeserialized call.  This CompleteDeserialization call will then iterate
    //  through the ISafeSerializationData objects calling each of their CompleteDeserialization methods so
    //  that they can plug the nearly-complete object with their saved data.
    //  
    //  The reason for having a new ISafeSerializationData interface which is basically identical to
    //  IDeserializationCallback is that IDeserializationCallback will be called on the stored data chunks
    //  by the serialization code when they are deserialized, and that's not a desirable behavior. 
    //  Essentially, we need to change the meaning of the object parameter to mean "parent object which
    //  participated in safe serialization", rather than "this object".
    //  
    //  Implementing safe serialization on an ISerialiable type is relatively straight forward.  (For an
    //  example, see System.Exception):
    //  
    //    1. Include a data member of type SafeSerializationManager:
    //  
    //       private SafeSerializationManager m_safeSerializationManager;
    //     
    //    2. Add a protected SerializeObjectState event, which passes through to the SafeSerializationManager:
    //  
    //       protected event EventHandler<SafeSerializationEventArgs> SerializeObjectState
    //       {
    //           add { m_safeSerializationManager.SerializeObjectState += value; }
    //           remove { m_safeSerializationManager.SerializeObjectState -= value; }
    //       }
    //
    //    3. Serialize the safe serialization object in GetObjectData, and call its CompleteSerialization method:
    //  
    //       //[SecurityCritical]
    //       void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    //       {
    //           info.AddValue("m_safeSerializationManager", m_safeSerializationManager, typeof(SafeSerializationManager));
    //           m_safeSerializationManager.CompleteSerialization(this, info, context);
    //       }
    //
    //    4. Add an OnDeserialized handler if one doesn't already exist, and call CompleteDeserialization in it:
    //  
    //       [OnDeserialized]
    //       private void OnDeserialized(StreamingContext context)
    //       {
    //           m_safeSerializationManager.CompleteDeserialization(this);
    //       }
    //
    // On the client side, using safe serialization is also pretty easy.  For example:
    // 
    //   [Serializable]
    //   public class TransparentException : Exception
    //   {
    //       [Serializable]
    //       private struct TransparentExceptionState : ISafeSerializationData
    //       {
    //           public string m_extraData;
    //
    //           void ISafeSerializationData.CompleteDeserialization(object obj)
    //           {
    //               TransparentException exception = obj as TransparentException;
    //               exception.m_state = this;
    //           }
    //       }
    //
    //       [NonSerialized]
    //       private TransparentExceptionState m_state = new TransparentExceptionState();
    //
    //       public TransparentException()
    //       {
    //           SerializeObjectState += delegate(object exception, SafeSerializationEventArgs eventArgs)
    //           {
    //               eventArgs.AddSerializedState(m_state);
    //           };
    //       }
    //
    //       public string ExtraData
    //       {
    //           get { return m_state.m_extraData; }
    //           set { m_state.m_extraData = value; }
    //       }
    //   }
    // 

    // SafeSerializationEventArgs are provided to the delegates which do safe serialization.  Each delegate
    // serializes its own state into an IDeserializationCallback instance which must, itself, be serializable.
    // These indivdiual states are then added to the SafeSerializationEventArgs in order to be saved away when
    // the original ISerializable type is serialized.
    public sealed class SafeSerializationEventArgs : EventArgs
    {
        private StreamingContext m_streamingContext;
        private List<object> m_serializedStates = new List<object>();

        internal SafeSerializationEventArgs(StreamingContext streamingContext)
        {
            m_streamingContext = streamingContext;
        }

        public void AddSerializedState(ISafeSerializationData serializedState)
        {
            if (serializedState == null)
                throw new ArgumentNullException("serializedState");
            if (!serializedState.GetType().IsSerializable)
                throw new ArgumentException(SszEnvironment.GetResourceString("Serialization_NonSerType", serializedState.GetType(), serializedState.GetType().Assembly.FullName));

            m_serializedStates.Add(serializedState);
        }

        internal IList<object> SerializedStates
        {
            get { return m_serializedStates; }
        }

        public StreamingContext StreamingContext
        {
            get { return m_streamingContext; }
        }
    }

    // Interface to be supported by objects which are stored in safe serialization stores
    public interface ISafeSerializationData
    {
        // CompleteDeserialization is called when the object to which the extra serialized data was attached
        // has completed its deserialization, and now needs to be populated with the extra data stored in
        // this object.
        void CompleteDeserialization(object deserialized);
    }

    // Helper class to implement safe serialization.  Concrete ISerializable types which want to allow
    // transparent subclasses code to participate in serialization should contain an instance of 
    // SafeSerializationManager and wire up to it as described in code:#SafeSerialization.
    [Serializable]
    internal sealed class SafeSerializationManager : IObjectReference, ISerializable
    {
        // Saved states to store in the serialization stream.  This is typed as object rather than
        // ISafeSerializationData because ISafeSerializationData can't be marked serializable.
        private IList<object> m_serializedStates;

        // This is the SerializationInfo that is used when the SafeSerializationManager type has replaced
        // itself as the target of serialziation.  It is not used directly by the safe serialization code,
        // but just held onto so that the real object being deserialzed can use it later.
        private SerializationInfo m_savedSerializationInfo;

        // Real object that we've deserialized - this is stored when we complete construction and calling
        // the deserialization .ctors on it and is used when we need to notify the stored safe
        // deserialization data that they should populate the object with their fields.
        private object m_realObject;

        // Real type that should be deserialized
        private Type m_realType;
        
        // Event fired when we need to collect state to serialize into the parent object
        internal event EventHandler<SafeSerializationEventArgs> SerializeObjectState;

        // Name that is used to store the real type being deserialized in the main SerializationInfo
        private const string RealTypeSerializationName = "CLR_SafeSerializationManager_RealType";

        internal SafeSerializationManager()
        {
        }

        //[SecurityCritical]
        private SafeSerializationManager(SerializationInfo info, StreamingContext context)
        {
            // We need to determine if we're being called to really deserialize a SafeSerializationManager,
            // or if we're being called because we've intercepted the deserialization callback for the real
            // object being deserialized.  We use the presence of the RealTypeSerializationName field in the
            // serialization info to indicate that this is the interception callback and we just need to
            // safe the info.  If that field is not present, then we should be in a real deserialization
            // construction.
            //VALFIX
            //Type realType = info.GetValueNoThrow(RealTypeSerializationName, typeof(Type)) as Type;
            Type realType = info.GetValue(RealTypeSerializationName, typeof(Type)) as Type;

            if (realType == null)
            {
                m_serializedStates = info.GetValue("m_serializedStates", typeof(List<object>)) as List<object>;
            }
            else
            {
                m_realType = realType;
                m_savedSerializationInfo = info;
            }
        }

        // Determine if the serialization manager is in an active state - that is if any code is hooked up
        // to use it for serialization
        internal bool IsActive
        {
            get { return SerializeObjectState != null; }
        }

        // CompleteSerialization is called by the base ISerializable in its GetObjectData method.  It is
        // responsible for gathering up the serialized object state of any delegates that wish to add their
        // own state to the serialized object.
        //[SecurityCritical]
        internal void CompleteSerialization(object serializedObject,
                                            SerializationInfo info,
                                            StreamingContext context)
        {
            Contract.Requires(serializedObject != null);
            Contract.Requires(info != null);
            Contract.Requires(typeof(ISerializable).IsAssignableFrom(serializedObject.GetType()));
            Contract.Requires(serializedObject.GetType().IsAssignableFrom(info.ObjectType));

            // Clear out any stale state
            m_serializedStates = null;

            // We only want to kick in our special serialization sauce if someone wants to participate in
            // it, otherwise if we have no delegates registered there's no reason for us to get in the way
            // of the regular serialization machinery.
            EventHandler<SafeSerializationEventArgs> serializeObjectStateEvent = SerializeObjectState;
            if (serializeObjectStateEvent != null)
            {
                // Get any extra data to add to our serialization state now
                SafeSerializationEventArgs eventArgs = new SafeSerializationEventArgs(context);
                serializeObjectStateEvent(serializedObject, eventArgs);
                m_serializedStates = eventArgs.SerializedStates;

                // Replace the type to be deserialized by the standard serialization code paths with
                // ourselves, which allows us to control the deserialization process.
                info.AddValue(RealTypeSerializationName, serializedObject.GetType(), typeof(Type));
                info.SetType(typeof(SafeSerializationManager));
            } 
        }

        // CompleteDeserialization is called by the base ISerializable object's OnDeserialized handler to
        // finish the deserialization of the object by notifying the saved states that they should
        // re-populate their portions of the deserialized object.
        internal void CompleteDeserialization(object deserializedObject)
        {
            Contract.Requires(deserializedObject != null);

            if (m_serializedStates != null)
            {
                foreach (ISafeSerializationData serializedState in m_serializedStates)
                {
                    serializedState.CompleteDeserialization(deserializedObject);
                }
            }
        }

        //[SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("m_serializedStates", m_serializedStates, typeof(List<IDeserializationCallback>));
        }

        // GetRealObject intercepts the deserialization process in order to allow deserializing part of the
        // object's inheritance heirarchy using standard ISerializable constructors, and the remaining
        // portion using the saved serialization states.
        //[SecurityCritical]
        object IObjectReference.GetRealObject(StreamingContext context)
        {
            // If we've already deserialized the real object, use that rather than deserializing it again
            if (m_realObject != null)
            {
                return m_realObject;
            }

            // If we don't have a real type to deserialize, then this is really a SafeSerializationManager
            // and we don't need to rebuild the object that we're standing in for.
            if (m_realType == null)
            {
                return this;
            }

            // Look for the last type in GetRealType's inheritance hierarchy which implements a critical
            // deserialization constructor.  This will be the object that we use as the deserialization
            // construction type to initialize via standard ISerializable semantics

            // First build up the chain starting at the type below Object and working to the real type we
            // serialized.
            Stack inheritanceChain = new Stack();
            Type currentType = m_realType;
            do
            {
                inheritanceChain.Push(currentType);
                currentType = currentType.BaseType as Type;
            }
            while (currentType != typeof(object));

            // Now look for the first type that does not implement the ISerializable .ctor.  When we find
            // that, previousType will point at the last type that did implement the .ctor.  We require that
            // the .ctor we invoke also be non-transparent
            ConstructorInfo serializationCtor = null;
            Type previousType = null;
            do
            {
                previousType = currentType;
                currentType = inheritanceChain.Pop() as Type;
                serializationCtor = currentType.GetSerializationCtor();
            }
            while (serializationCtor != null && serializationCtor.IsSecurityCritical);
            
            // previousType is the last type that did implement the deserialization .ctor before the first
            // type that did not, so we'll grab it's .ctor to use for deserialization.
            BCLDebug.Assert(previousType != null, "We should have at least one inheritance from the base type");
            serializationCtor = SszObjectManager.GetConstructor(previousType);

            // Allocate an instance of the final type and run the selected .ctor on that instance to get the
            // standard ISerializable initialization done.
            object deserialized = SszFormatterServices.GetUninitializedObject(m_realType);
            serializationCtor.SerializationInvoke(deserialized, m_savedSerializationInfo, context);
            m_savedSerializationInfo = null;
            m_realType = null;

            // Save away the real object that was deserialized so that we can fill it in later, and return
            // it back as the object that should result from the final deserialization.
            m_realObject = deserialized;
            return deserialized;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // We only need to complete deserialization if we were hooking the deserialization process.  If
            // we have not deserialized an object in the GetRealObject call, then there's nothing more for
            // us to do here.
            if (m_realObject != null)
            {
                // Fire the real object's OnDeserialized method if they registered one.  Since we replaced
                // ourselves as the target of the deserialization, OnDeserialized on the target won't
                // automatically get triggered unless we do it manually.
                SerializationEvents cache = SerializationEventsCache.GetSerializationEventsForType(m_realObject.GetType());
                cache.InvokeOnDeserialized(m_realObject, context);
                m_realObject = null;
            }
        }
    }
}
