using System.Reflection.Emit;

// ReSharper disable once CheckNamespace
namespace System.Reflection
{
    internal static class CustomTypeBuilderExtensions
    {
        // https://github.com/castleproject/Core/blob/netcore/src/Castle.Core/Compatibility/CustomTypeBuilderExtensions.cs
        // TypeBuilder and GenericTypeParameterBuilder no longer inherit from Type but TypeInfo,
        // so there is now an AsType method to get the Type which we are providing here to shim to itself.
        public static Type AsType(this TypeBuilder builder)
        {
            return builder;
        }

        public static Type AsType(this GenericTypeParameterBuilder builder)
        {
            return builder;
        }
    }
}
