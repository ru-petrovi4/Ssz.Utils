using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Ssz.Utils.MonitoredUndo;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Design.Controls
{
    /// <summary>
    ///     Interaction logic for DrawingsCollectionControlBase.xaml
    /// </summary>
    public partial class DesignDrawingDockControl : UserControl
    {
        #region construction and destruction

        public DesignDrawingDockControl()
        {
            InitializeComponent();

            DataContextChanged += DesignDrawingDockControl_OnDataContextChanged;            
        }        

        #endregion

        #region internal functions

        internal static void ShowOnViewportCenter(DesignDrawingViewModel? designDrawingViewModel, double drawingX, double drawingY)
        {
            if (designDrawingViewModel is null || designDrawingViewModel.DesignControlsInfo is null) return; 
            ScrollViewer? scrollViewer = designDrawingViewModel.DesignControlsInfo.ScrollViewer;
            if (scrollViewer is null) return;
            double viewScale = designDrawingViewModel.ViewScale;
            double x = ((designDrawingViewModel.BorderWidth -
                         designDrawingViewModel.Width)/2 + drawingX)*viewScale;
            double y = ((designDrawingViewModel.BorderHeight -
                         designDrawingViewModel.Height)/2 + drawingY)*viewScale;
            double horizontalOffset = x - 0.5*scrollViewer.ViewportWidth;
            double verticalOffset = y - 0.5*scrollViewer.ViewportHeight;
            scrollViewer.ScrollToHorizontalOffset(horizontalOffset > 0 ? horizontalOffset : 0);
            scrollViewer.ScrollToVerticalOffset(verticalOffset > 0 ? verticalOffset : 0);
        }

        internal static double GetFullDrawingViewScale(ScrollViewer scrollViewer, DesignDrawingViewModel designDrawingViewModel)
        {
            double viewScaleX = 0.99*scrollViewer.ViewportWidth/designDrawingViewModel.Width;
            double viewScaleY = 0.99*scrollViewer.ViewportHeight/designDrawingViewModel.Height;

            return Math.Min(viewScaleX, viewScaleY);
        }

        #endregion

        #region private functions

        private DesignDrawingViewModel? DesignDrawingViewModel => (DesignDrawingViewModel?)DataContext;

        private void DesignDrawingDockControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DesignDrawingViewModel is not null)
                DesignDrawingViewModel.ViewScaleChanging += DesignDrawingViewModel_OnViewScaleChanging;
        }

        private void MainScrollViewerOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var scrollViewer = (ScrollViewer) sender;
            var dsignerDrawingBorder = (DesignDrawingBorder) scrollViewer.FindName("DesignDrawingBorder");
            var designDrawingViewModel = DesignDrawingViewModel;
            if (designDrawingViewModel is null)
                return;

            dsignerDrawingBorder.DesignDrawingCanvas.Initialize();

            designDrawingViewModel.Initialize(new DesignControlsInfo(dsignerDrawingBorder.DesignDrawingCanvas) { ScrollViewer = scrollViewer });

            double viewScale = GetFullDrawingViewScale(scrollViewer, designDrawingViewModel);
            if (viewScale > 1) viewScale = 1;
            DesignDsProjectViewModel.Instance.SetDesignDrawingViewScale(viewScale, null);
            ShowOnViewportCenter(designDrawingViewModel, designDrawingViewModel.Width / 2,
                designDrawingViewModel.Height / 2);            
        }

        private void DesignDrawingViewModel_OnViewScaleChanging(double oldValue, double newValue,
            Point? immovableRelativePoint)
        {			
            if (!immovableRelativePoint.HasValue) return;

			double x = immovableRelativePoint.Value.X * MainScrollViewer.ViewportWidth;
			double y = immovableRelativePoint.Value.Y * MainScrollViewer.ViewportHeight;
			var centerPointAtDrawing = new Point((MainScrollViewer.HorizontalOffset + x) / oldValue,
				(MainScrollViewer.VerticalOffset + y) / oldValue);
            double horizontalOffset = newValue*centerPointAtDrawing.X - x;
            double verticalOffset = newValue*centerPointAtDrawing.Y - y;
			MainScrollViewer.ScrollToHorizontalOffset(horizontalOffset > 0 ? horizontalOffset : 0);
			MainScrollViewer.ScrollToVerticalOffset(verticalOffset > 0 ? verticalOffset : 0);
        }

        private void MainScrollViewerOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
				double offset = MainScrollViewer.HorizontalOffset;
                offset = offset + e.Delta;
				MainScrollViewer.ScrollToHorizontalOffset(offset > 0 ? offset : 0);
                e.Handled = true;
            }
            /*
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                double offset = MainScrollViewer.VerticalOffset;
                offset = offset + e.Delta;
                MainScrollViewer.ScrollToVerticalOffset(offset > 0 ? offset : 0);
                e.Handled = true;
            }*/
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Point mousePosition = e.GetPosition(MainScrollViewer);
				var immovableRelativePoint = new Point(mousePosition.X / MainScrollViewer.ViewportWidth,
					mousePosition.Y / MainScrollViewer.ViewportHeight);
                double value = DesignDsProjectViewModel.Instance.DesignDrawingViewScale;
                if (e.Delta > 0) value = value*1.3;
                else value = value/1.3;
                DesignDsProjectViewModel.Instance.SetDesignDrawingViewScale(value, immovableRelativePoint);
                e.Handled = true;
            }
        }        

        #endregion
    }
}