using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Ssz.Operator.Core.ControlsDesign.GeometryEditing;
using Ssz.Operator.Core.DsShapes;
using Ssz.Utils.MonitoredUndo;

namespace Ssz.Operator.Core.ControlsDesign
{
    public partial class DesignDrawingBorder : Border
    {
        #region construction and destruction

        public DesignDrawingBorder()
        {
            InitializeComponent();
        }

        #endregion

        #region protected functions

        protected DesignDrawingViewModel DrawingViewModel => (DesignDrawingViewModel) DataContext;

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            base.OnContextMenuOpening(e);

            DrawingViewModel.CurrentCursorPoint = Mouse.GetPosition(DesignDrawingCanvas);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            var point = e.GetPosition(DesignDrawingCanvas);
            var dsShapeView = DesignDrawingCanvas.GetRootDsShapeViewAt(point);

            _rubberbandSelectionStartPoint = null;
            var noRubberband = dsShapeView is not null && dsShapeView.DsShapeViewModel.IsSelected;

            if (dsShapeView is null)
                DrawingViewModel.SelectionService.UpdateSelection(null);
            else
                DrawingViewModel.SelectionService.UpdateSelection(dsShapeView.DsShapeViewModel);

            if (e.ClickCount > 1 && e.ChangedButton == MouseButton.Left)
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
                    var dsShapePoint =
                        designerGeometryDsShapeView.DsShapeViewModel.DsShape.GetDsShapePoint(point);
                    if (designerGeometryDsShapeView.DsShapeViewModel.IsSelected)
                        if (designerGeometryDsShapeView.DsShapeViewModel.DsShape.ResizeThumbContains(
                            dsShapePoint))
                            inResizeThumb = true;
                    if (inResizeThumb) break;

                    foreach (ControlPoint cp in designerGeometryDsShapeView.ControlPointsOrdered)
                    {
                        var di = cp.HitTest(dsShapePoint);
                        if (di.HasValue)
                        {
                            designerGeometryDsShapeView.SelectedControlPoint = cp;

                            switch (e.ChangedButton)
                            {
                                case MouseButton.Left:
                                    _dragInfo = di;
                                    _dragInfo.Value.DragObject.StartDrag();
                                    e.Handled = true;
                                    return;
                            }

                            break;
                        }
                    }
                }

            if (e.ChangedButton == MouseButton.Left)
                if (!noRubberband)
                    _rubberbandSelectionStartPoint = e.GetPosition(this);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!_inChangeSetBatch)
                {
                    UndoService.Current[DrawingViewModel.Drawing.GetUndoRoot()].BeginChangeSetBatch("Move", true);
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
                    var point = e.GetPosition(_dragInfo.Value.RelativeTo);
                    _dragInfo.Value.DragObject.DragObject(point - _dragInfo.Value.Offset);
                    e.Handled = true;
                }

                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(this);

                if (_rubberbandAdorner is null)
                    if (_rubberbandSelectionStartPoint.HasValue)
                    {
                        var rubberbandSelectionStartPoint = _rubberbandSelectionStartPoint.Value;
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

                    var designerDrawingCanvas = DesignDrawingCanvas;
                    var relativePoint =
                        designerDrawingCanvas.TransformToAncestor(this).Transform(new Point(0, 0));
                    var rubberBand = new Rect(_rubberbandAdorner.StartPoint, _rubberbandAdorner.EndPoint);
                    rubberBand.Offset(-relativePoint.X, -relativePoint.Y);

                    DesignDsProjectViewModel.Instance.UpdateSelection(rubberBand);

                    _rubberbandAdorner.InvalidateVisual();
                }

                return;
            }

            _rubberbandSelectionStartPoint = null;

            if (_rubberbandAdorner is not null) RemoveAdorner(_rubberbandAdorner);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (e.ChangedButton == MouseButton.Left)
            {
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
            }
        }

        #endregion

        #region private functions

        private void AddAdorner(RubberbandAdorner rubberbandAdorner)
        {
            if (_rubberbandAdorner is not null) return;

            _rubberbandAdorner = rubberbandAdorner;
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer is not null) adornerLayer.Add(rubberbandAdorner);
        }


        private void RemoveAdorner(RubberbandAdorner rubberbandAdorner)
        {
            _rubberbandAdorner = null;
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer is not null) adornerLayer.Remove(rubberbandAdorner);
        }

        #endregion

        #region private fields

        private RubberbandAdorner? _rubberbandAdorner;
        private Point? _rubberbandSelectionStartPoint;
        private bool _inChangeSetBatch;
        private DragInfo? _dragInfo;

        #endregion
    }
}