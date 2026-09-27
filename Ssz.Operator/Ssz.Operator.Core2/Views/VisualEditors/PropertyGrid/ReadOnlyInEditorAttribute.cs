using System;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     Marks a property the property grid shows but does not let the author change.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ReadOnlyInEditorAttribute : Attribute
    {
    }

    /// <summary>
    ///     Marks a property whose own properties the grid shows as a nested group.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
    public class ExpandableObjectAttribute : Attribute
    {
    }

    /// <summary>
    ///     The order of a property inside its category. Properties without one follow those with one,
    ///     in the order their names give.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class PropertyOrderAttribute : Attribute
    {
        public PropertyOrderAttribute(int order)
        {
            Order = order;
        }

        public int Order { get; }
    }
}
