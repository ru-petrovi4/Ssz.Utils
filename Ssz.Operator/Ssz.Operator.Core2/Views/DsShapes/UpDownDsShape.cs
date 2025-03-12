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
    public class UpDownDsShape : ControlDsShape
    {
        #region construction and destruction

        public UpDownDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public UpDownDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 80;
            HeightInitial = 30;

            TextInfo = new TextDataBinding(visualDesignMode, loadXamlContent);
            TextAlignment = TextAlignment.Left;
            TextWrapping = TextWrapping.NoWrap;            
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "UpDown";
        public static readonly Guid DsShapeTypeGuid = new(@"BDF6E6E2-C11D-42E0-AFB1-8694B2E51A2B");

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
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
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

                writer.Write(TextInfo, context);
                writer.Write((int) TextAlignment);
                writer.Write((int) TextWrapping);                
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        base.DeserializeOwnedDataAsync(reader, context);

                        try
                        {
                            reader.ReadOwnedData(TextInfo, context);
                            TextAlignment = TreeHelper.GetTextAlignment_FromWpf(reader.ReadInt32());
                            TextWrapping = TreeHelper.GetTextWrapping_FromWpf(reader.ReadInt32());                            
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    case 2:
                        base.DeserializeOwnedDataAsync(reader, context);

                        try
                        {
                            reader.ReadOwnedData(TextInfo, context);
                            TextAlignment = (TextAlignment)reader.ReadInt32();
                            TextWrapping = (TextWrapping)reader.ReadInt32();
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

        #endregion
    }
}