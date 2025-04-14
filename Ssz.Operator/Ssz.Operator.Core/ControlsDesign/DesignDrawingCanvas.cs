using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.ControlsCommon.Converters;
using Ssz.Operator.Core.ControlsDesign.GeometryEditing;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Utils;
using Ssz.Utils.MonitoredUndo;

namespace Ssz.Operator.Core.ControlsDesign
{
    public partial class DesignDrawingCanvas : Canvas
    {
        #region construction and destruction

        public DesignDrawingCanvas()
        {
            AllowDrop = true;
            Focusable = true;

            #region Lines Grid

            _overlyingLinesGrid = new LinesGrid();
            _overlyingLinesGrid.IsHitTestVisible = false;
            _overlyingLinesGrid.LineThickness = 1;
            _overlyingLinesGrid.LineBrush = new SolidColorBrush(Color.FromArgb(0x11, 0x00, 0x80, 0x00));
            _overlyingLinesGrid.SetBinding(WidthProperty, new Binding
            {
                Path = new PropertyPath(nameof(ActualWidth)),
                Source = this,
                Mode = BindingMode.OneWay
            });
            _overlyingLinesGrid.SetBinding(HeightProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(ActualHeight)),
                Mode = BindingMode.OneWay
            });
            _overlyingLinesGrid.SetBinding(VisibilityProperty, new Binding
            {
                Source = DesignDsProjectViewModel.Instance,
                Path = new PropertyPath(nameof(DesignDsProjectViewModel.DiscreteMode)),
                Converter = new BooleanToVisibilityConverter(),
                Mode = BindingMode.OneWay
            });
            _overlyingLinesGrid.SetBinding(LinesGrid.StepProperty, new Binding
            {
                Source = DesignDsProjectViewModel.Instance,
                Path = new PropertyPath(nameof(DesignDsProjectViewModel.DiscreteModeStep)),
                Mode = BindingMode.OneWay
            });
            SetZIndex(_overlyingLinesGrid, int.MaxValue - 1);

            #endregion

            _dsShapesInfoTooltipsCanvas = new DsShapesInfoTooltipsCanvas(this);
            _dsShapesInfoTooltipsCanvas.IsHitTestVisible = false;
            SetZIndex(_dsShapesInfoTooltipsCanvas, int.MaxValue);

