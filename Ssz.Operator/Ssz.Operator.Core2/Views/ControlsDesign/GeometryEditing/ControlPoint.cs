using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsDesign.GeometryEditing
{
    /// <summary>
    ///     A handle the author drags to reshape a geometry. It draws itself as a circle in one of the
    ///     two geometry groups of the view - the plain one or the one for the selected handle.
    /// </summary>
    public abstract class ControlPoint : IDragableObject, IDisposable
    {
        #region construction and destruction

        public ControlPoint(DesignGeometryDsShapeView designerGeometryDsShapeView)
        {
            DesignGeometryDsShapeView = designerGeometryDsShapeView;
            _controlPointGeometry = new EllipseGeometry
            {
                Center = new Point(0, 0),
                RadiusX = Radius,
                RadiusY = Radius
            };
            DesignGeometryDsShapeView.ControlPointsGeometryGroup.Children.Add(_controlPointGeometry);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isSelected)
                    DesignGeometryDsShapeView.SelectedControlPointGeometryGroup.Children
                        .Remove(_controlPointGeometry);
                else
                    DesignGeometryDsShapeView.ControlPointsGeometryGroup.Children.Remove(_controlPointGeometry);
            }

            Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~ControlPoint()
        {
            Dispose(false);
        }

        public bool Disposed { get; private set; }

        #endregion

        #region public functions

        public DesignGeometryDsShapeView DesignGeometryDsShapeView { get; }

        public int ZIndex { get; protected set; }

        public int Num { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    if (_isSelected)
                    {
                        DesignGeometryDsShapeView.ControlPointsGeometryGroup.Children.Remove(_controlPointGeometry);
                        DesignGeometryDsShapeView.SelectedControlPointGeometryGroup.Children.Add(
                            _controlPointGeometry);
                    }
                    else
                    {
                        DesignGeometryDsShapeView.SelectedControlPointGeometryGroup.Children.Remove(
                            _controlPointGeometry);
                        DesignGeometryDsShapeView.ControlPointsGeometryGroup.Children.Add(_controlPointGeometry);
                    }
                }
            }
        }

        public abstract double Radius { get; }

        public event Action? CenterChanged;

        public Point Center
        {
            get => _center;
            set
            {
                _center = value;
                _controlPointGeometry.Center = Center;
                if (CenterChanged is not null) CenterChanged();
            }
        }

        public bool IsDraged { get; internal set; }

        public abstract ContextMenu? GetContextMenu();

        public DragInfo? HitTest(Point point)
        {
            Vector d = point - _center;
            if (d.X * d.X + d.Y * d.Y <= Radius * Radius)
            {
                var hti = new DragInfo();
                hti.DragObject = this;
                hti.RelativeTo = DesignGeometryDsShapeView;
                hti.Offset = d;
                return hti;
            }

            return null;
        }

        public abstract void SetUnderlyingCenter(Point point);

        public void StartDrag()
        {
            IsDraged = true;
        }

        public abstract void DragObject(Point point);

        public void EndDrag()
        {
            IsDraged = false;

            DesignGeometryDsShapeView.GeometryDsShapeView.UpdateModelLayer();
        }

        #endregion

        #region private fields

        private Point _center;
        private bool _isSelected;

        protected EllipseGeometry _controlPointGeometry;

        #endregion
    }
}
