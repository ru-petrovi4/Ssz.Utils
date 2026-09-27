using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ssz.Operator.Core.ControlsDesign;

namespace Ssz.Operator.Core.Design.Views;

/// <summary>
///     One opened drawing on screen: the drawing surface inside a viewbox that zooms it, inside a
///     scroller.
///     <para>
///         Ported from the WPF editor's DesignDrawingDockControl. Avalonia scrolls by an offset
///         rather than by the two ScrollToOffset calls WPF had, and the wheel arrives as a
///         PointerWheelChangedEvent handled on the way down.
///     </para>
/// </summary>
public partial class DesignDrawingView : UserControl
{
    #region construction and destruction

    public DesignDrawingView()
    {
        InitializeComponent();

        AddHandler(PointerWheelChangedEvent, OnTunnelPointerWheelChanged, RoutingStrategies.Tunnel);
    }

    #endregion

    #region public functions

    /// <summary>
    ///     Scrolls so that the given point of the drawing is in the middle of what is shown.
    /// </summary>
    public static void ShowOnViewportCenter(DesignDrawingViewModel? designDrawingViewModel,
        double drawingX, double drawingY)
    {
        if (designDrawingViewModel?.DesignControlsInfo is null) return;
        ScrollViewer? scrollViewer = designDrawingViewModel.DesignControlsInfo.ScrollViewer;
        if (scrollViewer is null) return;

        var viewScale = designDrawingViewModel.ViewScale;
        var x = ((designDrawingViewModel.BorderWidth - designDrawingViewModel.Width) / 2 + drawingX) * viewScale;
        var y = ((designDrawingViewModel.BorderHeight - designDrawingViewModel.Height) / 2 + drawingY) * viewScale;
        var horizontalOffset = x - 0.5 * scrollViewer.Viewport.Width;
        var verticalOffset = y - 0.5 * scrollViewer.Viewport.Height;
        scrollViewer.Offset = new Vector(horizontalOffset > 0 ? horizontalOffset : 0,
            verticalOffset > 0 ? verticalOffset : 0);
    }

    /// <summary>
    ///     The zoom at which the whole drawing fits in what is shown.
    /// </summary>
    public static double GetFullDrawingViewScale(ScrollViewer scrollViewer,
        DesignDrawingViewModel designDrawingViewModel)
    {
        var viewScaleX = 0.99 * scrollViewer.Viewport.Width / designDrawingViewModel.Width;
        var viewScaleY = 0.99 * scrollViewer.Viewport.Height / designDrawingViewModel.Height;

        return Math.Min(viewScaleX, viewScaleY);
    }

    #endregion

    #region protected functions

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_designDrawingViewModel is not null)
            _designDrawingViewModel.ViewScaleChanging -= OnViewScaleChanging;

        _designDrawingViewModel = DataContext as DesignDrawingViewModel;

        if (_designDrawingViewModel is not null)
            _designDrawingViewModel.ViewScaleChanging += OnViewScaleChanging;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        DesignDrawingViewModel? designDrawingViewModel = _designDrawingViewModel;
        if (designDrawingViewModel is null) return;

        // The tab of a drawing may be moved, tabbed or floated, which takes the surface out of the
        // tree and puts it back, so the zoom is listened to again here.
        designDrawingViewModel.ViewScaleChanging -= OnViewScaleChanging;
        designDrawingViewModel.ViewScaleChanging += OnViewScaleChanging;

        if (designDrawingViewModel.DesignControlsInfo is not null) return;

        DesignDrawingBorder.DesignDrawingCanvas.Initialize();

        designDrawingViewModel.Initialize(
            new DesignControlsInfo(DesignDrawingBorder.DesignDrawingCanvas) { ScrollViewer = MainScrollViewer });

        // The scroller knows its size only after it is laid out, and the zoom depends on it.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var viewScale = GetFullDrawingViewScale(MainScrollViewer, designDrawingViewModel);
            if (viewScale > 1) viewScale = 1;
            DesignDsProjectViewModel.Instance.SetDesignDrawingViewScale(viewScale, null);
            ShowOnViewportCenter(designDrawingViewModel, designDrawingViewModel.Width / 2,
                designDrawingViewModel.Height / 2);
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_designDrawingViewModel is not null)
            _designDrawingViewModel.ViewScaleChanging -= OnViewScaleChanging;

        base.OnDetachedFromVisualTree(e);
    }

    #endregion

    #region private functions

    /// <summary>
    ///     Keeps the point under the pointer where it is while the zoom changes.
    /// </summary>
    private void OnViewScaleChanging(double oldValue, double newValue, Point? immovableRelativePoint)
    {
        if (!immovableRelativePoint.HasValue || oldValue == 0.0) return;

        var x = immovableRelativePoint.Value.X * MainScrollViewer.Viewport.Width;
        var y = immovableRelativePoint.Value.Y * MainScrollViewer.Viewport.Height;
        var centerPointAtDrawing = new Point((MainScrollViewer.Offset.X + x) / oldValue,
            (MainScrollViewer.Offset.Y + y) / oldValue);
        var horizontalOffset = newValue * centerPointAtDrawing.X - x;
        var verticalOffset = newValue * centerPointAtDrawing.Y - y;
        MainScrollViewer.Offset = new Vector(horizontalOffset > 0 ? horizontalOffset : 0,
            verticalOffset > 0 ? verticalOffset : 0);
    }

    private void OnTunnelPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            var offset = MainScrollViewer.Offset.X + e.Delta.Y * WheelScrollStep;
            MainScrollViewer.Offset = new Vector(offset > 0 ? offset : 0, MainScrollViewer.Offset.Y);
            e.Handled = true;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            Point pointerPosition = e.GetPosition(MainScrollViewer);
            var immovableRelativePoint = new Point(pointerPosition.X / MainScrollViewer.Viewport.Width,
                pointerPosition.Y / MainScrollViewer.Viewport.Height);
            var value = DesignDsProjectViewModel.Instance.DesignDrawingViewScale;
            if (e.Delta.Y > 0) value = value * 1.3;
            else value = value / 1.3;
            DesignDsProjectViewModel.Instance.SetDesignDrawingViewScale(value, immovableRelativePoint);
            e.Handled = true;
        }
    }

    #endregion

    #region private fields

    /// <summary>
    ///     WPF's wheel delta counted in 120ths of a turn; Avalonia counts in turns, so a turn scrolls
    ///     about as far as it used to.
    /// </summary>
    private const double WheelScrollStep = 120.0;

    private DesignDrawingViewModel? _designDrawingViewModel;

    #endregion
}
