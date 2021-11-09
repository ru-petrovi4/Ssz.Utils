using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
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
                string s = JsonSerializer.Serialize(this, GetType(), context as JsonSerializerOptions);
                writer.Write(s);
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
                        string s = reader.ReadString();
                        object? temp = JsonSerializer.Deserialize(s, GetType(), context as JsonSerializerOptions);
                        if (temp is null) throw new InvalidOperationException();
                        MemberInfo[] members = FormatterServices.GetSerializableMembers(GetType());
                        FormatterServices.PopulateObjectMembers(this, members,
                            FormatterServices.GetObjectData(temp, members));
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
    }
}