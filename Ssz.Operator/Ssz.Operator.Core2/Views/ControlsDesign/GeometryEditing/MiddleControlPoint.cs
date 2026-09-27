using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsDesign.GeometryEditing
{
    /// <summary>
    ///     The handle between two path handles. Dragging it moves both of them, which keeps the segment
    ///     between them straight, and its menu squares that segment up or sets its angle.
    /// </summary>
    public class MiddleControlPoint : ControlPoint
    {
        #region construction and destruction

        public MiddleControlPoint(DesignGeometryDsShapeView designerGeometryDsShapeView, PathControlPoint cp0,
            PathControlPoint cp1) :
            base(designerGeometryDsShapeView)
        {
            Cp0 = cp0;
            Cp1 = cp1;

            Cp0.CenterChanged += () =>
            {
                if (!_disableUpdate) Center = GetUnderlyingCenter();
            };
            Cp1.CenterChanged += () =>
            {
                if (!_disableUpdate) Center = GetUnderlyingCenter();
            };

            Center = GetUnderlyingCenter();

            ZIndex = -1;

            SetHorizontalCommand = new RelayCommand(SetHorizontal);
            SetVerticalCommand = new RelayCommand(SetVertical);
            SetAngleCommand = new RelayCommand(parameter => _ = SetAngleAsync());
        }

        #endregion

        #region public functions

        public PathControlPoint Cp0 { get; }

        public PathControlPoint Cp1 { get; }

        public override double Radius => 4.0;

        public ICommand SetHorizontalCommand { get; }

        public ICommand SetVerticalCommand { get; }

        public ICommand SetAngleCommand { get; }

        public override void DragObject(Point point)
        {
            SetUnderlyingCenter(point);
        }

        public override ContextMenu? GetContextMenu()
        {
            object? resource = null;
            Application.Current?.TryFindResource(@"MiddleControlPointContextMenu", out resource);
            var contextMenu = resource as ContextMenu;
            if (contextMenu is not null) contextMenu.DataContext = this;
            return contextMenu;
        }

        public override void SetUnderlyingCenter(Point point)
        {
            Point p0 = Cp0.Center;
            Point p1 = Cp1.Center;
            var dpX = p1.X - p0.X;
            var dpY = p1.Y - p0.Y;
            var deltaX = point.X - (p1.X + p0.X) / 2;
            var deltaY = point.Y - (p1.Y + p0.Y) / 2;
            if (dpY > -0.001 && dpY < 0.001)
                deltaX = 0.0;
            else if (dpX > -0.001 && dpX < 0.001) deltaY = 0.0;

            _disableUpdate = true;
            Cp0.SetUnderlyingCenter(new Point(p0.X + deltaX, p0.Y + deltaY));
            Cp1.SetUnderlyingCenter(new Point(p1.X + deltaX, p1.Y + deltaY));
            _disableUpdate = false;
            Center = GetUnderlyingCenter();
        }

        public Point GetUnderlyingCenter()
        {
            Point p0 = Cp0.Center;
            Point p1 = Cp1.Center;
            return new Point((p1.X + p0.X) / 2, (p1.Y + p0.Y) / 2);
        }

        #endregion

        #region private functions

        private void SetHorizontal(object? parameter)
        {
            var angle = (int) DesignGeometryDsShapeView.GeometryDsShapeView.DsShapeViewModel.DsShape
                .AngleInitial;
            if (angle % 180 == 90)
                SetVerticalUnconditionally();
            else
                SetHorizontalUnconditionally();
        }

        private void SetVertical(object? parameter)
        {
            var angle = (int) DesignGeometryDsShapeView.GeometryDsShapeView.DsShapeViewModel.DsShape
                .AngleInitial;
            if (angle % 180 == 90)
                SetHorizontalUnconditionally();
            else
                SetVerticalUnconditionally();
        }

        private async System.Threading.Tasks.Task SetAngleAsync()
        {
            string? angleString = await InputBoxHelper.ShowAsync(Resources.SetAnglePrompt, Resources.SetAngleTitle);
            if (angleString is null) return;

            var angle = Math.PI * ObsoleteAnyHelper.ConvertTo<double>(angleString, false) / 180;

            Point p0 = Cp0.Center;
            Point p1 = Cp1.Center;
            var l = Math.Sqrt(Math.Pow(p1.X - p0.X, 2) + Math.Pow(p1.Y - p0.Y, 2)) / 2;
            var lX = l * Math.Cos(angle);
            var lY = l * Math.Sin(angle);
            _disableUpdate = true;
            Cp0.SetUnderlyingCenter(new Point(Center.X - lX, Center.Y - lY));
            Cp1.SetUnderlyingCenter(new Point(Center.X + lX, Center.Y + lY));
            _disableUpdate = false;

            DesignGeometryDsShapeView.GeometryDsShapeView.UpdateModelLayer();
        }

        private void SetHorizontalUnconditionally()
        {
            Point p0 = Cp0.Center;
            Point p1 = Cp1.Center;
            _disableUpdate = true;
            Cp0.SetUnderlyingCenter(new Point(p0.X, Center.Y));
            Cp1.SetUnderlyingCenter(new Point(p1.X, Center.Y));
            _disableUpdate = false;

            DesignGeometryDsShapeView.GeometryDsShapeView.UpdateModelLayer();
        }

        private void SetVerticalUnconditionally()
        {
            Point p0 = Cp0.Center;
            Point p1 = Cp1.Center;
            _disableUpdate = true;
            Cp0.SetUnderlyingCenter(new Point(Center.X, p0.Y));
            Cp1.SetUnderlyingCenter(new Point(Center.X, p1.Y));
            _disableUpdate = false;

            DesignGeometryDsShapeView.GeometryDsShapeView.UpdateModelLayer();
        }

        #endregion

        #region private fields

        private bool _disableUpdate;

        #endregion
    }
}
