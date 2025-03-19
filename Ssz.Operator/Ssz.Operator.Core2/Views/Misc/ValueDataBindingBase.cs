using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Markup;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using Ssz.Utils.MonitoredUndo;
//using Ssz.Utils.Wpf;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Operator.Core.MultiValueConverters;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public interface IValueDataBinding :
        ICloneable, IOwnedDataSerializable, IDsItem, ISupportsUndo, IDisposable, INotifyPropertyChanged
    {
        object? FallbackValue { get; set; }

        object? ConstObject { get; set; }

        bool IsConst { get; }

        ObservableCollection<DataBindingItem> DataBindingItemsCollection { get; }

        string Format { get; set; }

        ValueConverterBase? Converter { get; set; }

        Task<object?> GetParsedConstObjectAsync(IDsContainer? container);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="dataSourceStrings">Are used only for writing to Source.</param>
        /// <returns></returns>
        ValueConverterBase GetConverterOrDefaultConverter(IDsContainer? container, List<string>? dataSourceStrings = null);

        void ClearPropertyChangedEvent();
    }

    //[ContentProperty(@"ConverterForXaml")] // For XAML serialization. Content property must be of type object or string.
    public abstract class ValueDataBindingBase<T> : DisposableViewModelBase, IValueDataBinding
    {
        #region construction and destruction

        protected ValueDataBindingBase(bool visualDesignMode, bool loadXamlContent)
        {
            VisualDesignMode = visualDesignMode;
            LoadXamlContent = loadXamlContent;

            if (visualDesignMode)
                DataBindingItemsCollection.CollectionChanged += DataBindingItemsCollectionChanged;

            FallbackValue = AvaloniaProperty.UnsetValue;

            _constValue = GetDefaultValue();
            var item = _constValue as IDsItem;
            if (item is not null) item.ParentItem = this;
        }

        protected ValueDataBindingBase(bool visualDesignMode, bool loadXamlContent,
            DataBindingItem dataBindingItem)
            : this(visualDesignMode, loadXamlContent)
        {
            DataBindingItemsCollection.Add(dataBindingItem);
        }

        protected ValueDataBindingBase(bool visualDesignMode, bool loadXamlContent,
            DataBindingItem[] dataBindingItems)
            : this(visualDesignMode, loadXamlContent)
        {
            foreach (DataBindingItem dataItem in dataBindingItems) DataBindingItemsCollection.Add(dataItem);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                if (VisualDesignMode)
                    DataBindingItemsCollection.CollectionChanged -= DataBindingItemsCollectionChanged;

                if (_converter is not null)
                {
                    _converter.Dispose();
                    _converter = null;
                }

                DataBindingItemsCollection.Clear();
                ParentItem = null;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [field: Searchable(false)] // For XAML serialization        
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public object? FallbackValue { get; set; }

        [Browsable(false)]
        [Searchable(true)]
        public T ConstValue
        {
            get => _constValue;
            set
            {
                if (Equals(value, _constValue)) return;
                OnChanging(@"ConstValue", _constValue, value);
                var item = _constValue as IDsItem;
                if (item is not null) item.ParentItem = null;
                _constValue = value;
                item = _constValue as IDsItem;
                if (item is not null) item.ParentItem = this;
                OnPropertyChangedAuto();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        object? IValueDataBinding.ConstObject
        {
            get => ConstValue;
            set
            {
                if (value is null) ConstValue = GetDefaultValue();
                else ConstValue = (T) value;
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool IsConst => DataBindingItemsCollection.Count == 0;

        [DsDisplayName(ResourceStrings.ValueDataBindingBaseDataBindingItemsCollection)]
        //[Editor(typeof(DataBindingItemsCollectionTypeEditor), typeof(DataBindingItemsCollectionTypeEditor))]
        //[PropertyOrder(0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility
            .Content)] // For XAML serialization of collections
        public ObservableCollection<DataBindingItem> DataBindingItemsCollection { get; } = new();

        [DsDisplayName(ResourceStrings.ValueDataBindingBaseFormat)]
        //[PropertyOrder(1)]
        public virtual string Format
        {
            get => _format;
            set
            {
                if (value is null) value = "";
                if (Equals(value, _format)) return;
                OnChanging("Format", _format, value);
                _format = value;
                OnPropertyChangedAuto();
            }
        }

        [DsDisplayName(ResourceStrings.ValueDataBindingBaseConverter)]
        //[PropertyOrder(2)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public virtual ValueConverterBase? Converter
        {
            get => _converter;
            set
            {
                if (Equals(value, _converter)) return;
                OnChanging("Converter", _converter, value);
                if (_converter is not null) _converter.Dispose();
                _converter = value;
                if (_converter is not null) _converter.ParentItem = this;
                OnPropertyChangedAuto();
            }
        }

        [Browsable(false)]
        [DefaultValue(null)]
        public object? ConverterForXaml
        {
            get => Converter;
            set => Converter = value as ValueConverterBase;
        }

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

        public virtual void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            if (ReferenceEquals(context, SerializationContext.ShortBytes))
            {
                writer.Write(Format);
                if (IsConst)
                {
                    writer.WriteObject(_constValue);
                }
                else
                {
                    writer.WriteListOfOwnedDataSerializable(DataBindingItemsCollection, context);
                    if (_converter is not null) _converter.SerializeOwnedData(writer, context);
                }

                return;
            }

            using (writer.EnterBlock(4))
            {
                writer.WriteObject(ConstValue);
                writer.WriteListOfOwnedDataSerializable(DataBindingItemsCollection, context);
                writer.Write(Format);
                writer.WriteNullableOwnedData(Converter, context);
            }
        }

        public virtual void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 4:
                    {
                        ConstValue = (T) reader.ReadObject();
                        List<DataBindingItem> dataBindingItems =
                            reader.ReadListOfOwnedDataSerializable(() => new DataBindingItem(), context);
                        DataBindingItemsCollection.Clear();
                        foreach (DataBindingItem dataBindingItem in dataBindingItems)
                            DataBindingItemsCollection.Add(dataBindingItem);
                        Format = reader.ReadString();
                        Converter = (ValueConverterBase?) reader.ReadNullableOwnedData(GetNewConverter, context);
                    }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public abstract object Clone();

        public virtual void FindConstants(HashSet<string> constants)
        {
            foreach (DataBindingItem dataBindingItem in DataBindingItemsCollection)
                dataBindingItem.FindConstants(constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            foreach (DataBindingItem dataBindingItem in DataBindingItemsCollection)
                ItemHelper.ReplaceConstants(dataBindingItem, container);
        }

        public virtual Task<object?> GetParsedConstObjectAsync(IDsContainer? container)
        {
            if (StringHelper.IsNullOrEmptyString(ConstValue)) 
                return Task.FromResult(FallbackValue);
            return Task.FromResult<object?>(ConstValue);
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
            OnPropertyChanged("IsConst");
        }

        public object? GetUndoRoot()
        {
            var drawing = ParentItem.Find<DrawingBase>();
            if (drawing is not null) return drawing.GetUndoRoot();
            return null;
        }

        /// <summary>
        ///     Always returns new object
        /// </summary>
        /// <param name="container"></param>
        /// <param name="dataSourceStrings">Are used only for writing to Source.</param>
        /// <returns></returns>
        public virtual ValueConverterBase GetConverterOrDefaultConverter(IDsContainer? container, List<string>? dataSourceStrings = null)
        {
            ValueConverterBase converter;
            if (Converter is not null)
                converter = (ValueConverterBase) Converter.Clone();
            else
                converter = GetNewConverter();
            converter.ParentItem = this;
            converter.NullOrEmptyValue = FallbackValue;
            converter.ReplaceConstants(container);
            converter.DataSourceStrings = dataSourceStrings;
            converter.Initialize();
            return converter;
        }

        #endregion

        #region protected functions

        protected bool VisualDesignMode { get; }

        protected bool LoadXamlContent { get; }

        protected bool Equals(ValueDataBindingBase<T> other)
        {
            return Equals(_constValue, other._constValue) &&
                   DataBindingItemsCollection.SequenceEqual(other.DataBindingItemsCollection) &&
                   Format == other.Format && Equals(_converter, other._converter);
        }

        protected void OnChanging(string propertyName, object? oldValue, object? newValue)
        {
            if (VisualDesignMode) DefaultChangeFactory.Instance.OnChanging(this, propertyName, oldValue, newValue);
        }

        protected void DataBindingItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, "DataBindingItemsCollection",
                DataBindingItemsCollection, e);
        }

        protected object?[] GetDataBindingItemsDefaultValues(IDsContainer? container)
        {
            object?[] defaultValues = new object[DataBindingItemsCollection.Count];
            for (var i = 0; i < defaultValues.Length; i += 1)
            {
                var dataBindingItem = DataBindingItemsCollection[i];
                using (var dsi = new DataAccess.DataValueItem(
                    DataItemHelper.GetDataSourceString(dataBindingItem.Type, dataBindingItem.IdString,
                        dataBindingItem.DefaultValue, container),
                    null, null, true))
                {
                    defaultValues[i] = dsi.Value;
                }
            }

            return defaultValues;
        }

        protected abstract T GetDefaultValue();

        protected abstract ValueConverterBase GetNewConverter();

        #endregion

        #region private fields

        private T _constValue;

        private ValueConverterBase? _converter;

        private string _format = "F02";

        #endregion
    }
}