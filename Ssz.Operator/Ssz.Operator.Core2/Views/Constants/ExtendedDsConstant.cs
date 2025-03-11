using System;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.Constants
{
    public class ExtendedDsConstant
    {
        #region construction and destruction

        public ExtendedDsConstant(DsConstant dsConstant, ComplexDsShape complexDsShape)
        {
            DsConstant = dsConstant;
            ComplexDsShape = complexDsShape;
        }

        #endregion

        #region public functions

        public DsConstant DsConstant { get; }

        public ComplexDsShape ComplexDsShape { get; }

        #endregion
    }
}