using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq.Dynamic.Core.Parser.SupportedMethods
{
    internal class MethodData
    {
        public MethodBase MethodBase { get; set; } = null!;

        public ParameterInfo[] Parameters { get; set; } = null!;

        public Expression[] Args { get; set; } = null!;
    }
}
