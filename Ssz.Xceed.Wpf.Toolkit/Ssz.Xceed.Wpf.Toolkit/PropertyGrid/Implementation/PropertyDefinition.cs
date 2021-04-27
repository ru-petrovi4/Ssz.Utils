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
using System.Collections.Generic;
using System.Diagnostics;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    public class PropertyDefinition : PropertyDefinitionBase
    {
        private string _category;
        private string _description;
        private string _displayName;
        private int? _displayOrder;
        private bool? _isBrowsable = true;
        private bool? _isExpandable;
        private string _name;

        [Obsolete(@"Use 'TargetProperties' instead of 'Name'")]
        public string Name
        {
            get => _name;
            set
            {
                const string usageError =
                    "{0}: \'Name\' property is obsolete. Instead use \'TargetProperties\'. (XAML example: <t:PropertyDefinition TargetProperties=\"FirstName,LastName\" />)";
                Trace.TraceWarning(usageError, typeof(PropertyDefinition));
                _name = value;
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                ThrowIfLocked(() => Category);
                _category = value;
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                ThrowIfLocked(() => DisplayName);
                _displayName = value;
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                ThrowIfLocked(() => Description);
                _description = value;
            }
        }

        public int? DisplayOrder
        {
            get => _displayOrder;
            set
            {
                ThrowIfLocked(() => DisplayOrder);
                _displayOrder = value;
            }
        }

        public bool? IsBrowsable
        {
            get => _isBrowsable;
            set
            {
                ThrowIfLocked(() => IsBrowsable);
                _isBrowsable = value;
            }
        }

        public bool? IsExpandable
        {
            get => _isExpandable;
            set
            {
                ThrowIfLocked(() => IsExpandable);
                _isExpandable = value;
            }
        }

        internal override void Lock()
        {
            if (_name != null
                && TargetProperties != null
                && TargetProperties.Count > 0)
                throw new InvalidOperationException(
                    string.Format(
                        @"{0}: When using 'TargetProperties' property, do not use 'Name' property.",
                        typeof(PropertyDefinition)));

            if (_name != null) TargetProperties = new List<object> {_name};
            base.Lock();
        }
    }
}