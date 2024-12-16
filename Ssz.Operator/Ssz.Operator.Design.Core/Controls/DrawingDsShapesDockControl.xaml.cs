using Ssz.Operator.Core;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ssz.Operator.Design.Controls
{    
    public partial class DrawingDsShapesDockControl : UserControl
    {
        #region construction and destruction

        public DrawingDsShapesDockControl()
        {
            InitializeComponent();
        }

        #endregion        

        #region private functions

        private void DrawingDsShapeTreeViewItemOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = (TreeViewItem)sender;

            treeViewItem.IsSelected = true;

            var focusedDesignDrawingViewModel = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
            if (focusedDesignDrawingViewModel is not null)
                focusedDesignDrawingViewModel.SelectionService.UpdateSelection(treeViewItem.DataContext as DsShapeViewModel);

            e.Handled = true;
        }

        private void DrawingDsShapeTreeViewItemOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var dsShapeViewModel = ((FrameworkElement)sender).DataContext as DsShapeViewModel;

                if (dsShapeViewModel is not null)
                {
                    Point center = dsShapeViewModel.DsShape.GetCenterInitialPositionOnDrawing();
                    DesignDrawingDockControl.ShowOnViewportCenter(
                        DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel,
                        center.X, center.Y);
                    DesignDsProjectViewModel.Instance.ShowFirstSelectedDsShapePropertiesWindow(DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel);
                }
            }

            e.Handled = true;
        }

        #endregion        
    }
}
