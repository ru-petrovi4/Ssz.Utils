using System;
using System.Windows.Markup;

namespace Ssz.Utils.Net4
{
    public class NameValueCollectionValueSerializer<T> : ValueSerializer
    {
        #region public functions

        public static readonly NameValueCollectionValueSerializer<T> Instance =
            new NameValueCollectionValueSerializer<T>();

        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }

        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            if (String.IsNullOrWhiteSpace(value)) return default(T);
            var result = Activator.CreateInstance<T>();
            NameValueCollectionHelper.SetNameValueCollection(result, NameValueCollectionHelper.Parse(value));
            return result;
        }

        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            return NameValueCollectionHelper.CanGetNameValueCollection(value);
        }

        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            return
                NameValueCollectionHelper.GetNameValueCollectionString(
                    NameValueCollectionHelper.GetNameValueCollection(value));
        }

        #endregion
    }
}