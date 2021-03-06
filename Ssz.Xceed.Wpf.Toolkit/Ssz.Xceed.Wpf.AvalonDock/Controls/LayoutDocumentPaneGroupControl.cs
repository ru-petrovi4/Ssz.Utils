/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutDocumentPaneGroupControl : LayoutGridControl<ILayoutDocumentPane>, ILayoutControl
    {
        #region Members

        private LayoutDocumentPaneGroup _model;

        #endregion

        #region Constructors

        internal LayoutDocumentPaneGroupControl(LayoutDocumentPaneGroup model)
            : base(model, model.Orientation)
        {
            _model = model;
        }

        #endregion

        #region Overrides

        protected override void OnFixChildrenDockLengths()
        {
            //if( _model.Orientation == Orientation.Horizontal )
            //{
            //  for( int i = 0; i < _model.Children.Count; i++ )
            //  {
            //    var childModel = _model.Children[ i ] as ILayoutPositionableElement;
            //    if( !childModel.DockWidth.IsStar )
            //    {
            //      childModel.DockWidth = new GridLength( 1.0, GridUnitType.Star );
            //    }
            //  }
            //}
            //else
            //{
            //  for( int i = 0; i < _model.Children.Count; i++ )
            //  {
            //    var childModel = _model.Children[ i ] as ILayoutPositionableElement;
            //    if( !childModel.DockHeight.IsStar )
            //    {
            //      childModel.DockHeight = new GridLength( 1.0, GridUnitType.Star );
            //    }
            //  }
            //}
            // #endregion
        }

        #endregion
    }
}