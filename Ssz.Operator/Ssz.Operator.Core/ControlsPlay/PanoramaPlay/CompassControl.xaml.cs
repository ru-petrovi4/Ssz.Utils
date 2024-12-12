using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    public partial class CompassControl : UserControl
    {
        #region construction and destruction

        public CompassControl()
        {
            InitializeComponent();

            _modelVisual3D = new ModelVisual3D();
            _rotation3D = new QuaternionRotation3D();

            Loaded += CompassControlOnLoaded;
        }

        #endregion

        #region public functions

        public void SetViewAzimuth(double viewAzimuth)
        {
            _rotation3D.Quaternion = new Quaternion(AxisY, viewAzimuth);
        }

        #endregion

        #region private functions

        private static Model3DGroup? GetCompassModel3D()
        {
            if (_compassModel3D is null)
                try
                {
                    var mi = new ModelImporter();
                    // need to modyfy ModelImporter NOT to use SetName() method
                    _compassModel3D = mi.Load(AppDomain.CurrentDomain.BaseDirectory + @"Resources\compass.obj", null,
                        true);
                }
                catch
                {
                }

            return _compassModel3D;
        }

        private void CompassControlOnLoaded(object? sender, RoutedEventArgs e)
        {
            MainViewport3D.Children.Clear();

            var lightModel = new ModelVisual3D();
            lightModel.Content = new DirectionalLight(Colors.White, new Vector3D(0, -1, 0));
            MainViewport3D.Children.Add(lightModel);

            var rotateTransform = new RotateTransform3D();
            rotateTransform.Rotation = _rotation3D;
            _modelVisual3D.Transform = rotateTransform;
            _modelVisual3D.Content = GetCompassModel3D();
            MainViewport3D.Children.Add(_modelVisual3D);
        }

        #endregion

        #region private fields

        private static readonly Vector3D AxisY = new(0, 1, 0);
        private static Model3DGroup? _compassModel3D;
        private readonly ModelVisual3D _modelVisual3D;
        private readonly QuaternionRotation3D _rotation3D;

        #endregion
    }
}