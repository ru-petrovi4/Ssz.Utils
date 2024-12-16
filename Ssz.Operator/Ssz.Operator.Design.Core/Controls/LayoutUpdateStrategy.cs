using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Operator.Design.Controls
{
    class LayoutUpdateStrategy : ILayoutUpdateStrategy
    {
        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
        {
            //AD wants to add the anchorable into destinationContainer
            //just for test provide a new anchorablepane 
            //if the pane is floating let the manager go ahead
            LayoutAnchorablePane? destPane = destinationContainer as LayoutAnchorablePane;
            if (destinationContainer is not null &&
                destinationContainer.FindParent<LayoutFloatingWindow>() is not null)
                return false;

            var toolsPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == "ToolsPane");
            if (toolsPane is not null)
            {
                toolsPane.Children.Add(anchorableToShow);
                return true;
            }

            return false;
        }


        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
        {
        }


        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer)
        {
            return false;
        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
        {

        }
    }
}
