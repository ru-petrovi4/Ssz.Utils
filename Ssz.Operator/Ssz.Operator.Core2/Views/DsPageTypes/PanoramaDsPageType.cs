using System;
using System.ComponentModel;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsPageTypes
{
    public class PanoramaDsPageType : DsPageTypeBase
    {
        #region construction and destruction

        public PanoramaDsPageType()
        {
            X = null;
            Y = null;
            Z = null;
            PanoramaType = PanoramaType.Cylindrical;
            LeftEdgeAzimuth = 0;
            DefaultViewAzimuth = 180;
            HorizontalImageAngle = 360;
            HorizonAngle = 0;

            FrameDsPageDrawingFileName = @"";
        }

        #endregion

        #region public functions

        public static readonly Guid TypeGuid = new(@"34671689-DF7F-4650-BBF3-548F7779D4DE");

        public override Guid Guid => TypeGuid;

        public override string Name => @"Panorama";

        public override string Desc => Resources.PanoramaDsPageType_Desc;

        public override bool IsFaceplate => false;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeFrameDsPageDrawingFileName)]
        [LocalizedDescription(ResourceStrings.PanoramaDsPageTypeFrameDsPageDrawingFileNameDescription)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(1)]
        public string FrameDsPageDrawingFileName { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypePanoramaType)]
        //[PropertyOrder(2)]
        public PanoramaType PanoramaType { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeX)]
        //[PropertyOrder(3)]
        public double? X { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeY)]
        //[PropertyOrder(4)]
        public double? Y { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeZ)]
        //[PropertyOrder(5)]
        public double? Z { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeLeftEdgeAzimuth)]
        //[PropertyOrder(6)]
        public int LeftEdgeAzimuth { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeDefaultViewAzimuth)]
        //[PropertyOrder(7)]
        public int DefaultViewAzimuth { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeHorizonAngle)]
        [LocalizedDescription(ResourceStrings.PanoramaDsPageTypeHorizonAngleDescription)]
        //[PropertyOrder(8)]
        public int HorizonAngle
        {
            get => _horizonAngle;
            set
            {
                if (value > 90) value = 90;
                else if (value < -90) value = -90;
                _horizonAngle = value;
            }
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeHorizontalImageAngle)]
        //[PropertyOrder(9)]
        public int HorizontalImageAngle
        {
            get => _horizontalImageAngle;
            set
            {
                if (value > 360) value = 360;
                else if (value < 90) value = 90;
                _horizontalImageAngle = value;
            }
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.PanoramaDsPageTypeCameraH)]
        [LocalizedDescription(ResourceStrings.PanoramaDsPageTypeCameraHDescription)]
        //[PropertyOrder(13)]
        public double? CameraH { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(3))
            {
                writer.WriteNullable(X);
                writer.WriteNullable(Y);
                writer.WriteNullable(Z);
                writer.Write((int) PanoramaType);
                writer.Write(LeftEdgeAzimuth);
                writer.Write(DefaultViewAzimuth);
                writer.Write(HorizontalImageAngle);
                writer.WriteNullable((int?) 0);
                writer.Write(HorizonAngle);
                writer.Write(FrameDsPageDrawingFileName);
                writer.WriteNullable(CameraH);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 3:
                        try
                        {
                            X = reader.ReadNullableDouble();
                            Y = reader.ReadNullableDouble();
                            Z = reader.ReadNullableDouble();
                            PanoramaType = (PanoramaType) reader.ReadInt32();
                            LeftEdgeAzimuth = reader.ReadInt32();
                            DefaultViewAzimuth = reader.ReadInt32();
                            HorizontalImageAngle = reader.ReadInt32();
                            reader.ReadNullableInt32();
                            HorizonAngle = reader.ReadInt32();
                            FrameDsPageDrawingFileName = reader.ReadString();
                            CameraH = reader.ReadNullableDouble();
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

        private int _horizontalImageAngle;
        private int _horizonAngle;

        #endregion
    }

    public enum PanoramaType
    {
        Spherical = 0,
        Cylindrical = 1
    }
}