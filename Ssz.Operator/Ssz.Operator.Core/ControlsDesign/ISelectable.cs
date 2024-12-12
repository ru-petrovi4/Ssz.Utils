namespace Ssz.Operator.Core.ControlsDesign
{
    // Common interface for items that can be selected
    // on the DesignCanvas; used by DesignDsShapeView
    public interface ISelectable
    {
        bool IsSelected { get; set; }
        bool IsFirstSelected { get; set; }
    }
}