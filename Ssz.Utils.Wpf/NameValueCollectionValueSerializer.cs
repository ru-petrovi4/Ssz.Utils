using System;
using System.Windows.Markup;

namespace Ssz.Utils.Wpf
{
    public class NameValueCollectionValueSerializer<T> : ValueSerializer
        where T : notnull, new()
    {
        #region public functions

        public static readonly NameValueCollectionValueSerializer<T> Instance =
            new NameValueCollectionValueSerializer<T>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override object ConvertFromString(string value, IValueSerializerContext context)
        { 
            return ConvertFromString(value);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public object ConvertFromString(string value)
        {
            object result = Activator.CreateInstance<T>();
            NameValueCollectionHelper.SetNameValueCollection(ref result, NameValueCollectionHelper.Parse(value));
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            return NameValueCollectionHelper.CanGetNameValueCollection(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            return ConvertToString(value);
        }

        public string ConvertToString(object value)
        {
            return
                NameValueCollectionHelper.GetNameValueCollectionString(
                    NameValueCollectionHelper.GetNameValueCollectionFromObject(value));
        }

        #endregion
    }
}