using System;
using System.IO;
using System.Linq;
using Ssz.Utils.Serialization;

namespace Ssz.Utils
{
    /// <summary>
    ///     Abstract base class allows to save/retrieve their internal data to/from an existing
    ///     SerializationWriter/SerializationReader.
    ///     Implemented Equal method which based of comparing serialized data.
    ///     Implemented interface which specify that class can be recreated during deserialization using a default
    ///     constructor and then calling DeserializeOwnedData()
    /// </summary>
    [Serializable]
    public abstract class OwnedDataSerializableAndCloneable : OwnedDataSerializable, ICloneable
    {
        #region public functions

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>a new object that is a copy of the current instance. </returns>
        public virtual object Clone()
        {
            return this.CloneUsingSerialization();
        }

        /// <summary>
        ///     Compares objects.
        /// </summary>
        /// <returns>Returns true if both references is equal or both objects have equal serialized data</returns>
        public override bool Equals(object obj)
        {
            var other = obj as OwnedDataSerializableAndCloneable;
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