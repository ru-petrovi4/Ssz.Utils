using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsDesign.GeometryEditing
{
    public class PathControlPoint : ControlPoint
    {
        #region construction and destruction

        public PathControlPoint(DesignGeometryDsShapeView designerGeometryDsShapeView,
            DependencyObject obj, DependencyProperty dp,
            int index) :
            base(designerGeometryDsShapeView)
        {
            Obj = obj;
            Dp = dp;
            Index = index;

            if (Obj is PathSegment)
            {
                if (dp.PropertyType == typeof(PointCollection)) Type = PathControlPointType.SegmentInPointsCollection;
                else if (dp.PropertyType == typeof(Point)) Type = PathControlPointType.SegmentOtherPoint;
            }
            else if (Obj is PathFigure && dp.Name == @"StartPoint")
            {
                Type = PathControlPointType.FigureStartPoint;
            }

            DependencyPropertyDescriptor.FromProperty(Dp, Obj.GetType())
                .AddValueChanged(Obj, OnDependencyPropertyChanged);

            Center = GetUnderlyingCenter();

            AddCommand = new RelayCommand(parameter => DesignGeometryDsShapeView.AddPoint(this));
            DeleteCommand = new RelayCommand(parameter => DesignGeometryDsShapeView.DeletePoint(this));
        }

        #endregion

        #region private functions

        private void OnDependencyPropertyChanged(object? sender, EventArgs e)
        {
            if (Disposed) return;

            Center = GetUnderlyingCenter();
        }

        #endregion

        #region public functions

        public DependencyObject Obj { get; }


        public DependencyProperty Dp { get; }

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
            /*
            ContextMenu? contextMenu = null;
            switch (Type)
            {
                case PathControlPointType.FigureStartPoint:
                    contextMenu = Application.Current.FindResource("FigureStartPointContextMenu") as ContextMenu;
                    break;
                case PathControlPointType.SegmentInPointsCollection:
                    contextMenu = Application.Current.FindResource("SegmentInPointsCollectionContextMenu") as ContextMenu;
                    break;
                case PathControlPointType.SegmentOtherPoint:
                    contextMenu = Application.Current.FindResource("SegmentOtherPointPointContextMenu") as ContextMenu;
                    break;
            }*/
            var contextMenu = Application.Current.FindResource("PathControlPointContextMenu") as ContextMenu;
            if (contextMenu is not null) contextMenu.DataContext = this;
            return contextMenu;
        }

        public override void SetUnderlyingCenter(Point point)
        {
            if (Dp is not null)
            {
                if (Dp.PropertyType == typeof(Point))
                {
                    Obj.SetValue(Dp, point);
                }
                else if (Dp.PropertyType == typeof(PointCollection))
                {
                    var pointCollection = (PointCollection) Obj.GetValue(Dp);
                    pointCollection[Index] = point;
                }
            }
        }

        public Point GetUnderlyingCenter()
        {
            if (Dp is not null)
            {
                if (Dp.PropertyType == typeof(Point)) return (Point) Obj.GetValue(Dp);
                if (Dp.PropertyType == typeof(PointCollection))
                {
                    var pointCollection = (PointCollection) Obj.GetValue(Dp);
                    return pointCollection[Index];
                }
            }

            return new Point(0, 0);
        }

        public override double Radius => 5.0;

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