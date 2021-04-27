/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Converters;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    public abstract class PropertyDefinitionBase : DefinitionBase
    {
        private IList _targetProperties;

        internal PropertyDefinitionBase()
        {
            _targetProperties = new List<object>();
        }

        [TypeConverter(typeof(ListConverter))]
        public IList TargetProperties
        {
            get => _targetProperties;
            set
            {
                ThrowIfLocked(() => TargetProperties);
                _targetProperties = value;
            }
        }

        internal override void Lock()
        {
            if (IsLocked)
                return;

            base.Lock();

            // Just create a new copy of the properties target to ensure 
            // that the list doesn't ever get modified.

            var newList = new List<object>();
            if (_targetProperties != null)
                foreach (var p in _targetProperties)
                {
                    var prop = p;
                    // Convert all TargetPropertyType to Types
                    var targetType = prop as TargetPropertyType;
                    if (targetType != null) prop = targetType.Type;
                    newList.Add(prop);
                }

            _targetProperties = new ReadOnlyCollection<object>(newList);
        }
    }
}