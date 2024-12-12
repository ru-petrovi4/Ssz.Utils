using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ssz.Operator.Core.MultiValueConverters
{
    public class VisibilityConverter : IMultiValueConverter, IDisposable
    {
        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region construction and destruction

        public VisibilityConverter(ValueConverterBase converter, Visibility valueWhenInvisible)
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

        public object? Convert(object?[]? values, Type? targetType, object? parameter, CultureInfo culture)
        {
            var convertedValue = Converter.Convert(values, typeof(bool), parameter, culture);
            if (convertedValue is bool) return (bool) convertedValue ? Visibility.Visible : ValueWhenInvisible;
            return Binding.DoNothing;
        }

        public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


        public ValueConverterBase Converter { get; }

        public Visibility ValueWhenInvisible { get; }

        #endregion
    }
}