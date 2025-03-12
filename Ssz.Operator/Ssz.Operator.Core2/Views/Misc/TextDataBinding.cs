using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup;
using Ssz.Operator.Core.Constants;



//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Operator.Core.MultiValueConverters;
using Avalonia.Data;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    [TypeConverter(typeof(TextDataBindingTypeConverter))]
    //[ValueSerializer(typeof(TextDataBindingValueSerializer))]
    public class TextDataBinding : ValueDataBindingBase<string>
    {
        #region construction and destruction

        public TextDataBinding()
            : this(true, true)
        {
        }

        public TextDataBinding(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
        }

        public TextDataBinding(DataBindingItem[] dataBindingItems)
            : base(true, true, dataBindingItems)
        {
        }

        public TextDataBinding(DataBindingItem dataBindingItem)
            : base(true, true, dataBindingItem)
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

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(3))
            {
                base.SerializeOwnedData(writer, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            if (reader.GetBlockVersionWithoutChangingStreamPosition() == 1)
            {
                base.DeserializeOwnedDataAsync(reader, context);

                using (Block block = reader.EnterBlock())
                {
                    switch (block.Version)
                    {
                        case 2:
                            ConstValue = reader.ReadString();
                            Converter = reader.ReadObject() as ValueConverterBase;
                            break;
                        default:
                            throw new BlockUnsupportedVersionException();
                    }
                }

                return;
            }

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 3:
                        base.DeserializeOwnedDataAsync(reader, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override object Clone()
        {
            return this.CloneUsingSerialization(() => new TextDataBinding(VisualDesignMode, LoadXamlContent));
        }

        public override void FindConstants(HashSet<string> constants)
        {
            base.FindConstants(constants);

            if (IsConst)
                ConstantsHelper.FindConstants(ConstValue, constants);
            else
                ConstantsHelper.FindConstants(Format, constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(container);

            ConstValue = ConstantsHelper.ComputeValue(container, ConstValue)!;
            Format = ConstantsHelper.ComputeValue(container, Format)!;
        }

        public override Task<object?> GetParsedConstObjectAsync(IDsContainer? container)
        {
            var constString = ConstantsHelper.ComputeValue(container, ConstValue);
            if (string.IsNullOrEmpty(constString)) 
                return Task.FromResult<object?>(FallbackValue);
            return Task.FromResult<object?>(constString);
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            if (DataBindingItemsCollection.Count > 0)
            {
                object?[] values = GetDataBindingItemsDefaultValues(container);

                using (var converter = (LocalizedConverter) GetConverterOrDefaultConverter(container))
                {
                    var value = converter.Convert(values, typeof(string), null, CultureInfo.InvariantCulture);
                    if (value == BindingOperations.DoNothing || value == AvaloniaProperty.UnsetValue)
                        ConstValue = @"";
                    else
                        ConstValue = value as string ?? "";
                }
            }
            else
            {
                if (ConstantsHelper.ContainsQuery(ConstValue))
                    OnPropertyChanged(@"ConstValue"); // Force to refresh, because it contains generic params
            }
        }

        public override string ToString()
        {
            return IsConst
                ? ConstValue
                : DataSourceIdsListToTextConverter
                        .Instance
                        .Convert(DataBindingItemsCollection, typeof(string), null, CultureInfo.InvariantCulture) as
                    string ?? @"";
        }

        public override bool Equals(object? obj)
        {
            var other = obj as TextDataBinding;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override ValueConverterBase GetConverterOrDefaultConverter(IDsContainer? container, List<string>? dataSourceStrings = null)
        {
            LocalizedConverter result = (LocalizedConverter) base.GetConverterOrDefaultConverter(container, dataSourceStrings);
            result.Format = ConstantsHelper.ComputeValue(container, Format)!;
            return result;
        }

        #endregion

        #region protected functions

        protected override string GetDefaultValue()
        {
            return @"";
        }

        protected override ValueConverterBase GetNewConverter()
        {
            return new LocalizedConverter();
        }

        #endregion
    }
}