/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Data;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Utilities
{
    internal sealed class GeneralUtilities : DependencyObject
    {
        private GeneralUtilities()
        {
        }

        public static object GetPathValue(object sourceObject, string path)
        {
            var targetObj = new GeneralUtilities();
            BindingOperations.SetBinding(targetObj, StubValueProperty, new Binding(path) {Source = sourceObject});
            var value = GetStubValue(targetObj);
            BindingOperations.ClearBinding(targetObj, StubValueProperty);
            return value;
        }

        public static object GetBindingValue(object sourceObject, Binding binding)
        {
            var bindingClone = new Binding
            {
                BindsDirectlyToSource = binding.BindsDirectlyToSource,
                Converter = binding.Converter,
                ConverterCulture = binding.ConverterCulture,
                ConverterParameter = binding.ConverterParameter,
                FallbackValue = binding.FallbackValue,
                Mode = BindingMode.OneTime,
                Path = binding.Path,
                StringFormat = binding.StringFormat,
                TargetNullValue = binding.TargetNullValue,
                XPath = binding.XPath
            };

            bindingClone.Source = sourceObject;

            var targetObj = new GeneralUtilities();
            BindingOperations.SetBinding(targetObj, StubValueProperty, bindingClone);
            var value = GetStubValue(targetObj);
            BindingOperations.ClearBinding(targetObj, StubValueProperty);
            return value;
        }

        internal static bool CanConvertValue(object value, object targetType)
        {
            return value is not null
                   && !Equals(value.GetType(), targetType)
                   && !Equals(targetType, typeof(object));
        }

        #region StubValue attached property

        internal static readonly DependencyProperty StubValueProperty = DependencyProperty.RegisterAttached(
            "StubValue",
            typeof(object),
            typeof(GeneralUtilities),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        internal static object GetStubValue(DependencyObject obj)
        {
            return obj.GetValue(StubValueProperty);
        }

        internal static void SetStubValue(DependencyObject obj, object value)
        {
            obj.SetValue(StubValueProperty, value);
        }

        #endregion StubValue attached property
    }
}