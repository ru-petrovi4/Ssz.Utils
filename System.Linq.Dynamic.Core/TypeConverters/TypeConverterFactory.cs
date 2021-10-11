
using System.ComponentModel;
using System.Linq.Dynamic.Core.Parser;
using System.Linq.Dynamic.Core.Validation;

namespace System.Linq.Dynamic.Core.TypeConverters
{
    internal class TypeConverterFactory : ITypeConverterFactory
    {
        private readonly ParsingConfig _config;

        public TypeConverterFactory(ParsingConfig config)
        {            
            _config = config;
        }

        /// <see cref="ITypeConverterFactory.GetConverter"/>
        public TypeConverter GetConverter(Type type)
        {
            Check.NotNull(type, nameof(type));

            if (_config.DateTimeIsParsedAsUTC && (type == typeof(DateTime) || type == typeof(DateTime?)))
            {
                return new CustomDateTimeConverter();
            }

            var typeToCheck = TypeHelper.IsNullableType(type) ? TypeHelper.GetNonNullableType(type) : type;
            if (_config.TypeConverters != null && _config.TypeConverters.TryGetValue(typeToCheck, out var typeConverter))
            {
                return typeConverter;
            }

            return TypeDescriptor.GetConverter(type);
        }
    }
}
