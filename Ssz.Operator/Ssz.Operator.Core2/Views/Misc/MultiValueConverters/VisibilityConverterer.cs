using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Ssz.Operator.Core.MultiValueConverters
{
    public class VisibilityConverter : IMultiValueConverter, IDisposable
    {
        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region construction and destruction

        public VisibilityConverter(ValueConverterBase converter, bool valueWhenInvisible)
        {
            Converter = converter;
            ValueWhenInvisible = valueWhenInvisible;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing) Converter.Dispose();

            Disposed = true;
        }


        ~VisibilityConverter()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var convertedValue = Converter.Convert(values, typeof(bool), parameter, culture);
            if (convertedValue is bool convertedValueBool) 
                return convertedValueBool ? true : ValueWhenInvisible;
            return BindingOperations.DoNothing;
        }        

        public ValueConverterBase Converter { get; }

        public bool ValueWhenInvisible { get; }

        #endregion
    }
}