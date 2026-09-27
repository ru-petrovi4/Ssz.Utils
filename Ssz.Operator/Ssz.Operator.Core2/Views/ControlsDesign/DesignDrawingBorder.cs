using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Ssz.Operator.Core.ControlsDesign.GeometryEditing;
using Ssz.Operator.Core.DsShapes;
using Ssz.Utils.MonitoredUndo;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     What surrounds the drawing in the editor: the checkered area around it, the handles that
    ///     resize the page, and all the pointer work - picking shapes, dragging the handles of a
    ///     geometry, and the rubber band that selects several shapes at once.
    ///     <para>
    ///         Ported from the WPF editor. WPF's preview (tunnelling) mouse events become Avalonia
    ///         handlers registered with RoutingStrategies.Tunnel, and the mouse position that WPF read
    ///         from the static Mouse class is remembered from the last pointer event instead.
    ///     </para>
    /// </summary>
    public class DesignDrawingBorder : Border
    {
        #region construction and destruction

        public DesignDrawingBorder()
        {
            DesignDrawingCanvas = new DesignDrawingCanvas();

            _resizeDecorator = CreateResizeDecorator();

            var drawingGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            drawingGrid.Children.Add(DesignDrawingCanvas);
            drawingGrid.Children.Add(_resizeDecorator);
            drawingGrid.Bind(WidthProperty, new Binding
            {
                Path = nameof(DesignDrawingViewModel.Width),
                Mode = BindingMode.OneWay
            });
            drawingGrid.Bind(HeightProperty, new Binding
            {
                Path = nameof(DesignDrawingViewModel.Height),
                Mode = BindingMode.OneWay
            });

            Child = drawingGrid;

            ContextMenuHelper.Attach(this, ContextMenuHelper.DesignDrawingContextMenuKey);

            object? checkerBrush = null;
            Application.Current?.TryFindResource(@"CheckerBrush", out checkerBrush);
            Background = checkerBrush as IBrush ?? new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));

            AddHandler(PointerPressedEvent, OnTunnelPointerPressed, RoutingStrategies.Tunnel);
            AddHandler(PointerMovedEvent, OnTunnelPointerMoved, RoutingStrategies.Tunnel);
            AddHandler(PointerReleasedEvent, OnTunnelPointerReleased, RoutingStrategies.Tunnel);
            AddHandler(ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
        }

        #endregion

        #region public functions

        public DesignDrawingCanvas DesignDrawingCanvas { get; }

        #endregion

        #region protected functions

        protected DesignDrawingViewModel DrawingViewModel => (DesignDrawingViewModel) DataContext!;

        #endregion

        #region private functions

        private Control CreateResizeDecorator()
        {
            var grid = new Grid { Opacity = 0.3 };

            void Add(double width, double height, HorizontalAlignment h, VerticalAlignment v, Thickness margin,
                StandardCursorType cursor)
            {
                var thumb = new DesignDrawingResizeThumb
                {
                    HorizontalAlignment = h,
                    VerticalAlignment = v,
                    Margin = margin,
                    Cursor = new Cursor(cursor)
                };
                if (!Double.IsNaN(width)) thumb.Width = width;
                if (!Double.IsNaN(height)) thumb.Height = height;
                grid.Children.Add(thumb);
            }

            Add(Double.NaN, 2, HorizontalAlignment.Stretch, VerticalAlignment.Top,
                new Thickness(0, -10, 0, 0), StandardCursorType.SizeNorthSouth);
            Add(2, Double.NaN, HorizontalAlignment.Left, VerticalAlignment.Stretch,
                new Thickness(-10, 0, 0, 0), StandardCursorType.SizeWestEast);
            Add(2, Double.NaN, HorizontalAlignment.Right, VerticalAlignment.Stretch,
                new Thickness(0, 0, -10, 0), StandardCursorType.SizeWestEast);
            Add(Double.NaN, 2, HorizontalAlignment.Stretch, VerticalAlignment.Bottom,
                new Thickness(0, 0, 0, -10), StandardCursorType.SizeNorthSouth);
            Add(5, 5, HorizontalAlignment.Left, VerticalAlignment.Top,
                new Thickness(-12, -12, 0, 0), StandardCursorType.TopLeftCorner);
            Add(5, 5, HorizontalAlignment.Right, VerticalAlignment.Top,
                new Thickness(0, -12, -12, 0), StandardCursorType.TopRightCorner);
            Add(5, 5, HorizontalAlignment.Left, VerticalAlignment.Bottom,
                new Thickness(-12, 0, 0, -12), StandardCursorType.BottomLeftCorner);
            Add(5, 5, HorizontalAlignment.Right, VerticalAlignment.Bottom,
                new Thickness(0, 0, -12, -12), StandardCursorType.BottomRightCorner);

            grid.Bind(IsVisibleProperty, new Binding
            {
                Path = nameof(DesignDrawingViewModel.ResizeDecoratorIsVisible),
                Mode = BindingMode.OneWay
            });

            return grid;
        }

        private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            DrawingViewModel.CurrentCursorPoint = _lastPointOnCanvas;
        }

        private void OnTunnelPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            PointerPointProperties properties = e.GetCurrentPoint(this).Properties;
            var isLeftButton = properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;

            Point point = e.GetPosition(DesignDrawingCanvas);
            _lastPointOnCanvas = point;
            DsShapeViews.DsShapeViewBase? dsShapeView = DesignDrawingCanvas.GetRootDsShapeViewAt(point);

            _rubberbandSelectionStartPoint = null;
            var noRubberband = dsShapeView is not null && dsShapeView.DsShapeViewModel.IsSelected;

            if (dsShapeView is null)
                DrawingViewModel.SelectionService.UpdateSelection(null, e.KeyModifiers);
            else
                DrawingViewModel.SelectionService.UpdateSelection(dsShapeView.DsShapeViewModel, e.KeyModifiers);

            if (e.ClickCount > 1 && isLeftButton)
            {
                if (dsShapeView is null)
                    DesignDsProjectViewModel.Instance.ShowDrawingPropertiesWindow(DrawingViewModel);
                else
                    DesignDsProjectViewModel.Instance.ShowFirstSelectedDsShapePropertiesWindow(DrawingViewModel);

                e.Handled = true;
                return;
            }

            if (e.ClickCount == 1)
                foreach (DesignGeometryDsShapeView designerGeometryDsShapeView in
                         DesignDrawingCanvas.Children.OfType<DesignGeometryDsShapeView>()
                             .Where(dgsv => dgsv.DsShapeViewModel.IsSelected)
                             .OrderByDescending(dgsv => dgsv.DsShapeViewModel.DsShape.Index))
                {
                    var inResizeThumb = false;
                    Point dsShapePoint =
                        designerGeometryDsShapeView.DsShapeViewModel.DsShape.GetDsShapePoint(point);
                    if (designerGeometryDsShapeView.DsShapeViewModel.IsSelected)
                        if (designerGeometryDsShapeView.DsShapeViewModel.DsShape.ResizeThumbContains(dsShapePoint))
                            inResizeThumb = true;
                    if (inResizeThumb) break;

                    foreach (ControlPoint cp in designerGeometryDsShapeView.ControlPointsOrdered)
                    {
                        DragInfo? di = cp.HitTest(dsShapePoint);
                        if (di.HasValue)
                        {
                            designerGeometryDsShapeView.SelectedControlPoint = cp;

                            if (isLeftButton)
                            {
                                _dragInfo = di;
                                _dragInfo.Value.DragObject.StartDrag();
                                e.Handled = true;
                                return;
                            }

                            break;
                        }
                    }
                }

            if (isLeftButton && !noRubberband)
                _rubberbandSelectionStartPoint = e.GetPosition(this);
        }

        private void OnTunnelPointerMoved(object? sender, PointerEventArgs e)
        {
            var isLeftButtonPressed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;

            _lastPointOnCanvas = e.GetPosition(DesignDrawingCanvas);

            // Everything one drag does is one step of the undo history.
            if (isLeftButtonPressed)
            {
                if (!_inChangeSetBatch)
                {
                    UndoService.Current[DrawingViewModel.Drawing.GetUndoRoot()].BeginChangeSetBatch(@"Move", true);
                    _inChangeSetBatch = true;
                }
            }
            else
            {
                if (_inChangeSetBatch)
                {
                    _inChangeSetBatch = false;
                    UndoService.Current[DrawingViewModel.Drawing.GetUndoRoot()].EndChangeSetBatch();
                }
            }

            if (_dragInfo.HasValue)
            {
                if (_dragInfo.Value.DragObject is not null)
                {
                    Point point = e.GetPosition(_dragInfo.Value.RelativeTo as Visual ?? this);
                    _dragInfo.Value.DragObject.DragObject(point - _dragInfo.Value.Offset);
                    e.Handled = true;
                }

                return;
            }

            if (isLeftButtonPressed)
            {
                Point position = e.GetPosition(this);

                if (_rubberbandAdorner is null)
                    if (_rubberbandSelectionStartPoint.HasValue)
                    {
                        Point rubberbandSelectionStartPoint = _rubberbandSelectionStartPoint.Value;
                        // A few pixels of slack, so a click that wobbles is still a click.
                        if (Math.Abs(position.X - rubberbandSelectionStartPoint.X) > 3 ||
                            Math.Abs(position.Y - rubberbandSelectionStartPoint.Y) > 3)
                        {
                            AddAdorner(new RubberbandAdorner(this, rubberbandSelectionStartPoint));
                            _rubberbandSelectionStartPoint = null;
                        }
                    }

                if (_rubberbandAdorner is not null)
                {
                    _rubberbandAdorner.EndPoint = position;

                    Point? relativePoint = DesignDrawingCanvas.TranslatePoint(new Point(0, 0), this);
                    var rubberBand = new Rect(_rubberbandAdorner.StartPoint, _rubberbandAdorner.EndPoint);
                    if (relativePoint is not null)
                        rubberBand = rubberBand.Translate(new Vector(-relativePoint.Value.X,
                            -relativePoint.Value.Y));

                    DesignDsProjectViewModel.Instance.UpdateSelection(rubberBand);
                }

                return;
            }

            _rubberbandSelectionStartPoint = null;

            if (_rubberbandAdorner is not null) RemoveAdorner(_rubberbandAdorner);
        }

        private void OnTunnelPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton != MouseButton.Left) return;

            if (_dragInfo.HasValue)
            {
                if (_dragInfo.Value.DragObject is not null)
                {
                    _dragInfo.Value.DragObject.EndDrag();
                    e.Handled = true;
                }

                _dragInfo = null;
            }

            if (_inChangeSetBatch)
            {
                _inChangeSetBatch = false;
                UndoService.Current[DrawingViewModel.Drawing.GetUndoRoot()].EndChangeSetBatch();
            }

            _rubberbandSelectionStartPoint = null;

            if (_rubberbandAdorner is not null) RemoveAdorner(_rubberbandAdorner);

            // What is selected has just changed, and with it what the ribbon may offer.
            Utils.RoutedCommand.InvalidateRequerySuggested();
        }

        private void AddAdorner(RubberbandAdorner rubberbandAdorner)
        {
            if (_rubberbandAdorner is not null) return;

            AdornerLayer? adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer is null) return;

            _rubberbandAdorner = rubberbandAdorner;
            AdornerLayer.SetAdornedElement(rubberbandAdorner, this);
            adornerLayer.Children.Add(rubberbandAdorner);
        }

        private void RemoveAdorner(RubberbandAdorner rubberbandAdorner)
        {
            _rubberbandAdorner = null;
            AdornerLayer? adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer is not null) adornerLayer.Children.Remove(rubberbandAdorner);
        }

        #endregion

        #region private fields

        private readonly Control _resizeDecorator;
        private RubberbandAdorner? _rubberbandAdorner;
        private Point? _rubberbandSelectionStartPoint;
        private bool _inChangeSetBatch;
        private DragInfo? _dragInfo;
        private Point _lastPointOnCanvas;

        #endregion
    }
}
