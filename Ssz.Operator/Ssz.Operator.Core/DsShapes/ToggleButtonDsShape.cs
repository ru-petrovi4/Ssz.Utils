using System;
using System.ComponentModel;
using System.Windows;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public class ToggleButtonDsShape : ControlDsShape
    {
        #region construction and destruction

        public ToggleButtonDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ToggleButtonDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 80;
            HeightInitial = 30;
            BorderThickness = new Thickness(3);
            IsCheckedInfo = new BooleanDataBinding(visualDesignMode, loadXamlContent) {ConstValue = false};
            UncheckedContentInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);
            CheckedContentInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);
            PressedContentInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "ToggleButton";
        public static readonly Guid DsShapeTypeGuid = new(@"992378A5-D8C2-4807-957F-C499ACE6A479");

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
        [DsDisplayName(ResourceStrings.ControlDsShapeStyleInfo)]
        [Editor(typeof(DsUIElementPropertyTypeEditor<ToggleButtonStyleInfoSupplier>),
            typeof(DsUIElementPropertyTypeEditor<ToggleButtonStyleInfoSupplier>))]
        // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ToggleButtonDsShapeUncheckedContentInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
        public XamlDataBinding UncheckedContentInfo
        {
            get => _uncheckedContentInfo;
            set => SetValue(ref _uncheckedContentInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ToggleButtonDsShapeCheckedContentInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
        public XamlDataBinding CheckedContentInfo
        {
            get => _checkedContentInfo;
            set => SetValue(ref _checkedContentInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ToggleButtonDsShapePressedContentInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
        public XamlDataBinding PressedContentInfo
        {
            get => _pressedContentInfo;
            set => SetValue(ref _pressedContentInfo, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ToggleButtonDsShapeIsCheckedInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        public BooleanDataBinding IsCheckedInfo
        {
            get => _isCheckedInfo;
            set => SetValue(ref _isCheckedInfo, value);
        }

        public override string? GetStyleXamlString(IDsContainer? container)
        {
            return new ToggleButtonStyleInfoSupplier().GetPropertyXamlString(base.StyleInfo, container);
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
                writer.Write(IsCheckedInfo, context);
                writer.Write(UncheckedContentInfo, context);
                writer.Write(CheckedContentInfo, context);
                writer.Write(PressedContentInfo, context);
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
                            reader.ReadOwnedData(IsCheckedInfo, context);
                            reader.ReadOwnedData(UncheckedContentInfo, context);
                            reader.ReadOwnedData(CheckedContentInfo, context);
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

        private BooleanDataBinding _isCheckedInfo = null!;
        private XamlDataBinding _uncheckedContentInfo = null!;
        private XamlDataBinding _checkedContentInfo = null!;
        private XamlDataBinding _pressedContentInfo = null!;

        #endregion
    }
}