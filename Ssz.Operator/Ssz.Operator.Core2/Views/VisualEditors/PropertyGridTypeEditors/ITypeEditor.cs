using Avalonia.Controls;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     An editor a property asks for by name, through the Editor attribute it carries.
    ///     <para>
    ///         Ported from the WPF editor, where the same attribute named a control implementing the
    ///         property grid toolkit's ITypeEditor. The shape of the contract is unchanged: the editor
    ///         is handed the row and answers with the control that edits it.
    ///     </para>
    /// </summary>
    public interface ITypeEditor
    {
        Control ResolveEditor(PropertyItemViewModel propertyItem);
    }
}
