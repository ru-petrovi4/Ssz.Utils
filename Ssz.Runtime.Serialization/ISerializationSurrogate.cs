//#nullable enable
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.Serialization;
//using System.Text;
//using System.Threading.Tasks;

//namespace Ssz.Runtime.Serialization
//{
//    public interface ISerializationSurrogate
//    {
//        //
//        // Summary:
//        //     Populates the provided System.Runtime.Serialization.SerializationInfo with the
//        //     data needed to serialize the object.
//        //
//        // Parameters:
//        //   obj:
//        //     The object to serialize.
//        //
//        //   info:
//        //     The System.Runtime.Serialization.SerializationInfo to populate with data.
//        //
//        //   context:
//        //     The destination (see System.Runtime.Serialization.StreamingContext) for this
//        //     serialization.
//        //
//        // Exceptions:
//        //   T:System.Security.SecurityException:
//        //     The caller does not have the required permission.
//        void GetObjectData(object obj, SerializationInfo info, StreamingContext context);
//        //
//        // Summary:
//        //     Populates the object using the information in the System.Runtime.Serialization.SerializationInfo.
//        //
//        //
//        // Parameters:
//        //   obj:
//        //     The object to populate.
//        //
//        //   info:
//        //     The information to populate the object.
//        //
//        //   context:
//        //     The source from which the object is deserialized.
//        //
//        //   selector:
//        //     The surrogate selector where the search for a compatible surrogate begins.
//        //
//        // Returns:
//        //     The populated deserialized object.
//        //
//        // Exceptions:
//        //   T:System.Security.SecurityException:
//        //     The caller does not have the required permission.
//        object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector? selector);
//    }
//}
