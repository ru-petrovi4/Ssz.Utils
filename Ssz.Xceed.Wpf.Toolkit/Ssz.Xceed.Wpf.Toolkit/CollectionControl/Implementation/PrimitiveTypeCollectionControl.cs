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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class PrimitiveTypeCollectionControl : ContentControl
    {
        #region Members

        private bool _surpressTextChanged;
        private bool _conversionFailed;

        #endregion //Members

        #region Properties

        #region IsOpen

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool),
            typeof(PrimitiveTypeCollectionControl), new UIPropertyMetadata(false, OnIsOpenChanged));

        public bool IsOpen
        {
            get => (bool) GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void OnIsOpenChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var primitiveTypeCollectionControl = o as PrimitiveTypeCollectionControl;
            if (primitiveTypeCollectionControl != null)
                primitiveTypeCollectionControl.OnIsOpenChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIsOpenChanged(bool oldValue, bool newValue)
        {
        }

        #endregion //IsOpen

        #region ItemsSource

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource",
            typeof(IList), typeof(PrimitiveTypeCollectionControl), new UIPropertyMetadata(null, OnItemsSourceChanged));

        public IList ItemsSource
        {
            get => (IList) GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var primitiveTypeCollectionControl = o as PrimitiveTypeCollectionControl;
            if (primitiveTypeCollectionControl != null)
                primitiveTypeCollectionControl.OnItemsSourceChanged((IList) e.OldValue, (IList) e.NewValue);
        }

        protected virtual void OnItemsSourceChanged(IList oldValue, IList newValue)
        {
            if (newValue == null)
                return;

            if (ItemsSourceType == null)
                ItemsSourceType = newValue.GetType();

            if (ItemType == null)
                ItemType = newValue.GetType().GetGenericArguments()[0];

            SetText(newValue);
        }

        #endregion //ItemsSource

        #region IsReadOnly

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PrimitiveTypeCollectionControl),
                new UIPropertyMetadata(false));

        public bool IsReadOnly
        {
            get => (bool) GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion //IsReadOnly

        #region ItemsSourceType

        public static readonly DependencyProperty ItemsSourceTypeProperty =
            DependencyProperty.Register("ItemsSourceType", typeof(Type), typeof(PrimitiveTypeCollectionControl),
                new UIPropertyMetadata(null));

        public Type ItemsSourceType
        {
            get => (Type) GetValue(ItemsSourceTypeProperty);
            set => SetValue(ItemsSourceTypeProperty, value);
        }

        #endregion ItemsSourceType

        #region ItemType

        public static readonly DependencyProperty ItemTypeProperty = DependencyProperty.Register("ItemType",
            typeof(Type), typeof(PrimitiveTypeCollectionControl), new UIPropertyMetadata(null));

        public Type ItemType
        {
            get => (Type) GetValue(ItemTypeProperty);
            set => SetValue(ItemTypeProperty, value);
        }

        #endregion ItemType

        #region Text

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string),
            typeof(PrimitiveTypeCollectionControl), new UIPropertyMetadata(null, OnTextChanged));

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var primitiveTypeCollectionControl = o as PrimitiveTypeCollectionControl;
            if (primitiveTypeCollectionControl != null)
                primitiveTypeCollectionControl.OnTextChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnTextChanged(string oldValue, string newValue)
        {
            if (!_surpressTextChanged)
                PersistChanges();
        }

        #endregion //Text

        #endregion //Properties

        #region Constructors

        static PrimitiveTypeCollectionControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PrimitiveTypeCollectionControl),
                new FrameworkPropertyMetadata(typeof(PrimitiveTypeCollectionControl)));
        }

        #endregion //Constructors

        #region Methods

        private void PersistChanges()
        {
            var list = ComputeItemsSource();
            if (list == null)
                return;

            //the easiest way to persist changes to the source is to just clear the source list and then add all items to it.
            list.Clear();

            var items = ComputeItems();
            foreach (var item in items) list.Add(item);
            ;

            // if something went wrong during conversion we want to reload the text to show only valid entries
            if (_conversionFailed)
                SetText(list);
        }

        private IList ComputeItems()
        {
            IList items = new List<object>();

            if (ItemType == null)
                return items;

            var textArray = Text.Split('\n');

            foreach (var s in textArray)
            {
                var valueString = s.TrimEnd('\r');
                if (!string.IsNullOrEmpty(valueString))
                {
                    object value = null;
                    try
                    {
                        value = Convert.ChangeType(valueString, ItemType);
                    }
                    catch
                    {
                        //a conversion failed
                        _conversionFailed = true;
                    }

                    if (value != null)
                        items.Add(value);
                }
            }

            return items;
        }

        private IList ComputeItemsSource()
        {
            if (ItemsSource == null)
                ItemsSource = CreateItemsSource();

            return ItemsSource;
        }

        private IList CreateItemsSource()
        {
            IList list = null;

            if (ItemsSourceType != null)
            {
                var constructor = ItemsSourceType.GetConstructor(Type.EmptyTypes);
                list = (IList) constructor.Invoke(null);
            }

            return list;
        }

        private void SetText(IEnumerable collection)
        {
            _surpressTextChanged = true;
            var builder = new StringBuilder();
            foreach (var obj2 in collection)
            {
                builder.Append(obj2);
                builder.AppendLine();
            }

            Text = builder.ToString().Trim();
            _surpressTextChanged = false;
        }

        #endregion //Methods
    }
}