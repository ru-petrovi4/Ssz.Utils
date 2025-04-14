using Ssz.Operator.Core.Drawings;

namespace Ssz.Operator.Core.DsShapes
{
    public class DsShapeInfo
    {
        #region public functions

        public string Name { get; set; } = @"";

        public string Desc { get; set; } = @"";

        public string DsShapeTypeNameToDisplay { get; set; } = @"";

        public bool IsRootDsShape { get; set; }

        public int Index { get; set; }       

        #endregion
    }
}