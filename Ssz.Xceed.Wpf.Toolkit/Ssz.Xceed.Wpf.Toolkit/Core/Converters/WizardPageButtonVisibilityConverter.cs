﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Converters
{
    public class WizardPageButtonVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is null || values.Length != 2)
                throw new ArgumentException("Wrong number of arguments for WizardPageButtonVisibilityConverter.");

            var wizardVisibility = values[0] is null || values[0] == DependencyProperty.UnsetValue
                ? Visibility.Hidden
                : (Visibility) values[0];

            var wizardPageVisibility = values[1] is null || values[1] == DependencyProperty.UnsetValue
                ? WizardPageButtonVisibility.Hidden
                : (WizardPageButtonVisibility) values[1];

            var visibility = Visibility.Visible;

            switch (wizardPageVisibility)
            {
                case WizardPageButtonVisibility.Inherit:
                    visibility = wizardVisibility;
                    break;
                case WizardPageButtonVisibility.Collapsed:
                    visibility = Visibility.Collapsed;
                    break;
                case WizardPageButtonVisibility.Hidden:
                    visibility = Visibility.Hidden;
                    break;
                case WizardPageButtonVisibility.Visible:
                    visibility = Visibility.Visible;
                    break;
            }

            return visibility;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}