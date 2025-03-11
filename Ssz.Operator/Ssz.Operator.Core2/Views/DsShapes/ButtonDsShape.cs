using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public class ButtonDsShape : ButtonDsShapeBase
    {
        #region construction and destruction

        public ButtonDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ButtonDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 80;
            HeightInitial = 30;

            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;

            Padding = new Thickness(2);
            ContentHorizontalAlignment = HorizontalAlignment.Center;
            ContentVerticalAlignment = VerticalAlignment.Center;
            ContentInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);
            PressedContentInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);

            TextMargin = new Thickness(-7, -7, -7, -7);
            TextStretch = Stretch.None;
            TextHorizontalAlignment = TextAlignment.Center;
            TextVerticalAlignment = VerticalAlignment.Center;
            TextWrapping = TextWrapping.NoWrap;
            TextInfo = new TextDataBinding(visualDesignMode, loadXamlContent) {ConstValue = "text"};
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Button";
        public static readonly Guid DsShapeTypeGuid = new(@"56045A40-C2B0-4F81-B5A4-80393D767198");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override HorizontalAlignment HorizontalContentAlignment
        {
            get => base.HorizontalContentAlignment;
            set => base.HorizontalContentAlignment = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override VerticalAlignment VerticalContentAlignment
        {
            get => base.VerticalContentAlignment;
            set => base.VerticalContentAlignment = value;
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_ContentHorizontalAlignment)]
        //[PropertyOrder(1001)]
        public HorizontalAlignment ContentHorizontalAlignment
        {
            get => _contentHorizontalAlignment;
            set => SetValue(ref _contentHorizontalAlignment, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_ContentVerticalAlignment)]
        //[PropertyOrder(1002)]
        public VerticalAlignment ContentVerticalAlignment
        {
            get => _contentVerticalAlignment;
            set => SetValue(ref _contentVerticalAlignment, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_ContentInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
        //[PropertyOrder(1003)]
        public XamlDataBinding ContentInfo
        {
            get => _contentInfo;
            set => SetValue(ref _contentInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_PressedContentInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
        //[PropertyOrder(1004)]
        public XamlDataBinding PressedContentInfo
        {
            get => _pressedContentInfo;
            set => SetValue(ref _pressedContentInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_TextMargin)]
        //[PropertyOrder(1005)]
        public Thickness TextMargin
        {
            get => _textMargin;
            set => SetValue(ref _textMargin, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_TextStretch)]
        //[PropertyOrder(1006)]
        public Stretch TextStretch
        {
            get => _textStretch;
            set => SetValue(ref _textStretch, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_TextHorizontalAlignment)]
        //[PropertyOrder(1007)]
        public TextAlignment TextHorizontalAlignment
        {
            get => _textHorizontalAlignment;
            set => SetValue(ref _textHorizontalAlignment, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_TextVerticalAlignment)]
        //[PropertyOrder(1008)]
        public VerticalAlignment TextVerticalAlignment
        {
            get => _textVerticalAlignment;
            set => SetValue(ref _textVerticalAlignment, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_TextWrapping)]
        //[PropertyOrder(1009)]
        public TextWrapping TextWrapping
        {
            get => _textWrapping;
            set => SetValue(ref _textWrapping, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_TextInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(1010)]
        public TextDataBinding TextInfo
        {
            get => _textInfo;
            set => SetValue(ref _textInfo, value);
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
            using (writer.EnterBlock(2))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write((int) ContentHorizontalAlignment);
                writer.Write((int) ContentVerticalAlignment);
                writer.Write(ContentInfo, context);
                writer.Write(PressedContentInfo, context);

                writer.Write(TextMargin);
                writer.Write((int) TextStretch);
                writer.Write((int) TextHorizontalAlignment);
                writer.Write((int) TextVerticalAlignment);
                writer.Write((int) TextWrapping);
                writer.Write(TextInfo, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        base.DeserializeOwnedData(reader, context);

                        ContentHorizontalAlignment = TreeHelper.GetHorizontalAlignment_FromWpf(reader.ReadInt32());
                        ContentVerticalAlignment = TreeHelper.GetVerticalAlignment_FromWpf(reader.ReadInt32());
                        reader.ReadOwnedData(ContentInfo, context);
                        reader.ReadOwnedData(PressedContentInfo, context);

                        TextMargin = reader.ReadThickness();
                        TextStretch = (Stretch)reader.ReadInt32();
                        TextHorizontalAlignment = TreeHelper.GetTextAlignment_FromWpf(reader.ReadInt32());
                        TextVerticalAlignment = TreeHelper.GetVerticalAlignment_FromWpf(reader.ReadInt32());
                        TextWrapping = TreeHelper.GetTextWrapping_FromWpf(reader.ReadInt32());
                        reader.ReadOwnedData(TextInfo, context);
                        break;
                    case 2:
                        base.DeserializeOwnedData(reader, context);

                        ContentHorizontalAlignment = (HorizontalAlignment)reader.ReadInt32();
                        ContentVerticalAlignment = (VerticalAlignment)reader.ReadInt32();
                        reader.ReadOwnedData(ContentInfo, context);
                        reader.ReadOwnedData(PressedContentInfo, context);

                        TextMargin = reader.ReadThickness();
                        TextStretch = (Stretch) reader.ReadInt32();
                        TextHorizontalAlignment = (TextAlignment) reader.ReadInt32();
                        TextVerticalAlignment = (VerticalAlignment)reader.ReadInt32();
                        TextWrapping = (TextWrapping) reader.ReadInt32();
                        reader.ReadOwnedData(TextInfo, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        #region private fields

        private HorizontalAlignment _contentHorizontalAlignment;
        private VerticalAlignment _contentVerticalAlignment;
        private XamlDataBinding _contentInfo = null!;
        private XamlDataBinding _pressedContentInfo = null!;

        private Thickness _textMargin;
        private Stretch _textStretch;
        private TextAlignment _textHorizontalAlignment;
        private VerticalAlignment _textVerticalAlignment;
        private TextWrapping _textWrapping;
        private TextDataBinding _textInfo = null!;

        #endregion
    }
}