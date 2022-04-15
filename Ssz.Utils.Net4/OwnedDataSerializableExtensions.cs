using System;
using System.IO;
using Ssz.Utils.Net4.Serialization;

namespace Ssz.Utils.Net4
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
        public static T CloneUsingSerialization<T>(this T obj, Func<T> func = null)
            where T : IOwnedDataSerializable, ICloneable
        {
            using (var memoryStream = new MemoryStream(1024))
            {
                using(var writer = new SerializationWriter(memoryStream))
                {
                    obj.SerializeOwnedData(writer, null);
                }
                memoryStream.Position = 0;
                T clone;
                if (func is null)
                {
                    clone = (T)Activator.CreateInstance(obj.GetType());
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