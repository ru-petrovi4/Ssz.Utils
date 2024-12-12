using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.MultiValueConverters
{
    public abstract class ValueConverterBase : OwnedDataSerializableAndCloneable, IMultiValueConverter,
        IDsItem, IDisposable
    {
        #region construction and destruction

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            ParentItem = null;

            Disposed = true;
        }


        ~ValueConverterBase()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool Disposed { get; private set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool DisableUpdatingTarget { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public object? NullOrEmptyValue { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public abstract object? Convert(object?[]? values, Type? targetType, object? parameter,
            CultureInfo culture);

        public abstract object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter,
            CultureInfo culture);

        public abstract void FindConstants(HashSet<string> constants);

        public abstract void ReplaceConstants(IDsContainer? container);

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        #endregion
    }
}