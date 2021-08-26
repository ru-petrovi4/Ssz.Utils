using System;
using System.IO;
using Ssz.Utils.Serialization;

namespace Ssz.Utils
{
    public static class OwnedDataSerializableExtensions
    {
        #region public functions

        /// <summary>
        ///     func is new object creator function, otherwise default constructor is used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static T CloneUsingSerialization<T>(this T obj, Func<T>? func = null)
            where T : class, IOwnedDataSerializable
        {
            using (var memoryStream = new MemoryStream(1024))
            {
                using(var writer = new SerializationWriter(memoryStream))
                {
                    obj.SerializeOwnedData(writer, null);
                }
                memoryStream.Position = 0;
                T? clone;
                if (func == null)
                {
                    clone = Activator.CreateInstance(obj.GetType()) as T;
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

        #endregion
    }
}