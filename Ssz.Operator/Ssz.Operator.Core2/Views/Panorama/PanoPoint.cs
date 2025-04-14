using System.Collections.Generic;
//using Avalonia.Media.Media3D;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Panorama
{
    public class PanoPoint : IOwnedDataSerializable
    {
        #region construction and destruction

        public PanoPoint()
        {
            PanoPointRefs = new List<PanoPointRef>();
        }

        #endregion

        #region public functions

        public static PanoPoint Default = new();

        public string DsPageName = "";
        public double X = double.NaN;
        public double Y = double.NaN;
        public double Z = double.NaN;


        public double CameraH;


        public List<PanoPointRef> PanoPointRefs;


        //public DiffuseMaterial Material = new();


        public int Index;


        public int DsPageMark;


        public double? UserX;


        public double? UserY;


        public double? UserZ;


        public bool Processed;


        public bool Error;

        public bool HasValidCoordinate()
        {
            return !double.IsNaN(X) && !double.IsNaN(Y) && !double.IsNaN(Z);
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(DsPageName);
                writer.Write(X);
                writer.Write(Y);
                writer.Write(Z);
                writer.Write(CameraH);
                writer.Write(PanoPointRefs);
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        DsPageName = reader.ReadString();
                        X = reader.ReadDouble();
                        Y = reader.ReadDouble();
                        Z = reader.ReadDouble();
                        CameraH = reader.ReadDouble();
                        PanoPointRefs = reader.ReadList<PanoPointRef>();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}