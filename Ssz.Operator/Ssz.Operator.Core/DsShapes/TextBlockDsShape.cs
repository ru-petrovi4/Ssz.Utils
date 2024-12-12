using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public class TextBlockDsShape : DsShapeBase
    {
        #region construction and destruction

        public TextBlockDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public TextBlockDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 80;
            HeightInitial = 30;

            ForegroundInfo = new BrushDataBinding(visualDesignMode, loadXamlContent)
            {
                ConstValue = new SolidDsBrush {Color = Colors.Black}
            };
            TextInfo = new TextDataBinding(visualDesignMode, loadXamlContent) {ConstValue = "text"};
            DsFont = null;
            TextDecorations = null;
            VerticalAlignment = VerticalAlignment.Center;
            TextAlignment = TextAlignment.Left;
            TextWrapping = TextWrapping.Wrap;
            TextStretch = Stretch.None;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Label";
        public static readonly Guid DsShapeTypeGuid = new(@"4A916E51-1FBB-4D33-AEE9-00DFB0805CEF");

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextDsShapeDsFont)]
        [Editor(typeof(DsFontTypeEditor), typeof(DsFontTypeEditor))]
        public DsFont? DsFont
        {
            get => _dsFont;
            set => SetValue(ref _dsFont, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextDsShapeTextDecorations)]
        [ItemsSource(typeof(TextDecorationsItemsSource), true)]
        [DefaultValue(null)] // For XAML serialization
        public string? TextDecorations
        {
            get => _textDecorations;
            set => SetValue(ref _textDecorations, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextBlockDsShapeForegroundInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        public BrushDataBinding ForegroundInfo
        {
            get => _foregroundInfo;
            set => SetValue(ref _foregroundInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.TextBlockDsShapeVerticalAlignment)]
        public VerticalAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set => SetValue(ref _verticalAlignment, value);
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

            using (writer.EnterBlock(4))
            {
                writer.Write(ForegroundInfo, context);
                writer.Write(TextInfo, context);
                writer.Write(NameValueCollectionValueSerializer<DsFont>.Instance.ConvertToString(DsFont, null));
                writer.Write(TextDecorations);
                writer.Write((int) VerticalAlignment);
                writer.Write((int) TextAlignment);
                writer.Write((int) TextWrapping);
                writer.Write((int) TextStretch);
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
                            reader.ReadOwnedData(ForegroundInfo, context);
                            reader.ReadOwnedData(TextInfo, context);
                            if (LoadXamlContent)
                            {
                                string dsFontString = reader.ReadString();
                                DsFont =
                                    NameValueCollectionValueSerializer<DsFont>.Instance.ConvertFromString(dsFontString,
                                        null) as DsFont;
                            }
                            else
                            {
                                reader.SkipString();
                            }

                            TextDecorations = reader.ReadString();
                            VerticalAlignment = (VerticalAlignment) reader.ReadInt32();
                            TextAlignment = (TextAlignment) reader.ReadInt32();
                            TextWrapping = (TextWrapping) reader.ReadInt32();
                            TextStretch = (Stretch) reader.ReadInt32();
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
            if (TextDecorations is not null && ConstantsHelper.ContainsQuery(TextDecorations))
                OnPropertyChanged(nameof(TextDecorations));
        }

        #endregion

        #region private fields

        private BrushDataBinding _foregroundInfo = null!;
        private TextDataBinding _textInfo = null!;
        private Stretch _textStretch;
        private DsFont? _dsFont;
        private string? _textDecorations;
        private VerticalAlignment _verticalAlignment;
        private TextAlignment _textAlignment;
        private TextWrapping _textWrapping;

        #endregion
    }

    public class TextDecorationsItemsSource : IItemsSource
    {
        #region public functions

        public ItemCollection GetValues()
        {
            var commands = new ItemCollection();
            commands.Add(@"OverLine");
            commands.Add(@"Strikethrough");
            commands.Add(@"Baseline");
            commands.Add(@"Underline");
            return commands;
        }

        #endregion
    }
}