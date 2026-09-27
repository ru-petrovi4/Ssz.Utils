using Avalonia;
using Avalonia.Input;

namespace Ssz.Operator.Core.ControlsDesign.GeometryEditing
{
    public struct DragInfo
    {
        public Vector Offset;

        public IInputElement RelativeTo;

        public IDragableObject DragObject;
    }
}
