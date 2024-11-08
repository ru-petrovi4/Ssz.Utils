using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using ProtoBuf;
using Ssz.Utils.Serialization;

namespace Ssz.Utils.Serialization
{
    /// <summary>
    ///     Implementation of serialization, which uses JsonSerializer internally.
    ///     Implemented Equal method which based of comparing serialized data.
    ///     Implemented ICloneable interface which based on serialized data.  
    /// </summary>
    [Serializable]    
    public abstract class OwnedDataSerializable : IOwnedDataSerializable, ICloneable
    {
        #region public functions

        /// <summary>
        ///     Implementation of serialization, which uses JsonSerializer internally.        
        ///     You can specify JsonSerializerOptions as context.
        /// </summary>
        /// <param name="writer"> The SerializationWriter to use </param>
        /// <param name="context"> Optional context to use as a hint as to what to store (BitVector32 is useful) </param>
        public virtual void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                using (var memoryStream = new MemoryStream())
                {
                    Serializer.Serialize(memoryStream, this);

                    writer.Write(memoryStream.ToArray());
                }
            }
        }

        /// <summary>        
        ///     Implementation of serialization, which uses JsonSerializer internally.
        ///     You can specify JsonSerializerOptions as context.
        /// </summary>
        /// <param name="reader"> The SerializationReader to use </param>
        /// <param name="context"> Optional context to use as a hint as to what to retrieve (BitVector32 is useful) </param>
        public virtual void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        byte[] data = reader.ReadByteArray();

                        MethodInfo? methodInfo = typeof(OwnedDataSerializable).GetMethod("Merge", BindingFlags.NonPublic | BindingFlags.Static);
                        MethodInfo genericMethod = methodInfo!.MakeGenericMethod(GetType());                        
                        genericMethod.Invoke(null, [ new MemoryStream(data), this ]);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>a new object that is a copy of the current instance. </returns>
        public virtual object Clone()
        {
            return SerializationHelper.CloneUsingSerialization(this);
        }

        /// <summary>
        ///     Compares objects.
        /// </summary>
        /// <returns>Returns true if both references is equal or both objects have equal serialized data</returns>
        public override bool Equals(object? obj)
        {
            var other = obj as OwnedDataSerializable;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetType() != other.GetType()) return false;

            byte[] thisOwnedData;
            using (var memoryStream = new MemoryStream(1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    SerializeOwnedData(writer, null);
                }
                thisOwnedData = memoryStream.ToArray();
            }

            byte[] otherOwnedData;
            using (var memoryStream = new MemoryStream(1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    other.SerializeOwnedData(writer, null);
                }
                otherOwnedData = memoryStream.ToArray();
            }

            return thisOwnedData.SequenceEqual(otherOwnedData);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return @"";
        }

        #endregion

        #region private functions

        private static void Merge<T>(Stream source, T instance)
        {
            Serializer.Merge(source, instance);
        }

        #endregion
    }
}