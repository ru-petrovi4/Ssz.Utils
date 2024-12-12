using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Drawings;

namespace Ssz.Operator.Core.ControlsPlay.WpfModel3DPlay
{
    public partial class WpfModel3DPlayControl : PlayControlBase
    {
        #region private functions

        private async void ViewModelOnPropertyChangedAsync(object? sender,
            PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "CurrentModel")
            {
                await Task.Delay(400);
                MainViewport3D.ZoomExtents(2000);
            }
        }

        #endregion

        #region construction and destruction

        public WpfModel3DPlayControl(IPlayWindow playWindow) :
            base(playWindow)
        {
            InitializeComponent();

            var wpfModel3DPlayWindowParams = DsProject.Instance.GetAddon<WpfModel3DAddon>();
            if (wpfModel3DPlayWindowParams.DsFont is not null)
            {
                double size;
                double.TryParse(
                    ConstantsHelper.ComputeValue(DsProject.Instance, wpfModel3DPlayWindowParams.DsFont.Size),
                    NumberStyles.Any, CultureInfo.InvariantCulture,
                    out size);
                if (size > 0.0)
                    MainViewport3D.TitleSize = size;
                MainViewport3D.TitleFontFamily = wpfModel3DPlayWindowParams.DsFont.Family;
            }

            if (wpfModel3DPlayWindowParams.BackgroundDsBrush is not null)
                MainViewport3D.Background = wpfModel3DPlayWindowParams.BackgroundDsBrush.GetBrush(DsProject.Instance);
            if (wpfModel3DPlayWindowParams.TitleDsBrush is not null)
                MainViewport3D.TextBrush = wpfModel3DPlayWindowParams.TitleDsBrush.GetBrush(DsProject.Instance);
            if (wpfModel3DPlayWindowParams.AmbientLightColor != default)
            {
                var mg = new ModelVisual3D {Content = new AmbientLight(wpfModel3DPlayWindowParams.AmbientLightColor)};
                MainViewport3D.Children.Add(mg);
            }
            else
            {
                var mg = new ModelVisual3D {Content = new AmbientLight(Color.FromRgb(20, 20, 20))};
                MainViewport3D.Children.Add(mg);
            }

            var viewModel = new WpfModel3DPlayViewModel();
            viewModel.PropertyChanged += ViewModelOnPropertyChangedAsync;

            DataContext = viewModel;
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
                // Release and Dispose managed resources.
                ((WpfModel3DPlayViewModel) DataContext).PropertyChanged -= ViewModelOnPropertyChangedAsync;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public override void Jump(JumpInfo jumpInfo, DsPageDrawingInfo dsPageDrawingInfo)
        {
            throw new NotImplementedException();
        }

        public override bool Jump(JumpInfo jumpInfo)
        {
            string fileFullName = DsProject.Instance.GetFileFullName(jumpInfo.FileRelativePath);

            ((WpfModel3DPlayViewModel) DataContext).JumpAsync(fileFullName);

            return true;
        }

        #endregion
    }
}