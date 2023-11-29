namespace System
{
    public static class StringComparerEx
    {
        public static StringComparer InvariantCultureIgnoreCase =
#if NET5_0_OR_GREATER
            StringComparer.OrdinalIgnoreCase;
#else
        StringComparer.InvariantCultureIgnoreCase;
#endif

        public static StringComparer InvariantCulture =
#if NET5_0_OR_GREATER
            StringComparer.Ordinal;
#else
        StringComparer.InvariantCulture;
#endif
    }
}