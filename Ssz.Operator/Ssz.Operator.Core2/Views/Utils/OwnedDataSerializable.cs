#nullable disable

using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Utils
{
    /// <summary>
    ///     Abstract base class allows to save/retrieve their internal data to/from an existing
    ///     SerializationWriter/SerializationReader.
    ///     Implemented interface which specify that class can be recreated during deserialization using a default
    ///     constructor and then calling DeserializeOwnedData()
    /// </summary>
    [Serializable]
    public abstract class OwnedDataSerializable : IOwnedDataSerializable
    {
        #region public functions

        /// <summary>
        ///     Store internal data directly into a SerializationWriter.
        /// </summary>
        /// <param name="writer"> The SerializationWriter to use </param>
        /// <param name="context"> Optional context to use as a hint as to what to store (BitVector32 is useful) </param>
        public virtual void SerializeOwnedData(SerializationWriter writer, object context)
        {
            using (writer.EnterBlock(1))
            {
                string s = JsonSerializer.Serialize(this, GetType(), context as JsonSerializerOptions);
                writer.Write(s);
            }
            //using (writer.EnterBlock(1))
            //{
            //    var formatter = new BinaryFormatter
            //    {
            //        AssemblyFormat = FormatterAssemblyStyle.Simple
            //    };
            //    formatter.Serialize(writer.BaseStream, this);
            //}
        }

        /// <summary>
        ///     Retrieve internal data directly from a SerializationReader.
        /// </summary>
        /// <param name="reader"> The SerializationReader to use </param>
        /// <param name="context"> Optional context to use as a hint as to what to retrieve (BitVector32 is useful) </param>
        public virtual void DeserializeOwnedDataAsync(SerializationReader reader, object context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        string s = reader.ReadString();
                        object temp = JsonSerializer.Deserialize(s, GetType(), context as JsonSerializerOptions);
                        if (temp == null) throw new InvalidOperationException();
                        // TODO
                        //MemberInfo[] members = FormatterServices.GetSerializableMembers(GetType());
                        //FormatterServices.PopulateObjectMembers(this, members,
                        //    FormatterServices.GetObjectData(temp, members));
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }

            //using (Block block = reader.EnterBlock())
            //{
            //    switch (block.Version)
            //    {
            //        case 1:
            //        {
            //            object temp = (new BinaryFormatter()).Deserialize(reader.BaseStream);
            //            MemberInfo[] members = FormatterServices.GetSerializableMembers(GetType());
            //            FormatterServices.PopulateObjectMembers(this, members,
            //                FormatterServices.GetObjectData(temp, members));
            //        }
            //            break;
            //        default:
            //            throw new BlockUnsupportedVersionException();
            //    }
            //}
        }

        #endregion
    }
}