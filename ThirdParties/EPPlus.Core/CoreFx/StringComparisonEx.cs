namespace System
{
    public static class StringComparisonEx
    {
        public static StringComparison InvariantCultureIgnoreCase =
#if NET5_0_OR_GREATER
            StringComparison.OrdinalIgnoreCase;
#else
        StringComparison.InvariantCultureIgnoreCase;
#endif

        public static StringComparison InvariantCulture =
#if NET5_0_OR_GREATER
            StringComparison.Ordinal;
#else
        StringComparison.InvariantCulture;
#endif
    }
}