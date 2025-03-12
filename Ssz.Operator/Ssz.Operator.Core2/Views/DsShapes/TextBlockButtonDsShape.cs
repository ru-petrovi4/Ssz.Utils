using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public class TextBlockButtonDsShape : ButtonDsShapeBase
    {
        #region construction and destruction

        public TextBlockButtonDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public TextBlockButtonDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 80;
            HeightInitial = 30;

            Padding = new Thickness(-5, -5, -5, -5);
            TextInfo = new TextDataBinding(visualDesignMode, loadXamlContent) {ConstValue = "text"};
            TextAlignment = TextAlignment.Center;
            TextWrapping = TextWrapping.NoWrap;
            TextStretch = Stretch.None;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "ButtonWithLabel";
        public static readonly Guid DsShapeTypeGuid = new(@"1A3C3AC5-8E1A-41B0-8B95-B1DEE0CDEC82");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override HorizontalAlignment HorizontalContentAlignment
        {
            get => base.HorizontalContentAlignment;
            set => base.HorizontalContentAlignment = value;
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
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public TextDataBinding TextInfo
        {
            get => _textInfo;
            set => SetValue(ref _textInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextDsShapeTextStretch)]
        public Stretch TextStretch
        {
            get => _textStretch;
            set => SetValue(ref _textStretch, value);
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

            using (writer.EnterBlock(3))
            {
                writer.Write(TextInfo, context);
                writer.Write((int) TextAlignment);
                writer.Write((int) TextWrapping);
                writer.Write((int) TextStretch);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedDataAsync(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 2:
                        try
                        {
                            reader.ReadOwnedData(TextInfo, context);
                            TextAlignment = TreeHelper.GetTextAlignment_FromWpf(reader.ReadInt32());
                            TextWrapping = TreeHelper.GetTextWrapping_FromWpf(reader.ReadInt32());
                            TextStretch = (Stretch) reader.ReadInt32();
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    case 3:
                        try
                        {
                            reader.ReadOwnedData(TextInfo, context);
                            TextAlignment = (TextAlignment)reader.ReadInt32();
                            TextWrapping = (TextWrapping)reader.ReadInt32();
                            TextStretch = (Stretch)reader.ReadInt32();
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
        private Stretch _textStretch;
        private TextAlignment _textAlignment;
        private TextWrapping _textWrapping;

        #endregion
    }
}