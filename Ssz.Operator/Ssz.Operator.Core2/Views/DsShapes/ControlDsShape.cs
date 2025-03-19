using System.ComponentModel;
using Avalonia;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Utils.Wpf;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using System;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Ssz.Operator.Core.DsShapes
{
    public abstract class ControlDsShape : DsShapeBase
    {
        #region construction and destruction

        protected ControlDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            DsFont = null;
            BackgroundInfo = new BrushDataBinding(visualDesignMode, loadXamlContent);
            ForegroundInfo = new BrushDataBinding(visualDesignMode, loadXamlContent);
            BorderThickness = new Thickness(0);
            Padding = new Thickness(0);
            BorderBrushInfo = new BrushDataBinding(visualDesignMode, loadXamlContent);
            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
            StyleInfo = new DsUIElementProperty(visualDesignMode, loadXamlContent);
            ToolTipTextInfo = new TextDataBinding {ConstValue = @""};
            ToolTipPlacement = PlacementMode.Right;
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextDsShapeDsFont)]
        //[Editor(typeof(DsFontTypeEditor), typeof(DsFontTypeEditor))]
        //[PropertyOrder(1)]
        public virtual DsFont? DsFont
        {
            get => _dsFont;
            set => SetValue(ref _dsFont, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeBackgroundInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        //[PropertyOrder(2)]
        public virtual BrushDataBinding BackgroundInfo
        {
            get => _backgroundInfo;
            set => SetValue(ref _backgroundInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeForegroundInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        //[PropertyOrder(3)]
        public virtual BrushDataBinding ForegroundInfo
        {
            get => _foregroundInfo;
            set => SetValue(ref _foregroundInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeBorderThickness)]
        //[PropertyOrder(4)]
        public virtual Thickness BorderThickness
        {
            get => _borderThickness;
            set => SetValue(ref _borderThickness, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapePadding)]
        //[PropertyOrder(5)]
        public virtual Thickness Padding
        {
            get => _padding;
            set => SetValue(ref _padding, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeBorderBrushInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        //[PropertyOrder(6)]
        public virtual BrushDataBinding BorderBrushInfo
        {
            get => _borderBrushInfo;
            set => SetValue(ref _borderBrushInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeHorizontalContentAlignment)]
        //[PropertyOrder(7)]
        public virtual HorizontalAlignment HorizontalContentAlignment
        {
            get => _horizontalContentAlignment;
            set => SetValue(ref _horizontalContentAlignment, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeVerticalContentAlignment)]
        //[PropertyOrder(8)]
        public virtual VerticalAlignment VerticalContentAlignment
        {
            get => _verticalContentAlignment;
            set => SetValue(ref _verticalContentAlignment, value);
        }

        public virtual DsUIElementProperty StyleInfo
        {
            get => _styleInfo;
            set => SetValue(ref _styleInfo, value);
        }

        [DsCategory(ResourceStrings.ToolTipCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeToolTipFileRelativePath)]
        //[PropertyOrder(1)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        [DefaultValue(@"")] // For XAML serialization
        public string ToolTipFileRelativePath
        {
            get => _toolTipFileRelativePath;
            set => SetValue(ref _toolTipFileRelativePath, value);
        }

        [DsCategory(ResourceStrings.ToolTipCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeToolTipTextInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(2)]
        [DefaultValue(typeof(TextDataBinding), @"")] // For XAML serialization
        public TextDataBinding ToolTipTextInfo
        {
            get => _toolTipTextInfo;
            set => SetValue(ref _toolTipTextInfo, value);
        }

        [DsCategory(ResourceStrings.ToolTipCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeToolTipPlacement)]        
        //[PropertyOrder(3)]        
        public PlacementMode ToolTipPlacement
        {
            get => _toolTipPlacement;
            set => SetValue(ref _toolTipPlacement, value);
        }        

        public virtual string? GetStyleXamlString(IDsContainer? container)
        {
            return null;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(5))
            {
                writer.Write(NameValueCollectionValueSerializer<DsFont>.Instance.ConvertToString(DsFont, null));
                writer.Write(BackgroundInfo, context);
                writer.Write(ForegroundInfo, context);
                writer.Write(BorderThickness);
                writer.Write(Padding);
                writer.Write(BorderBrushInfo, context);
                writer.Write((int) HorizontalContentAlignment);
                writer.Write((int) VerticalContentAlignment);
                writer.Write(StyleInfo, context);
                writer.Write(ToolTipFileRelativePath);
                writer.Write(ToolTipTextInfo, context);
                writer.Write((int)ToolTipPlacement);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 4:
                        try
                        {
                            if (LoadXamlContent)
                            {
                                string dsFontString = reader.ReadString();
                                DsFont =
                                    NameValueCollectionValueSerializer<DsFont>.Instance.ConvertFromString(dsFontString,
                                            null)
                                        as
                                        DsFont;
                            }
                            else
                            {
                                reader.SkipString();
                            }

                            reader.ReadOwnedData(BackgroundInfo, context);
                            reader.ReadOwnedData(ForegroundInfo, context);
                            BorderThickness = reader.ReadThickness();
                            Padding = reader.ReadThickness();
                            reader.ReadOwnedData(BorderBrushInfo, context);
                            HorizontalContentAlignment = TreeHelper.GetHorizontalAlignment_FromWpf(reader.ReadInt32());
                            VerticalContentAlignment = TreeHelper.GetVerticalAlignment_FromWpf(reader.ReadInt32());
                            reader.ReadOwnedData(StyleInfo, context);
                            ToolTipFileRelativePath = reader.ReadString();
                            reader.ReadOwnedData(ToolTipTextInfo, context);
                            ToolTipPlacement = TreeHelper.GetPlacementMode_FromWpf(reader.ReadInt32());
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    case 5:
                        try
                        {
                            if (LoadXamlContent)
                            {
                                string dsFontString = reader.ReadString();
                                DsFont =
                                    NameValueCollectionValueSerializer<DsFont>.Instance.ConvertFromString(dsFontString,
                                            null)
                                        as
                                        DsFont;
                            }
                            else
                            {
                                reader.SkipString();
                            }

                            reader.ReadOwnedData(BackgroundInfo, context);
                            reader.ReadOwnedData(ForegroundInfo, context);
                            BorderThickness = reader.ReadThickness();
                            Padding = reader.ReadThickness();
                            reader.ReadOwnedData(BorderBrushInfo, context);
                            HorizontalContentAlignment = (HorizontalAlignment)reader.ReadInt32();
                            VerticalContentAlignment = (VerticalAlignment)reader.ReadInt32();
                            reader.ReadOwnedData(StyleInfo, context);
                            ToolTipFileRelativePath = reader.ReadString();
                            reader.ReadOwnedData(ToolTipTextInfo, context);
                            ToolTipPlacement = (PlacementMode)reader.ReadInt32();
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

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            if (DsFont is not null && ConstantsHelper.ContainsQuery(DsFont.Size))
                OnPropertyChanged(nameof(DsFont));
        }        

        #endregion

        #region private fields

        private DsFont? _dsFont;
        private BrushDataBinding _backgroundInfo = null!;
        private BrushDataBinding _foregroundInfo = null!;
        private Thickness _borderThickness;
        private Thickness _padding;
        private BrushDataBinding _borderBrushInfo = null!;
        private HorizontalAlignment _horizontalContentAlignment;
        private VerticalAlignment _verticalContentAlignment;
        private DsUIElementProperty _styleInfo = null!;
        private string _toolTipFileRelativePath = @"";
        private TextDataBinding _toolTipTextInfo = null!;
        private PlacementMode _toolTipPlacement = PlacementMode.Right;

        #endregion
    }
}