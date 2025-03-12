using System;

namespace Ssz.Operator.Core.Design.Controls
{
    public class DsPageDrawingInfosGroupViewModel : GroupViewModel
    {
        #region construction and destruction

        public DsPageDrawingInfosGroupViewModel(string? drawingGroup, Guid? drawingTypeGuid)
        {
            DrawingGroup = drawingGroup;
            DrawingTypeGuid = drawingTypeGuid;
        }

        #endregion

        #region public functions

        public string? DrawingGroup { get; protected set; }

        public Guid? DrawingTypeGuid { get; protected set; }

        #endregion
    }
}