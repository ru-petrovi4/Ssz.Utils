using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Design.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Design.Controls
{
    public class DrawingDsShapesDockViewModel : DockViewModel
    {
        #region construction and destruction

        public DrawingDsShapesDockViewModel() :
            base(false)
        {
            Title = Resources.DrawingTabItemHeader;            
        }

        #endregion

        #region public functions        

        public IEnumerable<DsShapeViewModel>? DrawingDsShapesTreeViewItemsSource
        {
            get { return _drawingDsShapesTreeViewItemsSource; }
            set { SetValue(ref _drawingDsShapesTreeViewItemsSource, value); }
        }        

        #endregion

        #region private fields

        private IEnumerable<DsShapeViewModel>? _drawingDsShapesTreeViewItemsSource;

        #endregion
    }
}
