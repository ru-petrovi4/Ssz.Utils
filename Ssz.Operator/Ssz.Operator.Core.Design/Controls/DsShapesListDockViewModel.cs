﻿using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Design.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Design.Controls
{
    public class DsShapesListDockViewModel : DockViewModel
    {
        #region construction and destruction

        public DsShapesListDockViewModel() :
            base(false)
        {
            Title = Resources.DsShapesTabItemHeader;            
        }

        #endregion

        #region public functions

        public SelectionService<DrawingInfoViewModel> DsShapeDrawingInfosSelectionService { get; } = new();

        public List<string>? OpenDsShapeDrawingsErrorMessages { get; set; }

        public List<object>? DsShapesTreeViewItemsSource
        {
            get { return _dsShapesTreeViewItemsSource; }
            set { SetValue(ref _dsShapesTreeViewItemsSource, value); }
        }

        #endregion

        #region private fields

        private List<object>? _dsShapesTreeViewItemsSource;

        #endregion  
    }
}
