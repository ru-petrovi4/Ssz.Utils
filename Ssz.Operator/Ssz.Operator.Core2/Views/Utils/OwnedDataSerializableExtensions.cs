#nullable disable

using System;
using System.IO;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Utils
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
                if (func == null)
                {
                    clone = (T)Activator.CreateInstance(obj.GetType());
                }
                else
                {
                    clone = func();
                }                
                using (var reader = new SerializationReader(memoryStream))
                {
                    clone.DeserializeOwnedDataAsync(reader, null);
                }
                return clone;
            }
        }

        #endregion
    }
}