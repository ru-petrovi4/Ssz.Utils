using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils.Serialization; 
//using Ssz.Operator.Play.Ctcm.CustomAttributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class AlarmListDsShape : ControlDsShape
    {
        public enum AppearanceType
        {
            Generic = 0
        }

        #region private fields

        private AppearanceType _type;

        #endregion

        #region construction and destruction

        public AlarmListDsShape() // For XAML serialization
            : this(true, true)
        {
        }


        public AlarmListDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 120;
            HeightInitial = 60;

            BackgroundInfo.ConstValue = new SolidDsBrush
            {
                Color = Colors.Black
            };
            ForegroundInfo.ConstValue = new SolidDsBrush
            {
                Color = Colors.LightGray
            };

            base.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            base.VerticalContentAlignment = VerticalAlignment.Stretch;

            Type = AppearanceType.Generic;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "AlarmList";
        public static readonly Guid DsShapeTypeGuid = new(@"E5574014-4988-4769-A9DC-A740D7D8FFF7");

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

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.AlarmListDsShapeAppearanceType)]
        public AppearanceType Type
        {
            get => _type;
            set => SetValue(ref _type, value);
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
                writer.Write((int) Type);
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
                            Type = (AppearanceType) reader.ReadInt32();
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