using System;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core.CustomAttributes
{
    /// <summary>
    ///     Where a category of properties sits among the others in the property grid.
    ///     <para>
    ///         In the WPF editor this derived from the Xceed toolkit's CategoryOrderAttribute. The
    ///         toolkit is gone, so the attribute stands on its own and the property grid reads it.
    ///     </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DsCategoryOrderAttribute : Attribute
    {
        #region construction and destruction

        public DsCategoryOrderAttribute(string categoryResourceName, int order)
        {
            Category = Resources.ResourceManager.GetString(categoryResourceName, Resources.Culture) ??
                       categoryResourceName;
            Order = order;
        }

        #endregion

        #region public functions

        public string Category { get; }

        public int Order { get; }

        #endregion
    }
}
