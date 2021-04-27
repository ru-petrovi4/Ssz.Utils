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
using System.Windows;
using System.Windows.Input;

namespace Ssz.Xceed.Wpf.Toolkit
{
#pragma warning disable 0618

    public class WatermarkTextBox : AutoSelectTextBox
    {
        #region Constructors

        static WatermarkTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WatermarkTextBox),
                new FrameworkPropertyMetadata(typeof(WatermarkTextBox)));
        }

        #endregion //Constructors

        #region Properties

        #region SelectAllOnGotFocus (Obsolete)

        [Obsolete(
            "This property is obsolete and should no longer be used. Use AutoSelectTextBox.AutoSelectBehavior instead.")]
        public static readonly DependencyProperty SelectAllOnGotFocusProperty =
            DependencyProperty.Register("SelectAllOnGotFocus", typeof(bool), typeof(WatermarkTextBox),
                new PropertyMetadata(false));

        [Obsolete(
            "This property is obsolete and should no longer be used. Use AutoSelectTextBox.AutoSelectBehavior instead.")]
        public bool SelectAllOnGotFocus
        {
            get => (bool) GetValue(SelectAllOnGotFocusProperty);
            set => SetValue(SelectAllOnGotFocusProperty, value);
        }

        #endregion //SelectAllOnGotFocus

        #region Watermark

        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register("Watermark",
            typeof(object), typeof(WatermarkTextBox), new UIPropertyMetadata(null));

        public object Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        #endregion //Watermark

        #region WatermarkTemplate

        public static readonly DependencyProperty WatermarkTemplateProperty =
            DependencyProperty.Register("WatermarkTemplate", typeof(DataTemplate), typeof(WatermarkTextBox),
                new UIPropertyMetadata(null));

        public DataTemplate WatermarkTemplate
        {
            get => (DataTemplate) GetValue(WatermarkTemplateProperty);
            set => SetValue(WatermarkTemplateProperty, value);
        }

        #endregion //WatermarkTemplate

        #endregion //Properties

        #region Base Class Overrides

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            if (SelectAllOnGotFocus)
                SelectAll();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsKeyboardFocused && SelectAllOnGotFocus)
            {
                e.Handled = true;
                Focus();
            }

            base.OnPreviewMouseLeftButtonDown(e);
        }

        #endregion //Base Class Overrides
    }

#pragma warning restore 0618
}