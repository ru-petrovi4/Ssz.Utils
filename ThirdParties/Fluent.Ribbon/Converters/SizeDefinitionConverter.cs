namespace Fluent.Converters
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    /// <summary>
    /// Class which enables conversion from <see cref="string"/> to <see cref="RibbonControlSizeDefinition"/>
    /// </summary>
    public class SizeDefinitionConverter : TypeConverter
    {
        /// <inheritdoc />
#pragma warning disable CS8765 // Nullability of type of parameter 'context' doesn't match overridden member (possibly because of nullability attributes).
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
#pragma warning restore CS8765 // Nullability of type of parameter 'context' doesn't match overridden member (possibly because of nullability attributes).
        {
            return sourceType.IsAssignableFrom(typeof(string));
        }

        /// <inheritdoc />
#pragma warning disable CS8765 // Nullability of type of parameter 'culture' doesn't match overridden member (possibly because of nullability attributes).
#pragma warning disable CS8765 // Nullability of type of parameter 'context' doesn't match overridden member (possibly because of nullability attributes).
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
#pragma warning restore CS8765 // Nullability of type of parameter 'context' doesn't match overridden member (possibly because of nullability attributes).
#pragma warning restore CS8765 // Nullability of type of parameter 'culture' doesn't match overridden member (possibly because of nullability attributes).
        {
            return new RibbonControlSizeDefinition(value as string);
        }
    }
}