using System.Windows.Media.Media3D;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Panorama
{
    public class PanoPointRef : IOwnedDataSerializable
    {
        #region public functions

        public string ToDsPageName = "";
        public double HorizontalAngle = double.NaN;
        public double HorizontalLength = double.NaN;
        public double VerticalDelta = double.NaN;


        public PanoPoint ParentPanoPoint = PanoPoint.Default;


        public PanoPoint ToPanoPoint = PanoPoint.Default;


        public PanoPointRef? MutualPanoPointRef;


        public DiffuseMaterial Material = new();


        public double UserHorizontalAngle;


        public double UserHorizontalLength;


        public double UserVerticalDelta;


        public int IndexInPath;


        public bool Processed;


        public bool Error;

        public bool HasValidVector()
        {
            return !double.IsNaN(HorizontalAngle) &&
                   !double.IsNaN(HorizontalLength) &&
                   !double.IsNaN(VerticalDelta);
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(ToDsPageName);
                writer.Write(HorizontalLength);
                writer.Write(HorizontalAngle);
                writer.Write(VerticalDelta);
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        ToDsPageName = reader.ReadString();
                        HorizontalLength = reader.ReadDouble();
                        HorizontalAngle = reader.ReadDouble();
                        VerticalDelta = reader.ReadDouble();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override string ToString()
        {
            return ParentPanoPoint.DsPageName + " -> " + ToPanoPoint.DsPageName;
        }

        #endregion
    }
}