using System;
using System.IO;
using Ssz.Utils.Serialization;

namespace Ssz.Utils.Serialization
{
    public static class SerializationHelper
    {
        #region public functions

        /// <summary>
        ///     func is new object creator function, otherwise default constructor is used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ownedDataSerializable"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T CloneUsingSerialization<T>(T ownedDataSerializable, Func<T>? func = null)
            where T : class, IOwnedDataSerializable
        {
            using (var memoryStream = new MemoryStream(1024))
            {
                using(var writer = new SerializationWriter(memoryStream))
                {
                    ownedDataSerializable.SerializeOwnedData(writer, null);
                }
                memoryStream.Position = 0;
                T? clone;
                if (func == null)
                {
                    clone = Activator.CreateInstance(ownedDataSerializable.GetType()) as T;
                    if (clone == null) throw new InvalidOperationException();
                }
                else
                {
                    clone = func();
                }
                using (var reader = new SerializationReader(memoryStream))
                {
                    clone.DeserializeOwnedData(reader, null);
                }
                return clone;
            }
        }

        public static byte[] GetOwnedData(IOwnedDataSerializable ownedDataSerializable, int streamCapacity = 1024)
        {
            using (var memoryStream = new MemoryStream(streamCapacity))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    ownedDataSerializable.SerializeOwnedData(writer, null);
                }
                return memoryStream.ToArray();
            }
        }

        public static void SetOwnedData(IOwnedDataSerializable ownedDataSerializable, byte[] ownedData)            
        {            
            using (var reader = new SerializationReader(ownedData))
            {
                ownedDataSerializable.DeserializeOwnedData(reader, null);
            }
        }

        /// <summary>
        ///     func is new object creator function, otherwise default constructor is used.
        ///     If ownedData is null, returns null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ownedData"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T? CreateFromOwnedData<T>(byte[]? ownedData, Func<T>? func = null)
            where T : class, IOwnedDataSerializable
        {
            if (ownedData == null) return null;

            T? result;
            if (func == null)
            {
                result = Activator.CreateInstance(typeof (T)) as T;
                if (result == null) throw new InvalidOperationException();
            }
            else
            {
                result = func();
            }
            using (var reader = new SerializationReader(ownedData))
            {
                result.DeserializeOwnedData(reader, null);
            }
            return result;
        }        

        #endregion
    }
}