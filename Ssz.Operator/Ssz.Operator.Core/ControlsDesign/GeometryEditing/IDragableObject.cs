using System.Windows;

namespace Ssz.Operator.Core.ControlsDesign.GeometryEditing
{
    public interface IDragableObject
    {
        #region public functions

        bool IsDraged { get; }

        DragInfo? HitTest(Point drawingPoint);

        void StartDrag();

        void DragObject(Point drawingPoint);

        void EndDrag();

        #endregion
    }
}