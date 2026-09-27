using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.ControlsCommon.Converters;
using Ssz.Operator.Core.ControlsDesign.GeometryEditing;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.VisualEditors;
using Ssz.Utils.MonitoredUndo;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     The drawing surface of the editor. It holds two controls per shape: the shape view itself,
    ///     which looks exactly as it will at run time, and on top of it the design view that carries the
    ///     drag and resize handles.
    /// </summary>
    public partial class DesignDrawingCanvas : Canvas
    {
        #region construction and destruction

        public DesignDrawingCanvas()
        {
            DragDrop.SetAllowDrop(this, true);
            Focusable = true;

            #region Lines Grid

            _overlyingLinesGrid = new LinesGrid();
            _overlyingLinesGrid.IsHitTestVisible = false;
            _overlyingLinesGrid.LineThickness = 1;
            _overlyingLinesGrid.LineBrush = new SolidColorBrush(Color.FromArgb(0x11, 0x00, 0x80, 0x00));
            _overlyingLinesGrid.Bind(WidthProperty, new Binding
            {
                Path = nameof(Bounds) + @".Width",
                Source = this,
                Mode = BindingMode.OneWay
            });
            _overlyingLinesGrid.Bind(HeightProperty, new Binding
            {
                Path = nameof(Bounds) + @".Height",
                Source = this,
                Mode = BindingMode.OneWay
            });
            _overlyingLinesGrid.Bind(IsVisibleProperty, new Binding
            {
                Source = DesignDsProjectViewModel.Instance,
                Path = nameof(DesignDsProjectViewModel.DiscreteMode),
                Mode = BindingMode.OneWay
            });
            _overlyingLinesGrid.Bind(LinesGrid.StepProperty, new Binding
            {
                Source = DesignDsProjectViewModel.Instance,
                Path = nameof(DesignDsProjectViewModel.DiscreteModeStep),
                Mode = BindingMode.OneWay
            });
            _overlyingLinesGrid.ZIndex = Int32.MaxValue - 1;

            #endregion

            _dsShapesInfoTooltipsCanvas = new DsShapesInfoTooltipsCanvas(this);
            _dsShapesInfoTooltipsCanvas.IsHitTestVisible = false;
            _dsShapesInfoTooltipsCanvas.ZIndex = Int32.MaxValue;

            AddHandler(DragDrop.DropEvent, OnDrop);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The format a drawing list uses to hand shapes over in a drag. It never leaves the
        ///     application, so the object itself travels rather than a serialized copy.
        /// </summary>
        public static readonly DataFormat<EntityInfo> EntityInfoDataFormat =
            DataFormat.CreateInProcessFormat<EntityInfo>(@"Ssz.Operator.EntityInfo");

        public DesignDrawingViewModel DesignDrawingViewModel => (DesignDrawingViewModel) DataContext!;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            DesignDrawingViewModel designDrawingViewModel = DesignDrawingViewModel;

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

            ComplexDsShape[] complexDsShapes =
                designDrawingViewModel.Drawing.DsShapes.OfType<ComplexDsShape>().ToArray();
            foreach (ComplexDsShape complexDsShape in complexDsShapes)
                ComplexDsShapeOnCenterInitialPositionChanged(complexDsShape);
            foreach (ComplexDsShape complexDsShape in complexDsShapes)
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

            DesignDrawingViewModel designDrawingViewModel = DesignDrawingViewModel;

            UndoService.Current.Clear(designDrawingViewModel.Drawing.GetUndoRoot());

            designDrawingViewModel.Drawing.SetUndoRoot(null);

            var dsPageDrawing = designDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is not null) dsPageDrawing.PropertyChanged -= DsPageDrawingOnPropertyChanged;
            designDrawingViewModel.Drawing.DsShapesAdded -= DrawingDsShapesAdded;
            designDrawingViewModel.Drawing.DsShapesRemoved -= DrawingDsShapesRemoved;
            designDrawingViewModel.Drawing.DsShapesReodered -= UpdateDrawingDsShapesTreeView;

            foreach (Control child in Children)
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
            // Force shapes tree refresh
            DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel =
                DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
        }

        public DsShapeViewBase[] GetSelectedRootDsShapeViews()
        {
            return DesignDrawingViewModel.SelectionService.SelectedItems
                .Select(svm =>
                    ((DesignDsShapeView) (svm.DsShape.TagObject ?? throw new InvalidOperationException()))
                    .DsShapeView).ToArray();
        }

        /// <summary>
        ///     The topmost shape under the point. A selected shape wins over an unselected one, so that a
        ///     shape stays grabbable once it is picked.
        /// </summary>
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
                        Point dsShapePoint = dsShapeViewModel.DsShape.GetDsShapePoint(point);

                        if (designerConnectorDsShapeView.HitTestPath(dsShapePoint))
                            return designerDsShapeView.DsShapeView;

                        foreach (ControlPoint cp in designerConnectorDsShapeView.ControlPointsOrdered)
                        {
                            DragInfo? di = cp.HitTest(dsShapePoint);
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
                        Point dsShapePoint = dsShapeViewModel.DsShape.GetDsShapePoint(point);

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

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Close();

            base.OnDetachedFromVisualTree(e);
        }

        #endregion

        #region private functions

        private void OnDrop(object? sender, DragEventArgs e)
        {
            Point position = e.GetPosition(this);

            EntityInfo? entityInfo = e.DataTransfer.TryGetValue(EntityInfoDataFormat);
            DesignDrawingViewModel.AddDsShape(entityInfo, position);

            e.Handled = true;
        }

        private void DsPageDrawingOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case @"UnderlyingXaml":
                    SetUnderlyingContentControl();
                    DesignDrawingViewModel.TryConvertUnderlyingContentXamlToDsShapes();
                    break;
                case @"Background":
                    BackgroundChanged();
                    break;
            }
        }

        private void BackgroundChanged()
        {
            var dsPageDrawing = DesignDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is null) return;

            IBrush? background = dsPageDrawing.ComputeDsPageBackgroundBrush();
            if (background is not null)
                Background = background;
            else
                Background = new SolidColorBrush(Color.FromRgb(0xD3, 0xD3, 0xD3));
        }

        private async void SetUnderlyingContentControl()
        {
            var dsPageDrawing = DesignDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is null) return;

            try
            {
                if (_underlyingContentControl is null)
                {
                    _underlyingContentControl = new ContentControl();
                    _underlyingContentControl.Bind(WidthProperty, new Binding
                    {
                        Path = nameof(Bounds) + @".Width",
                        Source = this,
                        Mode = BindingMode.OneWay
                    });
                    _underlyingContentControl.Bind(HeightProperty, new Binding
                    {
                        Path = nameof(Bounds) + @".Height",
                        Source = this,
                        Mode = BindingMode.OneWay
                    });
                    Children.Add(_underlyingContentControl);
                }

                _underlyingContentControl.Content = await dsPageDrawing.GetUnderlyingContentAsync();
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"DrawingUnderlyingXaml error.");
            }
        }

        private void DrawingDsShapesAdded(IEnumerable<DsShapeBase> dsShapes)
        {
            using (DesignDsProjectViewModel.BusyCloser busyCloser =
                   DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                busyCloser.SetHeader(Properties.Resources.ProgressInfo_DescriptionLine1_AddingDsShapes);
                var i = 0;
                foreach (DsShapeBase dsShape in dsShapes)
                {
                    DsShapeViewBase? newDsShapeView = DsShapeViewFactory.New(dsShape, null);
                    if (newDsShapeView is null)
                        continue;
                    newDsShapeView.Initialize(null);

                    DesignDsShapeView newDesignDsShapeView = DesignDsShapeViewFactory.New(newDsShapeView, this);

                    // The design view only shows while its shape is selected: it is the handles, not the
                    // shape.
                    newDesignDsShapeView.Bind(IsVisibleProperty, new Binding
                    {
                        Path = nameof(DsShapeViewModel.IsSelected),
                        Mode = BindingMode.OneWay
                    });

                    newDesignDsShapeView.Bind(WidthProperty, new Binding
                    {
                        Path = nameof(DsShapeViewModel.WidthInitial),
                        Mode = BindingMode.OneWay
                    });

                    newDesignDsShapeView.Bind(HeightProperty, new Binding
                    {
                        Path = nameof(DsShapeViewModel.HeightInitial),
                        Mode = BindingMode.OneWay
                    });

                    newDesignDsShapeView.Bind(LeftProperty, new Binding
                    {
                        Path = nameof(DsShapeViewModel.LeftNotTransformed),
                        Mode = BindingMode.OneWay
                    });

                    newDesignDsShapeView.Bind(TopProperty, new Binding
                    {
                        Path = nameof(DsShapeViewModel.TopNotTransformed),
                        Mode = BindingMode.OneWay
                    });

                    newDesignDsShapeView.Bind(RenderTransformOriginProperty, new Binding
                    {
                        Path = nameof(DsShapeViewModel.CenterRelativePosition),
                        Mode = BindingMode.OneWay
                    });

                    newDesignDsShapeView.RotateTransform.Bind(RotateTransform.AngleProperty, new Binding
                    {
                        Source = newDsShapeView.DsShapeViewModel,
                        Path = nameof(DsShapeViewModel.AngleInitial),
                        Mode = BindingMode.OneWay
                    });

                    newDesignDsShapeView.ScaleTranform.Bind(ScaleTransform.ScaleXProperty, new Binding
                    {
                        Source = newDsShapeView.DsShapeViewModel,
                        Path = nameof(DsShapeViewModel.IsFlipped),
                        Converter = FlipBoolConverter.Instance,
                        Mode = BindingMode.OneWay
                    });

                    newDesignDsShapeView.Bind(ZIndexProperty, new Binding
                    {
                        Path = nameof(DsShapeViewModel.DesignZIndex),
                        Mode = BindingMode.OneWay
                    });

                    Children.Add(newDsShapeView);
                    Children.Add(newDesignDsShapeView);
                    DrawingDesignDsShapeViewAdded(newDesignDsShapeView);

                    DsShapeViewModel dsShapeViewModel = newDsShapeView.DsShapeViewModel;

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
                    // Let the busy indicator draw itself on a drawing with thousands of shapes.
                    if (i % 1000 == 0) Dispatcher.UIThread.RunJobs();
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
                DsShapeViewBase dsShapeView = designerDsShapeView.DsShapeView;
                Children.Remove(designerDsShapeView);
                Children.Remove(dsShapeView);
                DrawingDesignDsShapeViewRemoved(designerDsShapeView);

                DesignDrawingViewModel.SelectionService.Detach(designerDsShapeView.DsShapeViewModel);

                designerDsShapeView.Dispose();
                dsShapeView.Dispose();
            }

            UpdateDrawingDsShapesTreeView();
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
