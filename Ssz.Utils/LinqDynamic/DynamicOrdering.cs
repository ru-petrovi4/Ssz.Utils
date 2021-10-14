using System.Linq.Expressions;

namespace System.Linq.Dynamic.Core
{
    internal class DynamicOrdering
    {
        public Expression Selector = null!;
        public bool Ascending;
        public string MethodName = @"";
    }
}