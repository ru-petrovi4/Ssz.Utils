using System;
using System.ComponentModel;
using Avalonia;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class ContentButtonDsShape : ButtonDsShapeBase
    {
        #region construction and destruction

        public ContentButtonDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ContentButtonDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 80;
            HeightInitial = 30;

            Padding = new Thickness(2);
            ContentInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);
            PressedContentInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "ButtonWithContent";
        public static readonly Guid DsShapeTypeGuid = new(@"0618A194-4356-40CB-9094-D415BE4A9393");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override DsFont? DsFont
        {
            get => base.DsFont;
            set => base.DsFont = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override BrushDataBinding ForegroundInfo
        {
            get => base.ForegroundInfo;
            set => base.ForegroundInfo = value;
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShape_ContentInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
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
        public XamlDataBinding PressedContentInfo
        {
            get => _pressedContentInfo;
            set => SetValue(ref _pressedContentInfo, value);
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

            using (writer.EnterBlock(2))
            {
                writer.Write(ContentInfo, context);
                writer.Write(PressedContentInfo, context);
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
                            reader.ReadOwnedData(ContentInfo, context);
                            reader.ReadOwnedData(PressedContentInfo, context);
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

        private XamlDataBinding _contentInfo = null!;
        private XamlDataBinding _pressedContentInfo = null!;

        #endregion
    }
}