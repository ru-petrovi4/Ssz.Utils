using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class SszStatement
    {
        #region construction and destruction

        public SszStatement()
        {
            Condition = new SszExpression();
            Value = new SszExpression();
        }

        public SszStatement(string condition, string value, int paramNum)
        {            
            Condition = new SszExpression(condition);
            Value = new SszExpression(value);
            ParamNum = paramNum;
        }

        #endregion

        #region public functions

        public SszExpression Condition { get; set; }

        public SszExpression Value { get; set; }

        public int ParamNum { get; set; }

        #endregion
    }
}
