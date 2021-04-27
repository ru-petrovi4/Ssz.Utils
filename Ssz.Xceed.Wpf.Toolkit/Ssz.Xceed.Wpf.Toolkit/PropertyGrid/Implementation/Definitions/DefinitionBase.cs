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
using System.Linq.Expressions;
using System.Windows;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    public abstract class DefinitionBase : DependencyObject
    {
        internal bool IsLocked { get; private set; }

        internal void ThrowIfLocked<TMember>(Expression<Func<TMember>> propertyExpression)
        {
            if (IsLocked)
            {
                var propertyName = ReflectionHelper.GetPropertyOrFieldName(propertyExpression);
                var message = string.Format(
                    @"Cannot modify {0} once the definition has beed added to a collection.",
                    propertyName);
                throw new InvalidOperationException(message);
            }
        }

        internal virtual void Lock()
        {
            if (!IsLocked) IsLocked = true;
        }
    }
}