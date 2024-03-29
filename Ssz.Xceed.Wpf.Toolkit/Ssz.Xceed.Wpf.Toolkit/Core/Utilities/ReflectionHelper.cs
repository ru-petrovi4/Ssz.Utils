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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Utilities
{
    internal static class ReflectionHelper
    {
      /// <summary>
      ///     Check the existence of the specified public instance (i.e. non static) property against
      ///     the type of the specified source object. If the property is not defined by the type,
      ///     a debug assertion will fail. Typically used to validate the parameter of a
      ///     RaisePropertyChanged method.
      /// </summary>
      /// <param name="sourceObject">The object for which the type will be checked.</param>
      /// <param name="propertyName">The name of the property.</param>
      [Conditional("DEBUG")]
        internal static void ValidatePublicPropertyName(object sourceObject, string propertyName)
        {
            if (sourceObject is null)
                throw new ArgumentNullException("sourceObject");

            if (propertyName is null)
                throw new ArgumentNullException("propertyName");

            Debug.Assert(
                sourceObject.GetType().GetProperty(propertyName,
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public) is not null,
                string.Format("Public property {0} not found on object of type {1}.", propertyName,
                    sourceObject.GetType().FullName));
        }

      /// <summary>
      ///     Check the existence of the specified instance (i.e. non static) property against
      ///     the type of the specified source object. If the property is not defined by the type,
      ///     a debug assertion will fail. Typically used to validate the parameter of a
      ///     RaisePropertyChanged method.
      /// </summary>
      /// <param name="sourceObject">The object for which the type will be checked.</param>
      /// <param name="propertyName">The name of the property.</param>
      [Conditional("DEBUG")]
        internal static void ValidatePropertyName(object sourceObject, string propertyName)
        {
            if (sourceObject is null)
                throw new ArgumentNullException("sourceObject");

            if (propertyName is null)
                throw new ArgumentNullException("propertyName");

            Debug.Assert(
                sourceObject.GetType().GetProperty(propertyName,
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public |
                    BindingFlags.NonPublic) is not null,
                string.Format("Public property {0} not found on object of type {1}.", propertyName,
                    sourceObject.GetType().FullName));
        }

        internal static bool TryGetEnumDescriptionAttributeValue(Enum enumeration, out string description)
        {
            try
            {
                var fieldInfo = enumeration.GetType().GetField(enumeration.ToString());
                var attributes =
                    fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true) as DescriptionAttribute[];
                if (attributes is not null && attributes.Length > 0)
                {
                    description = attributes[0].Description;
                    return true;
                }
            }
            catch
            {
            }

            description = string.Empty;
            return false;
        }

        [DebuggerStepThrough]
        internal static string GetPropertyOrFieldName(MemberExpression expression)
        {
            string propertyOrFieldName;
            if (!TryGetPropertyOrFieldName(expression, out propertyOrFieldName))
                throw new InvalidOperationException("Unable to retrieve the property or field name.");

            return propertyOrFieldName;
        }

        [DebuggerStepThrough]
        internal static string GetPropertyOrFieldName<TMember>(Expression<Func<TMember>> expression)
        {
            string propertyOrFieldName;
            if (!TryGetPropertyOrFieldName(expression, out propertyOrFieldName))
                throw new InvalidOperationException("Unable to retrieve the property or field name.");

            return propertyOrFieldName;
        }

        [DebuggerStepThrough]
        internal static bool TryGetPropertyOrFieldName(MemberExpression expression, out string propertyOrFieldName)
        {
            propertyOrFieldName = null;

            if (expression is null)
                return false;

            propertyOrFieldName = expression.Member.Name;

            return true;
        }

        [DebuggerStepThrough]
        internal static bool TryGetPropertyOrFieldName<TMember>(Expression<Func<TMember>> expression,
            out string propertyOrFieldName)
        {
            propertyOrFieldName = null;

            if (expression is null)
                return false;

            return TryGetPropertyOrFieldName(expression.Body as MemberExpression, out propertyOrFieldName);
        }

        public static bool IsPublicInstanceProperty(Type type, string propertyName)
        {
            var flags = BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public;
            return type.GetProperty(propertyName, flags) is not null;
        }
    }
}