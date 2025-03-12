using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Operator.Core.MultiValueConverters;
using Avalonia.Data;

namespace Ssz.Operator.Core
{
    [TypeConverter(typeof(StructDataBindingTypeConverter<double, DoubleDataBinding>))]
    //[ValueSerializer(typeof(StructDataBindingValueSerializer<double>))]
    public class DoubleDataBinding : StructDataBinding<double>
    {
        #region construction and destruction

        public DoubleDataBinding() // For XAML serialization
            : base(true, true)
        {
        }

        public DoubleDataBinding(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
        }

        #region public functions

        public override object Clone()
        {
            return this.CloneUsingSerialization(() => new DoubleDataBinding(VisualDesignMode, LoadXamlContent));
        }

        #endregion

        #endregion
    }

    [TypeConverter(typeof(StructDataBindingTypeConverter<int, Int32DataBinding>))]
    //[ValueSerializer(typeof(StructDataBindingValueSerializer<int>))]
    public class Int32DataBinding : StructDataBinding<int>
    {
        #region public functions

        public override object Clone()
        {
            return this.CloneUsingSerialization(() => new Int32DataBinding(VisualDesignMode, LoadXamlContent));
        }

        #endregion

        #region construction and destruction

        public Int32DataBinding() // For XAML serialization
            : base(true, true)
        {
        }

        public Int32DataBinding(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
        }

        #endregion
    }

    [TypeConverter(typeof(StructDataBindingTypeConverter<bool, BooleanDataBinding>))]
    //[ValueSerializer(typeof(StructDataBindingValueSerializer<bool>))]
    public class BooleanDataBinding : StructDataBinding<bool>
    {
        #region public functions

        public override object Clone()
        {
            return this.CloneUsingSerialization(() => new BooleanDataBinding(VisualDesignMode, LoadXamlContent));
        }

        #endregion

        #region construction and destruction

        public BooleanDataBinding() // For XAML serialization
            : base(true, true)
        {
        }

        public BooleanDataBinding(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
        }

        #endregion
    }

    public abstract class StructDataBinding<T> : ValueDataBindingBase<T>
        where T : struct
    {
        #region construction and destruction

        public StructDataBinding(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
        }

        #endregion

        #region public functions

        //[Editor(typeof(StructConverterTypeEditor), typeof(StructConverterTypeEditor))]
        public override ValueConverterBase? Converter
        {
            get => base.Converter;
            set => base.Converter = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override string Format
        {
            get => base.Format;
            set => base.Format = value;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                base.SerializeOwnedData(writer, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 2:
                        base.DeserializeOwnedDataAsync(reader, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            if (DataBindingItemsCollection.Count > 0)
            {
                object?[] values = GetDataBindingItemsDefaultValues(container);

                using (var converter = (LocalizedConverter) GetConverterOrDefaultConverter(container))
                {
                    var value = converter.Convert(values, typeof(T), null, CultureInfo.InvariantCulture);
                    if (value == BindingOperations.DoNothing || value == AvaloniaProperty.UnsetValue)
                    {
                        ConstValue = default;
                    }
                    else
                    {
                        if (value is null) ConstValue = default;
                        else ConstValue = (T) value;
                    }
                }
            }
        }

        public override string ToString()
        {
            return IsConst
                ? "Const: " + ConstValue
                : DataSourceIdsListToTextConverter
                        .Instance
                        .Convert(DataBindingItemsCollection, typeof(string), null, CultureInfo.InvariantCulture) as
                    string ?? "";
        }

        public override bool Equals(object? obj)
        {
            var other = obj as StructDataBinding<T>;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        #endregion

        #region protected functions

        protected override T GetDefaultValue()
        {
            return default;
        }

        protected override ValueConverterBase GetNewConverter()
        {
            return new LocalizedConverter();
        }

        #endregion
    }
}