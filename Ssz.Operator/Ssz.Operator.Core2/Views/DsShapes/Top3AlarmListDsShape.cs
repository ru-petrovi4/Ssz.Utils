using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;


using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DsShapes
{
    public class Top3AlarmListDsShape : ControlDsShape
    {
        #region construction and destruction

        public Top3AlarmListDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public Top3AlarmListDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 120;
            HeightInitial = 60;

            BackgroundInfo.ConstValue = new SolidDsBrush
            {
                Color = Colors.Black
            };

            base.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            base.VerticalContentAlignment = VerticalAlignment.Stretch;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Top3AlarmList";
        public static readonly Guid DsShapeTypeGuid = new(@"33697553-C44A-4186-9AFB-DC02E6A13546");

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

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
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
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}