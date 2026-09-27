namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     An object the property grid edits and asks to refresh itself.
    ///     <para>
    ///         Ported from the Xceed WPF toolkit, where the property grid came from. The shapes were
    ///         already written against it, so the interface keeps its name and members.
    ///     </para>
    /// </summary>
    public interface IPropertyGridItem
    {
        bool RefreshForPropertyGridIsDisabled { get; set; }

        void RefreshForPropertyGrid();

        void EndEditInPropertyGrid();
    }
}
