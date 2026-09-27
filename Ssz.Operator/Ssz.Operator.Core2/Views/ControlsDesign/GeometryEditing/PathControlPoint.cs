using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.ControlsDesign.GeometryEditing
{
    /// <summary>
    ///     A handle over one point of a path: the start point of a figure, or one of the points a
    ///     segment is made of.
    ///     <para>
    ///         Ported from the WPF editor, where the point was watched through a
    ///         DependencyPropertyDescriptor. Avalonia reports a property change on the object itself, so
    ///         the handle listens there and stops listening when it goes.
    ///     </para>
    /// </summary>
    public class PathControlPoint : ControlPoint
    {
        #region construction and destruction

        public PathControlPoint(DesignGeometryDsShapeView designerGeometryDsShapeView,
            AvaloniaObject obj, AvaloniaProperty avaloniaProperty,
            int index) :
            base(designerGeometryDsShapeView)
        {
            Obj = obj;
            AvaloniaProperty = avaloniaProperty;
            Index = index;

            if (Obj is Avalonia.Media.PathSegment)
            {
                if (typeof(IList<Point>).IsAssignableFrom(avaloniaProperty.PropertyType))
                    Type = PathControlPointType.SegmentInPointsCollection;
                else if (avaloniaProperty.PropertyType == typeof(Point))
                    Type = PathControlPointType.SegmentOtherPoint;
            }
            else if (Obj is Avalonia.Media.PathFigure && avaloniaProperty.Name == @"StartPoint")
            {
                Type = PathControlPointType.FigureStartPoint;
            }

            Obj.PropertyChanged += ObjOnPropertyChanged;

            Center = GetUnderlyingCenter();

            AddCommand = new RelayCommand(parameter => DesignGeometryDsShapeView.AddPoint(this));
            DeleteCommand = new RelayCommand(parameter => DesignGeometryDsShapeView.DeletePoint(this));
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
                Obj.PropertyChanged -= ObjOnPropertyChanged;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public AvaloniaObject Obj { get; }

        public AvaloniaProperty AvaloniaProperty { get; }

        public int Index { get; }

        public PathControlPointType Type { get; }

        public ICommand AddCommand { get; }

        public ICommand DeleteCommand { get; }

        public override void DragObject(Point point)
        {
            SetUnderlyingCenter(point);
        }

        public override ContextMenu? GetContextMenu()
        {
            object? resource = null;
            Application.Current?.TryFindResource(@"PathControlPointContextMenu", out resource);
            var contextMenu = resource as ContextMenu;
            if (contextMenu is not null) contextMenu.DataContext = this;
            return contextMenu;
        }

        public override void SetUnderlyingCenter(Point point)
        {
            if (AvaloniaProperty.PropertyType == typeof(Point))
            {
                Obj.SetValue(AvaloniaProperty, point);
            }
            else
            {
                var points = Obj.GetValue(AvaloniaProperty) as IList<Point>;
                if (points is null || Index < 0 || Index >= points.Count) return;
                points[Index] = point;
            }

            // Replacing an item of a list is not a change of the property that holds it, so unlike the
            // Point case it never reaches ObjOnPropertyChanged. WPF's Freezable collections did report
            // it, and the middle handles rely on hearing about it.
            Center = GetUnderlyingCenter();
        }

        public Point GetUnderlyingCenter()
        {
            if (AvaloniaProperty.PropertyType == typeof(Point))
                return (Point) Obj.GetValue(AvaloniaProperty)!;

            var points = Obj.GetValue(AvaloniaProperty) as IList<Point>;
            if (points is not null && Index >= 0 && Index < points.Count)
                return points[Index];

            return new Point(0, 0);
        }

        public override double Radius => 5.0;

        #endregion

        #region private functions

        private void ObjOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (Disposed || e.Property != AvaloniaProperty) return;

            Center = GetUnderlyingCenter();
        }

        #endregion
    }

    public enum PathControlPointType
    {
        Uncknown,
        FigureStartPoint,
        SegmentInPointsCollection,
        SegmentOtherPoint
    }
}
