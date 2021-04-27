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
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Utilities
{
  /// <summary>
  ///     This helper class will raise events when a specific
  ///     path value on one or many items changes.
  /// </summary>
  internal class ValueChangeHelper : DependencyObject
    {
        #region Constructor

        public ValueChangeHelper(Action changeCallback)
        {
            if (changeCallback == null)
                throw new ArgumentNullException("changeCallback");

            ValueChanged += (s, args) => changeCallback();
        }

        #endregion

        public event EventHandler ValueChanged;

        #region BlankMultiValueConverter private class

        private class BlankMultiValueConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                // We will not use the result anyway. We just want the change notification to kick in.
                // Return a new object to have a different value.
                return new();
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new InvalidOperationException();
            }
        }

        #endregion

        #region Value Property

        /// <summary>
        ///     This private property serves as the target of a binding that monitors the value of the binding
        ///     of each item in the source.
        /// </summary>
        private static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object),
            typeof(ValueChangeHelper), new UIPropertyMetadata(null, OnValueChanged));

        private object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((ValueChangeHelper) sender).RaiseValueChanged();
        }

        #endregion

        #region Methods

        public void UpdateValueSource(object sourceItem, string path)
        {
            BindingBase binding = null;
            if (sourceItem != null && path != null) binding = new Binding(path) {Source = sourceItem};

            UpdateBinding(binding);
        }

        public void UpdateValueSource(IEnumerable sourceItems, string path)
        {
            BindingBase binding = null;
            if (sourceItems != null && path != null)
            {
                var multiBinding = new MultiBinding();
                multiBinding.Converter = new BlankMultiValueConverter();

                foreach (var item in sourceItems) multiBinding.Bindings.Add(new Binding(path) {Source = item});

                binding = multiBinding;
            }

            UpdateBinding(binding);
        }

        private void UpdateBinding(BindingBase binding)
        {
            if (binding != null)
                BindingOperations.SetBinding(this, ValueProperty, binding);
            else
                ClearBinding();
        }

        private void ClearBinding()
        {
            BindingOperations.ClearBinding(this, ValueProperty);
        }

        private void RaiseValueChanged()
        {
            if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
        }

        #endregion
    }
}