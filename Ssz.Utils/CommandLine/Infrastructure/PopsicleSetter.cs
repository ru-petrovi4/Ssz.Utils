using System;


namespace Ssz.Utils.CommandLine.Infrastructure
{
    internal static class PopsicleSetter
    {
        #region public functions

        public static void Set<T>(bool consumed, ref T field, T value)
        {
            if (consumed)
            {
                throw new InvalidOperationException();
            }

            field = value;
        }

        #endregion
    }
}