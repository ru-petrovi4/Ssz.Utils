using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ssz.Operator.Core.Panorama;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    public class PanoramaLinesViewport3D : Viewport3D, IDisposable
    {
        #region construction and destruction

        public PanoramaLinesViewport3D()
        {
            _mapModelVisual3D = new ModelVisual3D();
            MapRotation3D = new QuaternionRotation3D();
            MapTranslateTransform3D = new TranslateTransform3D();
            PerspectiveCamera = new PerspectiveCamera();
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.
            }

            // Release unmanaged resources.
            // Set large fields to null.            
            _disposed = true;
        }


        ~PanoramaLinesViewport3D()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public bool IsInited { get; private set; }

        public PerspectiveCamera PerspectiveCamera { get; }

        public QuaternionRotation3D MapRotation3D { get; }

        public TranslateTransform3D MapTranslateTransform3D { get; }

        public PanoPoint? PanoPoint
        {
            get => _panoPoint;
            set
            {
                _panoPoint = value;
                if (_panoPoint is null)
                {
                    _mapModelVisual3D!.Content = null;
                }
                else
                {
                    if (MapTranslateTransform3D is null) throw new InvalidOperationException();
                    MapTranslateTransform3D.OffsetZ = -_panoPoint.Z - _panoPoint.CameraH;
                    _mapModelVisual3D!.Content = _panoPointsCollection!.ImportNearPoint(_panoPoint);
                }
            }
        }


        public void Init(PanoPointsCollection panoPointsCollection)
        {
            _panoPointsCollection = panoPointsCollection;

            //PerspectiveCamera.FarPlaneDistance = 10;
            PerspectiveCamera.Position = new Point3D(0, 0, 0);
            PerspectiveCamera.UpDirection = new Vector3D(0, 0, 1);
            PerspectiveCamera.LookDirection = new Vector3D(1, 0, 0);
            Camera = PerspectiveCamera;

            // Light Model Initialize
            var lightModel = new ModelVisual3D();
            lightModel.Content = new DirectionalLight(Colors.White, new Vector3D(1, 0, 0));
            Children.Add(lightModel);

            var transform = new Transform3DGroup();
            transform.Children.Add(MapTranslateTransform3D);
            var rotateTransform = new RotateTransform3D();
            transform.Children.Add(rotateTransform);
            rotateTransform.Rotation = MapRotation3D;
            _mapModelVisual3D.Transform = transform;
            Children.Add(_mapModelVisual3D);

            IsInited = true;
        }

        #endregion

        #region private fields

        private bool _disposed;
        private PanoPoint? _panoPoint;
        private readonly ModelVisual3D _mapModelVisual3D;


        private PanoPointsCollection _panoPointsCollection = new();

        #endregion
    }
}