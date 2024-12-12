using System;
using System.ComponentModel;
using System.Windows;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public class TextBoxDsShape : ControlDsShape
    {
        #region construction and destruction

        public TextBoxDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public TextBoxDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 80;
            HeightInitial = 30;

            TextInfo = new TextDataBinding(visualDesignMode, loadXamlContent);
            TextAlignment = TextAlignment.Left;
            TextWrapping = TextWrapping.NoWrap;
            IsReadOnlyInfo = new BooleanDataBinding(visualDesignMode, loadXamlContent) {ConstValue = false};
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "EditBox";
        public static readonly Guid DsShapeTypeGuid = new(@"606F98F9-AD3C-49CE-9A82-9A25390CF38E");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override HorizontalAlignment HorizontalContentAlignment
        {
            get => base.HorizontalContentAlignment;
            set => base.HorizontalContentAlignment = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextDsShapeTextAlignment)]
        public TextAlignment TextAlignment
        {
            get => _textAlignment;
            set => SetValue(ref _textAlignment, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextDsShapeTextWrapping)]
        public TextWrapping TextWrapping
        {
            get => _textWrapping;
            set => SetValue(ref _textWrapping, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextDsShapeTextInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public TextDataBinding TextInfo
        {
            get => _textInfo;
            set => SetValue(ref _textInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextBoxDsShapeIsReadOnlyInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        public BooleanDataBinding IsReadOnlyInfo
        {
            get => _isReadOnlyInfo;
            set => SetValue(ref _isReadOnlyInfo, value);
        }

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }


        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(1))
            {
                writer.Write(TextInfo, context);
                writer.Write((int) TextAlignment);
                writer.Write((int) TextWrapping);
                writer.Write(IsReadOnlyInfo, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            reader.ReadOwnedData(TextInfo, context);
                            TextAlignment = (TextAlignment) reader.ReadInt32();
                            TextWrapping = (TextWrapping) reader.ReadInt32();
                            reader.ReadOwnedData(IsReadOnlyInfo, context);
                        }
                        catch (BlockEndingException)
                        {
                        }

                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        #region private fields

        private TextDataBinding _textInfo = null!;
        private TextAlignment _textAlignment;
        private TextWrapping _textWrapping;
        private BooleanDataBinding _isReadOnlyInfo = null!;

        #endregion
    }
}