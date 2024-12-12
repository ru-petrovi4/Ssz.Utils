using System;
using System.ComponentModel;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class ContentDsShape : DsShapeBase
    {
        #region private fields

        private XamlDataBinding _contentInfo = null!;

        #endregion

        #region construction and destruction

        public ContentDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ContentDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 80;
            HeightInitial = 30;

            ContentInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Content";
        public static readonly Guid DsShapeTypeGuid = new(@"E71D47F2-9C7B-4E55-8374-5FA9F85EE029");

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ContentDsShapeContentInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
        public XamlDataBinding ContentInfo
        {
            get => _contentInfo;
            set => SetValue(ref _contentInfo, value);
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
                writer.Write(ContentInfo, context);
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
                            reader.ReadOwnedData(ContentInfo, context);
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
    }
}