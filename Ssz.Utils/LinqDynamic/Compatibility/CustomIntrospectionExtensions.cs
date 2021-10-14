using System.Linq;

// ReSharper disable once CheckNamespace
namespace System.Reflection
{
    /// <summary>
    /// https://github.com/castleproject/Core/blob/netcore/src/Castle.Core/Compatibility/IntrospectionExtensions.cs
    /// </summary>
	internal static class CustomIntrospectionExtensions
    {
        public static Type[] GetGenericTypeArguments(this TypeInfo typeInfo)
        {
            return typeInfo.GenericTypeArguments;
        }
    }
}
