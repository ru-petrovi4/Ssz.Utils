using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Ssz.Utils.Wpf.Converters
{
    public class BooleanToObjectConverter : DependencyObject, IValueConverter
    {
        #region public functions
        
        public static readonly DependencyProperty OnTrueProperty =
            DependencyProperty.Register("OnTrue", typeof (object), typeof (BooleanToObjectConverter),
                new PropertyMetadata(default(object)));

        public static readonly DependencyProperty OnFalseProperty =
            DependencyProperty.Register("OnFalse", typeof (object), typeof (BooleanToObjectConverter),
                new PropertyMetadata(default(object)));

        public static readonly DependencyProperty OnNullProperty =
            DependencyProperty.Register("OnNull", typeof (object), typeof (BooleanToObjectConverter),
                new PropertyMetadata(default(object)));

        public object OnTrue
        {
            get { return GetValue(OnTrueProperty); }
            set { SetValue(OnTrueProperty, value); }
        }

        public object OnFalse
        {
            get { return GetValue(OnFalseProperty); }
            set { SetValue(OnFalseProperty, value); }
        }

        public object OnNull
        {
            get { return GetValue(OnNullProperty); }
            set { SetValue(OnNullProperty, value); }
        }

        public object? Convert(object? value, Type? targetType, object? parameter, string language)
        {
            if (value is null) return OnNull;            
            var boolValue = new Any(value).ValueAsBoolean(false);
            if (boolValue)
                return OnTrue;
            else
                return OnFalse;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, string language)
        {
            if (value == OnNull) return Default(targetType);            
            if (value == OnFalse) return false;
            if (value == OnTrue) return true;
            if (value is null) return null;

            if (OnNull is not null &&
                string.Equals(value.ToString(), OnNull.ToString(), StringComparison.CurrentCultureIgnoreCase))
                return Default(targetType);

            if (OnFalse is not null &&
                string.Equals(value.ToString(), OnFalse.ToString(), StringComparison.CurrentCultureIgnoreCase))
                return false;

            if (OnTrue is not null &&
                string.Equals(value.ToString(), OnTrue.ToString(), StringComparison.CurrentCultureIgnoreCase))
                return true;

            return null;
        }

        #endregion

        #region private functions

        private static object? Default(Type? type)
        {
            if (type is null) return null;
            return type.IsByRef ? null : Activator.CreateInstance(type);
        }

        #endregion
    }
}