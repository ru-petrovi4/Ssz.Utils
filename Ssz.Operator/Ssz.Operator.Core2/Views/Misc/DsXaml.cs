using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Markup;
using Ssz.Operator.Core.Drawings;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    //[ContentProperty(@"Xaml")] // For XAML serialization. Content property must be of type object or string.
    public class DsXaml : IDsItem, ICloneable
    {
        #region public functions

        [Searchable(false)]
        public string Xaml
        {
            get
            {
                if (_isAbsolutePaths) return _xaml;

                string? drawingFilesDirectoryFullName = null;
                var drawing = ParentItem.Find<DrawingBase>();
                if (drawing is not null) drawingFilesDirectoryFullName = drawing.DrawingFilesDirectoryFullName;

                return XamlHelper.GetXamlWithAbsolutePaths(_xaml, drawingFilesDirectoryFullName);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _xaml = @"";
                    _isAbsolutePaths = true;
                    _isRelativePaths = true;
                }
                else
                {
                    _xaml = value;
                    _isAbsolutePaths = false; // We do not know
                    _isRelativePaths = false; // We do not know

                    TryConvertAbsoluteToRelative();
                }
            }
        }


        [Searchable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string XamlWithRelativePaths
        {
            get
            {
                TryConvertAbsoluteToRelative();

                return _xaml;
            }
            internal set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _xaml = @"";
                    _isAbsolutePaths = true;
                    _isRelativePaths = true;
                }
                else
                {
                    _xaml = value;
                    _isAbsolutePaths = false;
                    _isRelativePaths = true;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string? XamlDesc => NameValueCollectionHelper.GetNameValueCollectionStringToDisplay(XamlHelper.GetXamlDesc(_xaml));

        [Searchable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool IsEmpty => _xaml == @"";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Searchable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public void ReplaceConstants(IDsContainer? container)
        {
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public void FindConstants(HashSet<string> constants)
        {
        }

        public override bool Equals(object? obj)
        {
            var other = obj as DsXaml;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _isAbsolutePaths == other._isAbsolutePaths &&
                   _isRelativePaths == other._isRelativePaths &&
                   _xaml == other._xaml;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public object Clone()
        {
            var clone = new DsXaml();
            clone._isAbsolutePaths = _isAbsolutePaths;
            clone._isRelativePaths = _isRelativePaths;
            clone._xaml = _xaml;
            return clone;
        }

        #endregion

        #region private functions

        private void TryConvertAbsoluteToRelative()
        {
            if (_isRelativePaths) return;

            string? drawingFilesDirectoryFullName = null;
            var drawing = ParentItem.Find<DrawingBase>();
            if (drawing is not null)
                drawingFilesDirectoryFullName = drawing.DrawingFilesDirectoryFullName;

            var xamlWithRelativePaths = XamlHelper.GetXamlWithRelativePathsAndCopyFiles(
                _xaml,
                drawingFilesDirectoryFullName);
            if (xamlWithRelativePaths is not null)
            {
                _xaml = xamlWithRelativePaths;
                _isAbsolutePaths = false;
                _isRelativePaths = true;
            }
        }

        #endregion

        #region private fields

        private bool _isAbsolutePaths = true;
        private bool _isRelativePaths = true;

        private string _xaml = "";

        #endregion
    }
}