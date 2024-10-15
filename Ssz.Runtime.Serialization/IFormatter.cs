//#nullable enable

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.Serialization;
//using System.Text;
//using System.Threading.Tasks;

//namespace Ssz.Runtime.Serialization
//{
//    public interface IFormatter
//    {
//        //
//        // Summary:
//        //     Gets or sets the System.Runtime.Serialization.SerializationBinder that performs
//        //     type lookups during deserialization.
//        //
//        // Returns:
//        //     The System.Runtime.Serialization.SerializationBinder that performs type lookups
//        //     during deserialization.
//        SerializationBinder? Binder { get; set; }
//        //
//        // Summary:
//        //     Gets or sets the System.Runtime.Serialization.StreamingContext used for serialization
//        //     and deserialization.
//        //
//        // Returns:
//        //     The System.Runtime.Serialization.StreamingContext used for serialization and
//        //     deserialization.
//        StreamingContext Context { get; set; }
//        //
//        // Summary:
//        //     Gets or sets the System.Runtime.Serialization.SurrogateSelector used by the current
//        //     formatter.
//        //
//        // Returns:
//        //     The System.Runtime.Serialization.SurrogateSelector used by this formatter.
//        ISurrogateSelector? SurrogateSelector { get; set; }

//        //
//        // Summary:
//        //     Deserializes the data on the provided stream and reconstitutes the graph of objects.
//        //
//        //
//        // Parameters:
//        //   serializationStream:
//        //     The stream that contains the data to deserialize.
//        //
//        // Returns:
//        //     The top object of the deserialized graph.        
//        object Deserialize(Stream serializationStream);
//        //
//        // Summary:
//        //     Serializes an object, or graph of objects with the given root to the provided
//        //     stream.
//        //
//        // Parameters:
//        //   serializationStream:
//        //     The stream where the formatter puts the serialized data. This stream can reference
//        //     a variety of backing stores (such as files, network, memory, and so on).
//        //
//        //   graph:
//        //     The object, or root of the object graph, to serialize. All child objects of this
//        //     root object are automatically serialized
        
//        void Serialize(Stream serializationStream, object graph);
//    }
//}
