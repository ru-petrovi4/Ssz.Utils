// ReSharper disable once CheckNamespace
namespace System.Linq.Expressions
{
    internal static class LambdaExpressionExtensions
    {
        public static Type GetReturnType(this LambdaExpression lambdaExpression)
        {
            return lambdaExpression.Body.Type;
        }
    }
}
