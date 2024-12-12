using System;
using System.Collections.Generic;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.DsShapes
{
    public interface IDsShapeFactory
    {
        IEnumerable<EntityInfo> GetDsShapeTypes();

        DsShapeBase? NewDsShape(Guid dsShapeTypeGuid, bool visualDesignMode, bool loadXamlContent);

        DsShapeViewBase? NewDsShapeView(DsShapeBase dsShape, Frame? frame);
    }
}