using System.Collections.Generic;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.Design.Controls
{
    /// <summary>
    ///     What the pages list of the editor shows and what is selected in it.
    /// </summary>
    public class DsPagesListViewModel : ViewModelBase
    {
        #region public functions

        public SelectionService<DsPageDrawingInfoViewModel> DsPageDrawingInfosSelectionService { get; } = new();

        /// <summary>
        ///     The roots of the tree: pages that belong to no group, then the groups.
        /// </summary>
        public List<object>? DsPagesTreeViewItemsSource
        {
            get => _dsPagesTreeViewItemsSource;
            set => SetProperty(ref _dsPagesTreeViewItemsSource, value);
        }

        #endregion

        #region private fields

        private List<object>? _dsPagesTreeViewItemsSource;

        #endregion
    }
}
