using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.DataAccess;
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

        /// <summary>
        ///     Are used only for writing to Source.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public List<string>? DataSourceStrings { get; set; } = null!;

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public abstract object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture);

        /// <summary>
        ///     Uses DataSourceStrings
        /// </summary>
        /// <param name="value"></param>
        /// <param name="dataValueViewModel"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        public abstract void ConvertBack(object? value, DataValueViewModel dataValueViewModel, object? parameter,
            CultureInfo culture);

        public abstract void FindConstants(HashSet<string> constants);

        public abstract void ReplaceConstants(IDsContainer? container);

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public virtual void Initialize()
        {   
        }

        #endregion
    }
}