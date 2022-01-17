#nullable enable
using System;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Ssz.Utils.Wpf.Converters
{
    public class NullToObjectConverter : DependencyObject, IValueConverter
    {
        #region public functions

        public static readonly DependencyProperty OnNullProperty =
            DependencyProperty.Register("OnNull", typeof(object), typeof(NullToObjectConverter),
                new PropertyMetadata(default(object)));

        public static readonly DependencyProperty OnNotNullProperty =
            DependencyProperty.Register("OnNotNull", typeof(object), typeof(NullToObjectConverter),
                new PropertyMetadata(default(object)));

        public object OnNull
        {
            get { return GetValue(OnNullProperty); }
            set { SetValue(OnNullProperty, value); }
        }

        public object OnNotNull
        {
            get { return GetValue(OnNotNullProperty); }
            set { SetValue(OnNotNullProperty, value); }
        }

        public object? Convert(object? value, Type? targetType, object? parameter, string language)
        {
            return value is null ? OnNull : OnNotNull;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, string language)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}