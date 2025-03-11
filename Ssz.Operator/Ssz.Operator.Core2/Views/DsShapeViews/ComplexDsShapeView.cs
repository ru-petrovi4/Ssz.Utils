using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ComplexDsShapeView : DsShapeViewBase
    {
        #region construction and destruction

        public ComplexDsShapeView(ComplexDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            _complexDsShape = dsShape;
            _canvas = new Canvas();

            foreach (DsShapeBase sh in _complexDsShape.DsShapes)
            {
                var newDsShapeView = DsShapeViewFactory.New(sh, frame);
                if (newDsShapeView is null) continue;

                _dsShapeViewsList.Add(newDsShapeView);
                _canvas.Children.Add(newDsShapeView);
            }

            if (VisualDesignMode)
            {
                if (_complexDsShape.GetParentComplexDsShape() is null)
                    _canvas.SizeChanged += DesignCanvasOnSizeChanged;

                ConnectionPointDsShapeViews = GetConnectionPointDsShapeViews().ToArray();
            }
            else
            {
                _canvas.SizeChanged += PlayCanvasOnSizeChanged;
            }

            Content = _canvas;
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                try
                {
                    _canvas.Children.Clear();
                }
                catch
                {
                }

                if (VisualDesignMode)
                {
                    if (_complexDsShape.GetParentComplexDsShape() is null)
                        _canvas.SizeChanged -= DesignCanvasOnSizeChanged;
                }
                else
                {
                    _canvas.SizeChanged -= PlayCanvasOnSizeChanged;
                }

                foreach (DsShapeViewBase dsShapeView in _dsShapeViewsList) dsShapeView.Dispose();
                _dsShapeViewsList.Clear();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public DsShapeViewBase[] DsShapeViews => _canvas.Children.OfType<DsShapeViewBase>().ToArray();

        public ConnectionPointDsShapeView[]? ConnectionPointDsShapeViews { get; }

        public override void Initialize(PlayDrawingViewModel? playDrawingViewModel)
        {
            base.Initialize(playDrawingViewModel);

            foreach (var dsShapeView in _canvas.Children.OfType<DsShapeViewBase>())
            {
                dsShapeView.Initialize(playDrawingViewModel);
            }
        }

        public void UpdateModelLayer()
        {
            //if (ConnectionPointDsShapeViews is null) return;
            //foreach (var cpsv in ConnectionPointDsShapeViews)
            //{
            //    if (cpsv.ConnectionPointInfo is null) continue;
            //    ConnectionPointInfo connectionPointInfo = cpsv.ConnectionPointInfo;
            //    if (connectionPointInfo is null) continue;

            //    foreach (DesignConnectorDsShapeView dcsv in connectionPointInfo.BeginDesignConnectorDsShapeViews)
            //        dcsv.GeometryDsShapeView.UpdateModelLayer();

            //    foreach (DesignConnectorDsShapeView dcsv in connectionPointInfo.EndDesignConnectorDsShapeViews)
            //        dcsv.GeometryDsShapeView.UpdateModelLayer();
            //}
        }

        #endregion

        #region private functions

        private List<ConnectionPointDsShapeView> GetConnectionPointDsShapeViews()
        {
            var result = new List<ConnectionPointDsShapeView>();

            foreach (var sv in _canvas.Children)
            {
                var connectionPointDsShapeView = sv as ConnectionPointDsShapeView;
                if (connectionPointDsShapeView is not null)
                {
                    result.Add(connectionPointDsShapeView);
                    continue;
                }

                var csv = sv as ComplexDsShapeView;
                if (csv is not null) result.AddRange(csv.GetConnectionPointDsShapeViews());
            }

            return result;
        }

        private void DesignCanvasOnSizeChanged(object? sender, SizeChangedEventArgs args)
        {
            _complexDsShape.TransformDsShapes(args.NewSize.Width / args.PreviousSize.Width,
                args.NewSize.Height / args.PreviousSize.Height);
        }

        private void PlayCanvasOnSizeChanged(object? sender, SizeChangedEventArgs args)
        {
            var scaleX = args.NewSize.Width / args.PreviousSize.Width;
            var scaleY = args.NewSize.Height / args.PreviousSize.Height;
            if (!double.IsNaN(scaleX) && !double.IsInfinity(scaleX) &&
                !double.IsNaN(scaleY) && !double.IsInfinity(scaleY) &&
                (scaleX != 1.0 || scaleY != 1.0))
                foreach (DsShapeBase dsShape in _complexDsShape.DsShapes)
                    dsShape.Transform(scaleX, scaleY);
        }

        #endregion

        #region private fields

        private readonly ComplexDsShape _complexDsShape;

        private readonly Canvas _canvas;

        private readonly List<DsShapeViewBase> _dsShapeViewsList = new();

        #endregion
    }
}