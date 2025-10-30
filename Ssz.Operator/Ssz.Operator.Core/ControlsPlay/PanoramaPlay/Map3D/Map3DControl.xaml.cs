using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Panorama;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay.Map3D
{
    public partial class Map3DControl : UserControl
    {
        #region private fields

        private bool _disposed;

        #endregion

        #region public functions

        public async void ShowAsync()
        {
            var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
            PanoPointsCollection panoPointsCollection = panoramaAddon.PanoPointsCollection;

            await Task.Delay(1);

            Model3DGroup model3DGroup = panoPointsCollection.ImportFullMap();
            foreach (Model3D model3D in model3DGroup.Children)
                MainViewport3D.Children.Add(new ModelVisual3D
                {
                    Content = model3D
                });

            await Task.Delay(400);

            MainViewport3D.ZoomExtents(2000);

            await Task.Delay(2000);

            Func<DsConstant, bool> predicate;
            if (string.IsNullOrWhiteSpace(panoramaAddon.TagsConstantsTypes))
            {
                predicate = gpi => !StringHelper.CompareIgnoreCase(gpi.Type, "dsPage");
            }
            else
            {
                string[] constantTypes =
                    panoramaAddon.TagsConstantsTypes.Split(new[] {','}, 100, StringSplitOptions.RemoveEmptyEntries);
                predicate = gpi =>
                    constantTypes.FirstOrDefault(t => StringHelper.CompareIgnoreCase(gpi.Type, t.Trim())) is not null;
            }

            var tagsAndDsPageNames = new CaseInsensitiveOrderedDictionary<string>();
            foreach (DsPageDrawing dsPage in DsProject.Instance.AllDsPagesCache.Values)
            foreach (ComplexDsShape complexDsShape in dsPage.DsShapes.OfType<ComplexDsShape>())
            foreach (var dsConstant in complexDsShape.DsConstantsCollection.Where(gpi => predicate(gpi)))
                if (dsConstant.Value != @"")
                    tagsAndDsPageNames[dsConstant.Value] = dsPage.Name;

            var itemsSource = new List<TagViewModel>();
            foreach (var kvp in tagsAndDsPageNames.OrderBy(k => k.Key))
                itemsSource.Add(new TagViewModel(kvp.Key, kvp.Value));
            TagsComboBox.ItemsSource = itemsSource;
        }

        #endregion

        #region construction and destruction

        public Map3DControl()
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

            TagsComboBox.CurrentItemChanged += TagsComboBoxOnCurrentItemChanged;
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


        ~Map3DControl()
        {
            Dispose(false);
        }

        #endregion

        #region private functions

        private void MainViewport3DOnPreviewMouseMove(object? sender, MouseEventArgs mouseEventArgs)
        {
            if (sender is null) return;
            var viewport = (HelixViewport3D) sender;
            var ptMouse = mouseEventArgs.GetPosition(viewport);
            var result = VisualTreeHelper.HitTest(viewport, ptMouse) as RayMeshGeometry3DHitTestResult;
            if (result is null) return;
            var modelVisual3D = result.VisualHit as ModelVisual3D;
            if (modelVisual3D is null) return;
            var model3DGroup = modelVisual3D.Content as Model3DGroup;
            if (model3DGroup is null) return;
            string name = model3DGroup.GetName();
            TextBlock toolTipTextBlock =
                (TreeHelper.FindParent<Map3DControl>(viewport) ?? throw new InvalidOperationException())
                .ToolTipTextBlock;
            Canvas.SetLeft(toolTipTextBlock, ptMouse.X + 12);
            Canvas.SetTop(toolTipTextBlock, ptMouse.Y + 12);
            toolTipTextBlock.Text = name;
        }

        private void TagsComboBoxOnCurrentItemChanged(TagViewModel? tagViewModel)
        {
            if (tagViewModel is null) return;

            var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();

            Dispatcher.BeginInvoke(new Action(() =>
                panoramaAddon.PanoPointsCollection.ShowPath(tagViewModel.DsPageName)));
        }

        #endregion
    }
}