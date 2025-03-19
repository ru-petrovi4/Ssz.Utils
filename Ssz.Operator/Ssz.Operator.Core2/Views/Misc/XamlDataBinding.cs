using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Operator.Core.MultiValueConverters;
using Avalonia.Controls.Shapes;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    [TypeConverter(typeof(XamlDataBindingTypeConverter))]
    public class XamlDataBinding : ValueDataBindingBase<DsXaml>
    {
        #region construction and destruction

        public XamlDataBinding() // For XAML serialization
            : this(true, true)
        {
        }

        public XamlDataBinding(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
        }

        #endregion

        #region public functions

        [DsDisplayName(ResourceStrings.ValueDataBindingBaseConverter)]
        //[Editor(typeof(XamlConverterTypeEditor), typeof(XamlConverterTypeEditor))]
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

        public override async Task<object?> GetParsedConstObjectAsync(IDsContainer? container)
        {
            object? result;
            try
            {
                result = await XamlHelper.LoadFromXamlWithDescAsync(ConstValue.Xaml, ParentItem.Find<DrawingBase>()?.DrawingFilesDirectoryFullName);
            }
            catch
            {
                result = new Rectangle {Fill = new SolidColorBrush(Colors.White)};
            }

            if (result is null) return FallbackValue;
            return result;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            if (ReferenceEquals(context, SerializationContext.ShortBytes))
            {
                if (IsConst)
                {
                    string? drawingFilesDirectoryFullName = null;
                    var drawing = ParentItem.Find<DrawingBase>();
                    if (drawing is not null) drawingFilesDirectoryFullName = drawing.DrawingFilesDirectoryFullName;
                    writer.WriteHashOfXaml(ConstValue.XamlWithRelativePaths, drawingFilesDirectoryFullName);
                }
                else
                {
                    writer.WriteListOfOwnedDataSerializable(DataBindingItemsCollection, context);
                    if (Converter is not null) Converter.SerializeOwnedData(writer, context);
                }

                return;
            }

            using (writer.EnterBlock(5))
            {
                writer.Write(ConstValue.XamlWithRelativePaths);
                writer.WriteListOfOwnedDataSerializable(DataBindingItemsCollection, context);
                writer.Write(Format);
                writer.WriteNullableOwnedData(Converter, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 5:
                    {
                        if (LoadXamlContent)
                            ConstValue.XamlWithRelativePaths = reader.ReadString();
                        else
                            reader.SkipString();
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

        public override object Clone()
        {
            return this.CloneUsingSerialization(() => new XamlDataBinding(VisualDesignMode, LoadXamlContent));
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            if (DataBindingItemsCollection.Count > 0)
            {
                object?[] values = GetDataBindingItemsDefaultValues(container);

                using (var converter = (XamlConverter) GetConverterOrDefaultConverter(container))
                {
                    var firstTrue =
                        converter.DataSourceToUiStatements.FirstOrDefault(s =>
                            ObsoleteAnyHelper.ConvertTo<bool>(s.Condition.Evaluate(values, null, null), false));
                    if (firstTrue is not null)
                        ConstValue = (DsXaml) ((DsXaml) firstTrue.ConstXaml).Clone();
                    else
                        ConstValue = new DsXaml();
                }
            }
        }

        public override string ToString()
        {
            return IsConst
                ? "Const"
                : DataSourceIdsListToTextConverter
                        .Instance
                        .Convert(DataBindingItemsCollection, typeof(string), null, CultureInfo.InvariantCulture) as
                    string ?? "";
        }

        public override bool Equals(object? obj)
        {
            var other = obj as XamlDataBinding;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public void GetUsedFileNames(HashSet<string> usedFileNames)
        {
            XamlHelper.GetUsedFileNames(ConstValue.XamlWithRelativePaths, usedFileNames);
            var xamlConverter = Converter as XamlConverter;
            if (xamlConverter is not null) 
                xamlConverter.GetUsedFileNames(usedFileNames);
        }

        #endregion

        #region protected functions

        protected override DsXaml GetDefaultValue()
        {
            return new();
        }

        protected override ValueConverterBase GetNewConverter()
        {
            return new XamlConverter(VisualDesignMode, LoadXamlContent);
        }

        #endregion
    }
}