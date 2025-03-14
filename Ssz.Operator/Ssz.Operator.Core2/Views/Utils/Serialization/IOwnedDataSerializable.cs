#nullable disable

namespace Ssz.Operator.Core.Utils.Serialization
{
    /// <summary>
    ///     Interface which allows a class to save/retrieve their internal data to/from an existing
    ///     SerializationWriter/SerializationReader.
    /// </summary>
    public interface IOwnedDataSerializable
    {
        /// <summary>
        ///     Lets the implementing class store internal data directly into a SerializationWriter.
        /// </summary>
        /// <param name="writer"> The SerializationWriter to use </param>
        /// <param name="context"> Optional context to use as a hint as to what to store </param>
        void SerializeOwnedData(SerializationWriter writer, object context);

        /// <summary>
        ///     Lets the implementing class retrieve internal data directly from a SerializationReader.
        /// </summary>
        /// <param name="reader"> The SerializationReader to use </param>
        /// <param name="context"> Optional context to use as a hint as to what to retrieve </param>
        void DeserializeOwnedDataAsync(SerializationReader reader, object context);
    }
}