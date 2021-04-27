namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors
{
    public interface IPropertyGridItem
    {
        bool RefreshForPropertyGridIsDisabled { get; set; }

        void RefreshForPropertyGrid();

        void EndEditInPropertyGrid();
    }
}