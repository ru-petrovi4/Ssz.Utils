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
using System.ComponentModel;
using System.Linq.Expressions;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Utilities
{
    internal static class PropertyChangedExt
    {
        private static string GetPropertyName(MemberExpression expression, Type ownerType)
        {
            var targetType = expression.Expression.Type;
            if (!targetType.IsAssignableFrom(ownerType))
                throw new ArgumentException("The expression must target a property or field on the appropriate owner.",
                    "expression");

            return ReflectionHelper.GetPropertyOrFieldName(expression);
        }

        #region Notify Methods

        public static void Notify<TMember>(
            this INotifyPropertyChanged sender,
            PropertyChangedEventHandler handler,
            Expression<Func<TMember>> expression)
        {
            if (sender is null)
                throw new ArgumentNullException("sender");

            if (expression is null)
                throw new ArgumentNullException("expression");

            var body = expression.Body as MemberExpression;
            if (body is null)
                throw new ArgumentException("The expression must target a property or field.", "expression");

            var propertyName = GetPropertyName(body, sender.GetType());

            NotifyCore(sender, handler, propertyName);
        }

        public static void Notify(this INotifyPropertyChanged sender, PropertyChangedEventHandler handler,
            string propertyName)
        {
            if (sender is null)
                throw new ArgumentNullException("sender");

            if (propertyName is null)
                throw new ArgumentNullException("propertyName");

            ReflectionHelper.ValidatePropertyName(sender, propertyName);

            NotifyCore(sender, handler, propertyName);
        }

        private static void NotifyCore(INotifyPropertyChanged sender, PropertyChangedEventHandler handler,
            string propertyName)
        {
            if (handler is not null) handler(sender, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region PropertyChanged Verification Methods

        internal static bool PropertyChanged(string propertyName, PropertyChangedEventArgs e, bool targetPropertyOnly)
        {
            var target = e.PropertyName;
            if (target == propertyName)
                return true;

            return !targetPropertyOnly
                   && string.IsNullOrEmpty(target);
        }

        internal static bool PropertyChanged<TOwner, TMember>(
            Expression<Func<TMember>> expression,
            PropertyChangedEventArgs e,
            bool targetPropertyOnly)
        {
            var body = expression.Body as MemberExpression;
            if (body is null)
                throw new ArgumentException("The expression must target a property or field.", "expression");

            return PropertyChanged(body, typeof(TOwner), e, targetPropertyOnly);
        }

        internal static bool PropertyChanged<TOwner, TMember>(
            Expression<Func<TOwner, TMember>> expression,
            PropertyChangedEventArgs e,
            bool targetPropertyOnly)
        {
            var body = expression.Body as MemberExpression;
            if (body is null)
                throw new ArgumentException("The expression must target a property or field.", "expression");

            return PropertyChanged(body, typeof(TOwner), e, targetPropertyOnly);
        }

        private static bool PropertyChanged(MemberExpression expression, Type ownerType, PropertyChangedEventArgs e,
            bool targetPropertyOnly)
        {
            var propertyName = GetPropertyName(expression, ownerType);

            return PropertyChanged(propertyName, e, targetPropertyOnly);
        }

        #endregion
    }
}