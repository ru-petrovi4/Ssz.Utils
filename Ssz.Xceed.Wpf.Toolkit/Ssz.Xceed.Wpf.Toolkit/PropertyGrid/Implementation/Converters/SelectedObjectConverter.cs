/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Converters
{
    public class SelectedObjectConverter : IValueConverter
    {
        private const string ValidParameterMessage =
            @"parameter must be one of the following strings: 'Type', 'TypeName', 'SelectedObjectName'";

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                throw new ArgumentNullException("parameter");

            if (!(parameter is string))
                throw new ArgumentException(ValidParameterMessage);

            if (CompareParam(parameter, "Type"))
                return ConvertToType(value, culture);
            if (CompareParam(parameter, "TypeName"))
                return ConvertToTypeName(value, culture);
            if (CompareParam(parameter, "SelectedObjectName"))
                return ConvertToSelectedObjectName(value, culture);
            throw new ArgumentException(ValidParameterMessage);
        }

        private bool CompareParam(object parameter, string parameterValue)
        {
            return string.Compare((string) parameter, parameterValue, true) == 0;
        }

        private object ConvertToType(object value, CultureInfo culture)
        {
            return value != null
                ? value.GetType()
                : null;
        }

        private object ConvertToTypeName(object value, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var newType = value.GetType();

            var displayNameAttribute =
                newType.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();

            return displayNameAttribute == null
                ? newType.Name
                : displayNameAttribute.DisplayName;
        }

        private object ConvertToSelectedObjectName(object value, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            var newType = value.GetType();
            var properties = newType.GetProperties();
            foreach (var property in properties)
                if (property.Name == "Name")
                    return property.GetValue(value, null);

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}