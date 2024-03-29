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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;
using Selector = Ssz.Xceed.Wpf.Toolkit.Primitives.Selector;

namespace Ssz.Xceed.Wpf.Toolkit
{
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    public class CheckComboBox : Selector
    {
        private const string PART_Popup = "PART_Popup";

        #region Members

        private readonly ValueChangeHelper _displayMemberPathValuesChangeHelper;
        private Popup _popup;
        private readonly List<object> _initialValue = new();

        #endregion

        #region Constructors

        static CheckComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CheckComboBox),
                new FrameworkPropertyMetadata(typeof(CheckComboBox)));
        }

        public CheckComboBox()
        {
            Keyboard.AddKeyDownHandler(this, OnKeyDown);
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseDownOutsideCapturedElement);
            _displayMemberPathValuesChangeHelper = new ValueChangeHelper(OnDisplayMemberPathValuesChanged);
        }

        #endregion //Constructors

        #region Properties

        #region Text

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string),
            typeof(CheckComboBox), new UIPropertyMetadata(null));

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        #endregion

        #region IsDropDownOpen

        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register("IsDropDownOpen",
            typeof(bool), typeof(CheckComboBox), new UIPropertyMetadata(false, OnIsDropDownOpenChanged));

        public bool IsDropDownOpen
        {
            get => (bool) GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        private static void OnIsDropDownOpenChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var comboBox = o as CheckComboBox;
            if (comboBox is not null)
                comboBox.OnIsDropDownOpenChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIsDropDownOpenChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                _initialValue.Clear();
                foreach (var o in SelectedItems)
                    _initialValue.Add(o);
            }
            else
            {
                _initialValue.Clear();
            }

            // TODO: Add your property changed side-effects. Descendants can override as well.
        }

        #endregion //IsDropDownOpen

        #region MaxDropDownHeight

        public static readonly DependencyProperty MaxDropDownHeightProperty =
            DependencyProperty.Register("MaxDropDownHeight", typeof(double), typeof(CheckComboBox),
                new UIPropertyMetadata(SystemParameters.PrimaryScreenHeight / 3.0, OnMaxDropDownHeightChanged));

        public double MaxDropDownHeight
        {
            get => (double) GetValue(MaxDropDownHeightProperty);
            set => SetValue(MaxDropDownHeightProperty, value);
        }

        private static void OnMaxDropDownHeightChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var comboBox = o as CheckComboBox;
            if (comboBox is not null)
                comboBox.OnMaxDropDownHeightChanged((double) e.OldValue, (double) e.NewValue);
        }

        protected virtual void OnMaxDropDownHeightChanged(double oldValue, double newValue)
        {
            // TODO: Add your property changed side-effects. Descendants can override as well.
        }

        #endregion

        #endregion //Properties

        #region Base Class Overrides

        protected override void OnSelectedValueChanged(string oldValue, string newValue)
        {
            base.OnSelectedValueChanged(oldValue, newValue);
            UpdateText();
        }

        protected override void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
            base.OnDisplayMemberPathChanged(oldDisplayMemberPath, newDisplayMemberPath);
            UpdateDisplayMemberPathValuesBindings();
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            UpdateDisplayMemberPathValuesBindings();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_popup is not null)
                _popup.Opened -= Popup_Opened;

            _popup = GetTemplateChild(PART_Popup) as Popup;

            if (_popup is not null)
                _popup.Opened += Popup_Opened;
        }

        #endregion //Base Class Overrides

        #region Event Handlers

        private void OnMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            CloseDropDown(false);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsDropDownOpen)
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    IsDropDownOpen = true;
                    // Popup_Opened() will Focus on ComboBoxItem.
                    e.Handled = true;
                }
            }
            else
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    CloseDropDown(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    CloseDropDown(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    SelectedItems.Clear();
                    foreach (var o in _initialValue)
                        SelectedItems.Add(o);
                    CloseDropDown(true);
                    e.Handled = true;
                }
            }
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            var item = ItemContainerGenerator.ContainerFromItem(SelectedItem) as UIElement;
            if (item is null && Items.Count > 0)
                item = ItemContainerGenerator.ContainerFromItem(Items[0]) as UIElement;
            if (item is not null)
                item.Focus();
        }

        #endregion //Event Handlers

        #region Methods

        private void UpdateDisplayMemberPathValuesBindings()
        {
            _displayMemberPathValuesChangeHelper.UpdateValueSource(ItemsCollection, DisplayMemberPath);
        }

        private void OnDisplayMemberPathValuesChanged()
        {
            UpdateText();
        }

        private void UpdateText()
        {
#if VS2008
      string newValue =
 String.Join( Delimiter, SelectedItems.Cast<object>().Select( x => GetItemDisplayValue( x ).ToString() ).ToArray() );
#else
            var newValue = string.Join(Delimiter, SelectedItems.Cast<object>().Select(x => GetItemDisplayValue(x)));
#endif

            if (string.IsNullOrEmpty(Text) || !Text.Equals(newValue))
                Text = newValue;
        }

        protected object GetItemDisplayValue(object item)
        {
            if (!string.IsNullOrEmpty(DisplayMemberPath))
            {
                var property = item.GetType().GetProperty(DisplayMemberPath);
                if (property is not null)
                    return property.GetValue(item, null);
            }

            return item;
        }

        private void CloseDropDown(bool isFocusOnComboBox)
        {
            if (IsDropDownOpen)
                IsDropDownOpen = false;
            ReleaseMouseCapture();

            if (isFocusOnComboBox)
                Focus();
        }

        #endregion //Methods
    }
}