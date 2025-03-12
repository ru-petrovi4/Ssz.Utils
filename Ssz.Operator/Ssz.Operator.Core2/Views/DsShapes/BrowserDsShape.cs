using System;
using System.ComponentModel;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class BrowserDsShape : DsShapeBase
    {
        #region private fields

        private string _url = @"";

        #endregion

        #region construction and destruction

        public BrowserDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public BrowserDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 300;
            HeightInitial = 200;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Browser";
        public static readonly Guid DsShapeTypeGuid = new(@"95EC8D69-0B11-4B75-8701-0713D878EFFA");

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.BrowserDsShapeUrl)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(1)]
        public string Url
        {
            get => _url;
            set => SetValue(ref _url, value);
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
                writer.Write(Url);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedDataAsync(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            Url = reader.ReadString();
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