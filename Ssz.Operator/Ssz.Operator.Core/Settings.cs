using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Markup;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    [TypeConverter(typeof(SettingsTypeConverter))]
    [ValueSerializer(typeof(SettingsValueSerializer))]
    public class Settings : IOwnedDataSerializable
    {
        #region private fields

        private CaseInsensitiveOrderedDictionary<string?> _settings = new();

        #endregion

        public class SettingsValueSerializer : ValueSerializer
        {
            #region public functions

            public static readonly SettingsValueSerializer Instance =
                new();

            public override bool CanConvertFromString(string value, IValueSerializerContext? context)
            {
                return true;
            }

            public override object? ConvertFromString(string value, IValueSerializerContext? context)
            {
                var result = new Settings();
                if (string.IsNullOrWhiteSpace(value)) return result;
                result._settings = NameValueCollectionHelper.Parse(value);
                return result;
            }

            public override bool CanConvertToString(object value, IValueSerializerContext? context)
            {
                return true;
            }

            public override string ConvertToString(object value, IValueSerializerContext? context)
            {
                return
                    NameValueCollectionHelper.GetNameValueCollectionString(
                        ((Settings) value)._settings);
            }

            #endregion
        }

        public class SettingsTypeConverter : TypeConverter
        {
            #region public functions

            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                if (sourceType == typeof(string)) return true;

                return base.CanConvertFrom(context, sourceType);
            }


            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is null) throw GetConvertFromException(null);

                var source = value as string;

                if (source is not null) return SettingsValueSerializer.Instance.ConvertFromString(source, null);

                return base.ConvertFrom(context, culture, value);
            }

            #endregion
        }

        #region public functions

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(_settings.Count);
                foreach (var kvp in _settings)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        _settings.Clear();
                        var count = reader.ReadInt32();
                        for (var i = 0; i < count; i += 1)
                        {
                            string key = reader.ReadString();
                            string value = reader.ReadString();
                            _settings[key] = value;
                        }

                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void SetValue(string key, string value)
        {
            _settings[key] = value;
        }

        public string? GetValue(string key, string defaultValue)
        {
            string? result;
            if (!_settings.TryGetValue(key, out result))
                return defaultValue;
            return result;
        }

        public void Clear()
        {
            _settings.Clear();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as Settings;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (_settings.Count == 0 && other._settings.Count == 0) return true;

            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        #endregion
    }
}