using System.Globalization;


namespace System.Linq.Dynamic.Core.Parser
{
    /// <summary>
    /// NumberParser
    /// </summary>
    public class NumberParser
    {
        private readonly CultureInfo _culture;

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberParser"/> class.
        /// </summary>
        /// <param name="config">The ParsingConfig.</param>
        public NumberParser(ParsingConfig? config)
        {
            _culture = config?.NumberParseCulture ?? CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Tries to parse the number (text) into the specified type.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="type">The type.</param>
        /// <param name="result">The result.</param>
        public bool TryParseNumber(string text, Type type, out object? result)
        {
            result = ParseNumber(text, type);
            return result != null;
        }

        /// <summary>
        /// Parses the number (text) into the specified type.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="type">The type.</param>
        public object? ParseNumber(string text, Type type)
        {
            try
            {
                switch (Type.GetTypeCode(TypeHelper.GetNonNullableType(type)))
                {
                    case TypeCode.SByte:
                        return sbyte.Parse(text, _culture);
                    case TypeCode.Byte:
                        return byte.Parse(text, _culture);
                    case TypeCode.Int16:
                        return short.Parse(text, _culture);
                    case TypeCode.UInt16:
                        return ushort.Parse(text, _culture);
                    case TypeCode.Int32:
                        return int.Parse(text, _culture);
                    case TypeCode.UInt32:
                        return uint.Parse(text, _culture);
                    case TypeCode.Int64:
                        return long.Parse(text, _culture);
                    case TypeCode.UInt64:
                        return ulong.Parse(text, _culture);
                    case TypeCode.Single:
                        return float.Parse(text, _culture);
                    case TypeCode.Double:
                        return double.Parse(text, _culture);
                    case TypeCode.Decimal:
                        return decimal.Parse(text, _culture);
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}
