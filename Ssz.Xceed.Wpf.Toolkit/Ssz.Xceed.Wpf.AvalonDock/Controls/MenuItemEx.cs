/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Controls;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class MenuItemEx : MenuItem
    {
        #region Members

        private bool _reentrantFlag;

        #endregion

        #region Constructors

        static MenuItemEx()
        {
            IconProperty.OverrideMetadata(typeof(MenuItemEx), new FrameworkPropertyMetadata(OnIconPropertyChanged));
        }

        #endregion

        #region Properties

        #region IconTemplate

        /// <summary>
        ///     IconTemplate Dependency Property
        /// </summary>
        public static readonly DependencyProperty IconTemplateProperty = DependencyProperty.Register("IconTemplate",
            typeof(DataTemplate), typeof(MenuItemEx),
            new FrameworkPropertyMetadata(null, OnIconTemplateChanged));

        /// <summary>
        ///     Gets or sets the IconTemplate property.  This dependency property
        ///     indicates the data template for the icon.
        /// </summary>
        public DataTemplate IconTemplate
        {
            get => (DataTemplate) GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
        }

        /// <summary>
        ///     Handles changes to the IconTemplate property.
        /// </summary>
        private static void OnIconTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MenuItemEx) d).OnIconTemplateChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the IconTemplate property.
        /// </summary>
        protected virtual void OnIconTemplateChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateIcon();
        }

        #endregion

        #region IconTemplateSelector

        /// <summary>
        ///     IconTemplateSelector Dependency Property
        /// </summary>
        public static readonly DependencyProperty IconTemplateSelectorProperty = DependencyProperty.Register(
            "IconTemplateSelector", typeof(DataTemplateSelector), typeof(MenuItemEx),
            new FrameworkPropertyMetadata(null, OnIconTemplateSelectorChanged));

        /// <summary>
        ///     Gets or sets the IconTemplateSelector property.  This dependency property
        ///     indicates the DataTemplateSelector for the Icon.
        /// </summary>
        public DataTemplateSelector IconTemplateSelector
        {
            get => (DataTemplateSelector) GetValue(IconTemplateSelectorProperty);
            set => SetValue(IconTemplateSelectorProperty, value);
        }

        /// <summary>
        ///     Handles changes to the IconTemplateSelector property.
        /// </summary>
        private static void OnIconTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MenuItemEx) d).OnIconTemplateSelectorChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the IconTemplateSelector property.
        /// </summary>
        protected virtual void OnIconTemplateSelectorChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateIcon();
        }

        #endregion

        #endregion

        #region Private Mehods

        private static void OnIconPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not null) ((MenuItemEx) sender).UpdateIcon();
        }

        private void UpdateIcon()
        {
            if (_reentrantFlag)
                return;
            _reentrantFlag = true;
            if (IconTemplateSelector is not null)
            {
                var dataTemplateToUse = IconTemplateSelector.SelectTemplate(Icon, this);
                if (dataTemplateToUse is not null)
                    Icon = dataTemplateToUse.LoadContent();
            }
            else if (IconTemplate is not null)
            {
                Icon = IconTemplate.LoadContent();
            }

            _reentrantFlag = false;
        }

        #endregion
    }
}