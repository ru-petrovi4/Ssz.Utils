using System.Collections.Generic;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.Design.Controls
{
    /// <summary>
    ///     What the shapes palette shows: the shapes built into the editor, the ones the addons bring,
    ///     and the complex shapes of the project.
    /// </summary>
    public class DsShapesListViewModel : ViewModelBase
    {
        #region public functions

        public SelectionService<DrawingInfoViewModel> DsShapeDrawingInfosSelectionService { get; } = new();

        /// <summary>
        ///     Null until the complex shapes have been read once; the messages of that reading are
        ///     collected here so they are shown to the author only the first time.
        /// </summary>
        public List<string>? OpenDsShapeDrawingsErrorMessages { get; set; }

        public List<object>? DsShapesTreeViewItemsSource
        {
            get => _dsShapesTreeViewItemsSource;
            set => SetProperty(ref _dsShapesTreeViewItemsSource, value);
        }

        #endregion

        #region private fields

        private List<object>? _dsShapesTreeViewItemsSource;

        #endregion
    }
}