            Unloaded += OnUnloaded;
        }

        #endregion

        #region public functions

        public DesignDrawingViewModel DesignDrawingViewModel => (DesignDrawingViewModel) DataContext;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var designDrawingViewModel = DesignDrawingViewModel;

            designDrawingViewModel.SelectionService.Clear();

            if (designDrawingViewModel.Drawing is DsShapeDrawing)
                Background = new SolidColorBrush(Colors.Transparent);

            var dsPageDrawing = designDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is not null)
            {
                dsPageDrawing.PropertyChanged += DsPageDrawingOnPropertyChanged;
                SetUnderlyingContentControl();
                BackgroundChanged();
            }

            designDrawingViewModel.Drawing.DsShapesAdded += DrawingDsShapesAdded;
            designDrawingViewModel.Drawing.DsShapesRemoved += DrawingDsShapesRemoved;
            designDrawingViewModel.Drawing.DsShapesReodered += UpdateDrawingDsShapesTreeView;

            Children.Add(_overlyingLinesGrid);
            Children.Add(_dsShapesInfoTooltipsCanvas);

            DrawingDsShapesAdded(
                designDrawingViewModel.Drawing.DsShapes.Concat(designDrawingViewModel.Drawing.SystemDsShapes));

            designDrawingViewModel.Drawing.SetUndoRoot(designDrawingViewModel.Drawing);

            var complexDsShapes = designDrawingViewModel.Drawing.DsShapes.OfType<ComplexDsShape>().ToArray();
            foreach (var complexDsShape in complexDsShapes)
                ComplexDsShapeOnCenterInitialPositionChanged(complexDsShape);
            foreach (var complexDsShape in complexDsShapes)
            {
                if (complexDsShape.TagObject is null) continue;
                ((ComplexDsShapeView) ((DesignDsShapeView) complexDsShape.TagObject).DsShapeView)
                    .UpdateModelLayer();
            }

            DesignDrawingViewModel.TryConvertUnderlyingContentXamlToDsShapes();
        }

        public void Close()
        {
            if (!_initialized) return;
            _initialized = false;

            var designDrawingViewModel = DesignDrawingViewModel;

            UndoService.Current.Clear(designDrawingViewModel.Drawing.GetUndoRoot());

            designDrawingViewModel.Drawing.SetUndoRoot(null);

            var dsPageDrawing = designDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is not null) dsPageDrawing.PropertyChanged -= DsPageDrawingOnPropertyChanged;
            designDrawingViewModel.Drawing.DsShapesAdded -= DrawingDsShapesAdded;
            designDrawingViewModel.Drawing.DsShapesRemoved -= DrawingDsShapesRemoved;
            designDrawingViewModel.Drawing.DsShapesReodered -= UpdateDrawingDsShapesTreeView;

            foreach (object child in Children)
            {
                var disposable = child as IDisposable;
                if (disposable is not null) disposable.Dispose();
            }

            Children.Clear();

            _underlyingContentControl = null;

            UpdateDrawingDsShapesTreeView();
        }

        public void UpdateDrawingDsShapesTreeView()
        {
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel; // Force shapes tree refresh
        }

        public DsShapeViewBase[] GetSelectedRootDsShapeViews()
        {
            return DesignDrawingViewModel.SelectionService.SelectedItems
                .Select(svm =>
                    ((DesignDsShapeView) (svm.DsShape.TagObject ?? throw new InvalidOperationException()))
                    .DsShapeView).ToArray();
        }

        public DsShapeViewBase? GetRootDsShapeViewAt(Point point)
        {
            foreach (DsShapeViewModel dsShapeViewModel in DesignDrawingViewModel.GetRootDsShapeViewModels()
                .Where(svm => !svm.DsShape.IsLocked)
                .OrderByDescending(svm => svm.IsSelected)
                .ThenByDescending(svm => svm.DsShape.Index))
            {
                var designerDsShapeView = dsShapeViewModel.DsShape.TagObject as DesignDsShapeView;
                if (designerDsShapeView is null) continue;

                if (dsShapeViewModel.IsSelected)
                {
                    var designerConnectorDsShapeView = designerDsShapeView as DesignConnectorDsShapeView;
                    if (designerConnectorDsShapeView is not null)
                    {
                        var dsShapePoint = dsShapeViewModel.DsShape.GetDsShapePoint(point);

                        if (designerConnectorDsShapeView.HitTestPath(dsShapePoint))
                            return designerDsShapeView.DsShapeView;

                        DragInfo? di;
                        foreach (ControlPoint cp in designerConnectorDsShapeView.ControlPointsOrdered)
                        {
                            di = cp.HitTest(dsShapePoint);
                            if (di.HasValue) return designerDsShapeView.DsShapeView;
                        }
                    }
                    else
                    {
                        if (dsShapeViewModel.DsShape.Contains(point, true))
                            return designerDsShapeView.DsShapeView;
                    }
                }
                else
                {
                    var designerGeometryDsShapeView = designerDsShapeView as DesignGeometryDsShapeView;
                    if (designerGeometryDsShapeView is not null)
                    {
                        var dsShapePoint = dsShapeViewModel.DsShape.GetDsShapePoint(point);

                        if (designerGeometryDsShapeView.HitTestPath(dsShapePoint))
                            return designerDsShapeView.DsShapeView;
                    }
                    else
                    {
                        if (dsShapeViewModel.DsShape.Contains(point, false))
                            return designerDsShapeView.DsShapeView;
                    }
                }
            }

            return null;
        }

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            var position = e.GetPosition(this);

            var entityInfo = e.Data.GetData(typeof(EntityInfo)) as EntityInfo;
            if (entityInfo is null)
                entityInfo = e.Data.GetData(typeof(DsShapeDrawingInfo)) as DsShapeDrawingInfo;
            DesignDrawingViewModel.AddDsShape(entityInfo, position);

            e.Handled = true;
        }

        #endregion

        #region private functions

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DsPageDrawingOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case @"UnderlyingXaml":
                    SetUnderlyingContentControl();
                    DesignDrawingViewModel.TryConvertUnderlyingContentXamlToDsShapes();
                    break;
                case "Background":
                    BackgroundChanged();
                    break;
            }
        }

        private void BackgroundChanged()
        {
            var dsPageDrawing = DesignDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is null) return;

            var background = dsPageDrawing.ComputeDsPageBackgroundBrush();
            if (background is not null)
                Background = background;
            else
                Background = new SolidColorBrush(Color.FromRgb(0xD3, 0xD3, 0xD3));
        }

        private void SetUnderlyingContentControl()
        {
            var dsPageDrawing = DesignDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is null) return;

            try
            {
                if (_underlyingContentControl is null)
                {
                    _underlyingContentControl = new ContentControl();
                    _underlyingContentControl.SetBinding(WidthProperty, new Binding
                    {
                        Path = new PropertyPath(nameof(ActualWidth)),
                        Source = this,
                        Mode = BindingMode.OneWay
                    });
                    _underlyingContentControl.SetBinding(HeightProperty, new Binding
                    {
                        Path = new PropertyPath(nameof(ActualHeight)),
                        Source = this,
                        Mode = BindingMode.OneWay
                    });
                    Children.Add(_underlyingContentControl);
                }

                _underlyingContentControl.Content = dsPageDrawing.GetUnderlyingContent();
                _underlyingContentControl.SnapsToDevicePixels = false;
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, "DrawingUnderlyingXaml error.");
            }
        }

        private void DrawingDsShapesAdded(IEnumerable<DsShapeBase> dsShapes)
        {
            using (var busyCloser = DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                busyCloser.SetHeader(Properties.Resources.ProgressInfo_DescriptionLine1_AddingDsShapes);
                var i = 0;
                foreach (DsShapeBase dsShape in dsShapes)
                {
                    var newDsShapeView = DsShapeViewFactory.New(dsShape, null);
                    if (newDsShapeView is null) 
                        continue;
                    newDsShapeView.Initialize(null);

                    DesignDsShapeView newDesignDsShapeView = DesignDsShapeViewFactory.New(newDsShapeView, this);

                    DsShapeViewModel dsShapeViewModel = newDsShapeView.DsShapeViewModel;

                    BindingOperations.SetBinding(newDesignDsShapeView, VisibilityProperty, new Binding
                    {
                        Path =
                            DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.IsSelected),
                        Converter = new BooleanToVisibilityConverter(),
                        Mode = BindingMode.OneWay
                    });

                    BindingOperations.SetBinding(newDesignDsShapeView, WidthProperty, new Binding
                    {
                        Path =
                            DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.WidthInitial),
                        Mode = BindingMode.OneWay
                    });

                    BindingOperations.SetBinding(newDesignDsShapeView, HeightProperty, new Binding
                    {
                        Path =
                            DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.HeightInitial),
                        Mode = BindingMode.OneWay
                    });

                    BindingOperations.SetBinding(newDesignDsShapeView, LeftProperty, new Binding
                    {
                        Path =
                            DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.LeftNotTransformed),
                        Mode = BindingMode.OneWay
                    });

                    BindingOperations.SetBinding(newDesignDsShapeView, TopProperty, new Binding
                    {
                        Path =
                            DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.TopNotTransformed),
                        Mode = BindingMode.OneWay
                    });

                    BindingOperations.SetBinding(newDesignDsShapeView, RenderTransformOriginProperty, new Binding
                    {
                        Path =
                            DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.CenterRelativePosition),
                        Mode =
                            BindingMode
                                .OneWay
                    });

                    BindingOperations.SetBinding(newDesignDsShapeView.RotateTransform, RotateTransform.AngleProperty,
                        new Binding
                        {
                            Path = DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.AngleInitial),
                            Mode = BindingMode.OneWay
                        });

                    BindingOperations.SetBinding(newDesignDsShapeView.ScaleTranform, ScaleTransform.ScaleXProperty,
                        new Binding
                        {
                            Path = DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.IsFlipped),
                            Converter = FlipBoolConverter.Instance,
                            Mode = BindingMode.OneWay
                        });

                    BindingOperations.SetBinding(newDesignDsShapeView, ZIndexProperty, new Binding
                    {
                        Path = DsShapeViewBase.GetPropertyPath(() => dsShapeViewModel.DesignZIndex),
                        Mode = BindingMode.OneWay
                    });

                    Children.Add(newDsShapeView);
                    Children.Add(newDesignDsShapeView);
                    DrawingDesignDsShapeViewAdded(newDesignDsShapeView);

                    if (dsShape.SelectWhenShow)
                    {
                        dsShape.SelectWhenShow = false;
                        DesignDrawingViewModel.SelectionService.AddToSelection(dsShapeViewModel);
                    }

                    if (dsShape.FirstSelectWhenShow)
                    {
                        dsShape.FirstSelectWhenShow = false;
                        DesignDrawingViewModel.SelectionService.MakeFirstSelected(dsShapeViewModel);
                    }

                    DesignDrawingViewModel.SelectionService.Attach(dsShapeViewModel);

                    dsShape.TagObject = newDesignDsShapeView;

                    i += 1;
                    if (i % 1000 == 0) DispatcherHelper.CurrentDispatcherDoEvents();
                }

                UpdateDrawingDsShapesTreeView();

                _dsShapesInfoTooltipsCanvas.Refresh(DesignDsProjectViewModel.Instance.ShowDsShapesInfoTooltips);
            }
        }

        private void DrawingDsShapesRemoved(IEnumerable<DsShapeBase> dsShapes)
        {
            foreach (DsShapeBase dsShape in dsShapes)
            {
                var designerDsShapeView = dsShape.TagObject as DesignDsShapeView;
                if (designerDsShapeView is null) continue;
                var dsShapeView = designerDsShapeView.DsShapeView;
                Children.Remove(designerDsShapeView);
                Children.Remove(dsShapeView);
                DrawingDesignDsShapeViewRemoved(designerDsShapeView);

                DesignDrawingViewModel.SelectionService.Detach(designerDsShapeView.DsShapeViewModel);

                designerDsShapeView.Dispose();
                dsShapeView.Dispose();
            }

            UpdateDrawingDsShapesTreeView();
        }

        private Point SnapToGrid(Point pt)
        {
            /*
            if (IsSnapToGridEnabled && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                pt.X = Math.Round(pt.X / SnapGridWidth) * SnapGridWidth;
                pt.Y = Math.Round(pt.Y / SnapGridWidth) * SnapGridWidth;
            }*/
            return pt;
        }

        #endregion

        #region private fields

        private bool _initialized;
        private ContentControl? _underlyingContentControl;
        private readonly LinesGrid _overlyingLinesGrid;
        private readonly DsShapesInfoTooltipsCanvas _dsShapesInfoTooltipsCanvas;

        #endregion
    }
}