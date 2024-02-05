using System;
using System.IO;
using CommunityToolkit.HighPerformance;
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
                if (func is null)
                {
                    clone = Activator.CreateInstance(ownedDataSerializable.GetType()) as T;
                    if (clone is null) throw new InvalidOperationException();
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

        public static void SetOwnedData(IOwnedDataSerializable ownedDataSerializable, ReadOnlyMemory<byte> ownedData)            
        {
            if (ownedData.Length == 0)
                return;

            using (var stream = ownedData.AsStream())
            using (var reader = new SerializationReader(stream))
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
        public static T CreateFromOwnedData<T>(ReadOnlyMemory<byte> ownedData, Func<T>? func = null)
            where T : class, IOwnedDataSerializable
        {            
            T? result;
            if (func is null)
            {
                result = Activator.CreateInstance(typeof (T)) as T;
                if (result is null) 
                    throw new InvalidOperationException();
            }
            else
            {
                result = func();
            }
            using (var stream = ownedData.AsStream())
            using (var reader = new SerializationReader(stream))
            {
                result.DeserializeOwnedData(reader, null);
            }
            return result;
        }        

        #endregion
    }
}