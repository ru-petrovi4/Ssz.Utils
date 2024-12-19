using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Design.Core.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Design.Core.Controls
{
    public class DsPagesListDockViewModel : DockViewModel
    {
        #region construction and destruction

        public DsPagesListDockViewModel() :
            base(false)
        {
            Title = Resources.DsPagesTabItemHeader;            
        }

        #endregion

        #region public functions   

        public SelectionService<DsPageDrawingInfoViewModel> DsPageDrawingInfosSelectionService { get; } = new();

        public List<object>? DsPagesTreeViewItemsSource
        {
            get { return _dsPagesTreeViewItemsSource; }
            set { SetValue(ref _dsPagesTreeViewItemsSource, value); }
        }

        #endregion

        #region private fields

        private List<object>? _dsPagesTreeViewItemsSource;

        #endregion        
    }
}
