using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core.Panorama
{
    public class PanoPointsCollection : IOwnedDataSerializable
    {
        #region private fields

        private PanoPointRef[] _currentPath;

        #endregion

        #region construction and destruction

        public PanoPointsCollection()
        {
            PanoPoints = new List<PanoPoint>();
            PanoPointsDictionary = new CaseInsensitiveDictionary<PanoPoint>();
            StartDsPageName = "";
            _currentPath = new PanoPointRef[0];
        }

        #endregion

        #region public functions

        public List<PanoPoint> PanoPoints { get; private set; }


        public CaseInsensitiveDictionary<PanoPoint> PanoPointsDictionary { get; private set; }

        public string StartDsPageName { get; set; }

        public PanoPoint? CurrentPoint { get; set; }


        public PanoPointRef[] CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;
                var currentPathChanged = CurrentPathChanged;
                if (currentPathChanged is not null) currentPathChanged();
            }
        }

        public event Action? CurrentPathChanged;

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(PanoPoints);
                writer.Write(StartDsPageName);
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        PanoPoints = reader.ReadList<PanoPoint>();
                        StartDsPageName = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void Initialize()
        {
            PanoPointsDictionary = new CaseInsensitiveDictionary<PanoPoint>(PanoPoints.Count);
            foreach (PanoPoint panoPoint in PanoPoints) PanoPointsDictionary.Add(panoPoint.DsPageName, panoPoint);

            var index = 0;
            foreach (PanoPoint panoPoint in PanoPoints)
            {
                panoPoint.Material = new DiffuseMaterial();
                panoPoint.Index = index;

                foreach (PanoPointRef panoPointRef in panoPoint.PanoPointRefs.ToArray())
                {
                    panoPointRef.ParentPanoPoint = panoPoint;
                    panoPointRef.ToPanoPoint = PanoPointsDictionary.TryGetValue(panoPointRef.ToDsPageName) ??
                                               throw new InvalidOperationException();
                    panoPointRef.Material = new DiffuseMaterial();
                    panoPointRef.Material.Brush = new SolidColorBrush(Colors.Cyan);
                }

                index += 1;
            }

            foreach (PanoPoint panoPoint in PanoPoints)
            foreach (PanoPointRef panoPointRef in panoPoint.PanoPointRefs.ToArray())
                panoPointRef.MutualPanoPointRef =
                    panoPointRef.ToPanoPoint.PanoPointRefs.FirstOrDefault(
                        r => ReferenceEquals(r.ToPanoPoint, panoPoint));
        }

        public void Clear()
        {
            PanoPoints.Clear();
            PanoPointsDictionary.Clear();

            StartDsPageName = "";
        }

        #endregion
    }
}