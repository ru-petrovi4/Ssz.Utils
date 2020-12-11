using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;


namespace Ssz.Utils.CommandLine.Parsing
{
    /// <summary>
    ///     Encapsulates property writing primitives.
    /// </summary>
    internal sealed class PropertyWriter
    {
        #region construction and destruction

        public PropertyWriter(PropertyInfo property, CultureInfo parsingCulture)
        {
            _parsingCulture = parsingCulture;
            Property = property;
        }

        #endregion

        #region public functions

        public bool WriteScalar(string value, object target)
        {
            try
            {
                object? propertyValue = null;
                if (Property.PropertyType.IsEnum)
                {
                    propertyValue = Enum.Parse(Property.PropertyType, value, true);
                }
                else
                {
                    propertyValue = Convert.ChangeType(value, Property.PropertyType, _parsingCulture);
                }

                Property.SetValue(target, propertyValue, null);
            }
            catch (InvalidCastException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }

            return true;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification =
                "FormatException (thrown by ConvertFromString) is thrown as Exception.InnerException, so we've to catch directly System.Exception"
            )]
        public bool WriteNullable(string value, object target)
        {
            var nc = new NullableConverter(Property.PropertyType);

            // FormatException (thrown by ConvertFromString) is thrown as Exception.InnerException, so we've to catch directly System.Exception
            try
            {
                Property.SetValue(target, nc.ConvertFromString(null, _parsingCulture, value), null);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public PropertyInfo Property { get; private set; }

        #endregion

        #region private fields

        private readonly CultureInfo _parsingCulture;

        #endregion
    }
}