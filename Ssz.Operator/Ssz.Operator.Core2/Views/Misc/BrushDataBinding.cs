using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Markup;
using Avalonia.Media;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Operator.Core.MultiValueConverters;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    [TypeConverter(typeof(BrushDataBindingTypeConverter))]
    //[ValueSerializer(typeof(BrushDataBindingValueSerializer))]
    public class BrushDataBinding : ValueDataBindingBase<DsBrushBase?>
    {
        #region construction and destruction

        public BrushDataBinding()
            : this(true, true)
        {
        }

        public BrushDataBinding(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
        }

        #endregion

        #region public functions

        //[Editor(typeof(BrushConverterTypeEditor), typeof(BrushConverterTypeEditor))]
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

        public override object Clone()
        {
            return this.CloneUsingSerialization(() => new BrushDataBinding(VisualDesignMode, LoadXamlContent));
        }

        public override void FindConstants(HashSet<string> constants)
        {
            base.FindConstants(constants);

            if (IsConst)
                if (ConstValue is not null)
                    ConstValue.FindConstants(constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(container);

            ItemHelper.ReplaceConstants(ConstValue, container);
        }

        public override Task<object?> GetParsedConstObjectAsync(IDsContainer? container)
        {
            if (ConstValue is null)
                return Task.FromResult<object?>(FallbackValue);
            return Task.FromResult<object?>(ConstValue.GetBrush(container));
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            if (DataBindingItemsCollection.Count > 0)
            {
                object?[] values = GetDataBindingItemsDefaultValues(container);

                using (var converter = (DsBrushConverter) GetConverterOrDefaultConverter(container))
                {
                    if (converter.DataSourceToUiStatements.Count > 0)
                    {
                        var firstTrue =
                            converter.DataSourceToUiStatements.FirstOrDefault(
                                s => ObsoleteAnyHelper.ConvertTo<bool>(s.Condition.Evaluate(values, null), false));
                        if (firstTrue is not null)
                        {
                            if (firstTrue.ParamNum.HasValue)
                            {
                                var paramNum = firstTrue.ParamNum.Value;
                                if (paramNum >= 0 && paramNum < values.Length)
                                    ConstValue = new XamlDsBrush
                                        {Brush = ObsoleteAnyHelper.ConvertTo<Brush>(values[paramNum], true)};
                                else
                                    ConstValue = null;
                            }
                            else
                            {
                                ConstValue = firstTrue.ConstDsBrush;
                            }
                        }
                        else
                        {
                            ConstValue = null;
                        }
                    }
                    else
                    {
                        var color = ObsoleteAnyHelper.ConvertTo<Color>(values[0], false);
                        //var deafaultColor = Activator.CreateInstance(typeof(Color));                        
                        if (color == new Color())
                            ConstValue = null;
                        else
                            ConstValue = new SolidDsBrush(color);
                    }
                }

                if (ConstValue is not null)
                {
                    var constants = new HashSet<string>();
                    ConstValue.FindConstants(constants);
                    if (constants.Count > 0)
                        OnPropertyChanged(@"ConstValue");
                    // Force to refresh ConstValue, because it contains generic params internally 
                }
            }
        }

        public override string ToString()
        {
            return IsConst
                ? "Const: " + (ConstValue is not null ? ConstValue.ToString() : Resources.DefaultValue)
                : DataSourceIdsListToTextConverter
                        .Instance
                        .Convert(DataBindingItemsCollection, typeof(string), null, CultureInfo.InvariantCulture) as
                    string ?? "";
        }

        public override bool Equals(object? obj)
        {
            var other = obj as BrushDataBinding;
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

        protected override DsBrushBase? GetDefaultValue()
        {
            return null;
        }

        protected override ValueConverterBase GetNewConverter()
        {
            return new DsBrushConverter();
        }

        #endregion
    }
}