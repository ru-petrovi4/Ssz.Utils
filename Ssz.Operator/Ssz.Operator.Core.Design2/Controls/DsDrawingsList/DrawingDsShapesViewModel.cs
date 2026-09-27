using System.Collections.Generic;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Design.Properties;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.Design.Controls
{
    /// <summary>
    ///     What the tree of the shapes of the focused drawing shows.
    /// </summary>
    public class DrawingDsShapesViewModel : ViewModelBase
    {
        #region public functions

        public string Title => Resources.DrawingTabItemHeader;

        public IEnumerable<DsShapeViewModel>? DrawingDsShapesTreeViewItemsSource
        {
            get => _drawingDsShapesTreeViewItemsSource;
            set => SetProperty(ref _drawingDsShapesTreeViewItemsSource, value);
        }

        #endregion

        #region private fields

        private IEnumerable<DsShapeViewModel>? _drawingDsShapesTreeViewItemsSource;

        #endregion
    }
}
