/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class DocumentPaneTabPanel : Panel
    {
        #region Constructors

        public DocumentPaneTabPanel()
        {
            FlowDirection = FlowDirection.LeftToRight;
        }

        #endregion

        #region Overrides

        protected override Size MeasureOverride(Size availableSize)
        {
            var visibleChildren = Children.Cast<UIElement>().Where(ch => ch.Visibility != Visibility.Collapsed);

            var desideredSize = new Size();
            foreach (FrameworkElement child in Children)
            {
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                desideredSize.Width += child.DesiredSize.Width;

                desideredSize.Height = Math.Max(desideredSize.Height, child.DesiredSize.Height);
            }

            return new Size(Math.Min(desideredSize.Width, availableSize.Width), desideredSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var visibleChildren = Children.Cast<UIElement>().Where(ch => ch.Visibility != Visibility.Collapsed);
            var offset = 0.0;
            var skipAllOthers = false;
            foreach (TabItem doc in visibleChildren)
            {
                var layoutContent = doc.Content as LayoutContent;
                if (skipAllOthers || offset + doc.DesiredSize.Width > finalSize.Width)
                {
                    if (layoutContent.IsSelected && !doc.IsVisible)
                    {
                        var parentContainer = layoutContent.Parent;
                        var parentSelector = layoutContent.Parent as ILayoutContentSelector;
                        var parentPane = layoutContent.Parent as ILayoutPane;
                        var contentIndex = parentSelector.IndexOf(layoutContent);
                        if (contentIndex > 0 &&
                            parentContainer.ChildrenCount > 1)
                        {
                            parentPane.MoveChild(contentIndex, 0);
                            parentSelector.SelectedContentIndex = 0;
                            return ArrangeOverride(finalSize);
                        }
                    }

                    doc.Visibility = Visibility.Hidden;
                    skipAllOthers = true;
                }
                else
                {
                    doc.Visibility = Visibility.Visible;
                    doc.Arrange(new Rect(offset, 0.0, doc.DesiredSize.Width, finalSize.Height));
                    offset += doc.ActualWidth + doc.Margin.Left + doc.Margin.Right;
                }
            }

            return finalSize;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            //if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed &&
            //    LayoutDocumentTabItem.IsDraggingItem())
            //{
            //    var contentModel = LayoutDocumentTabItem.GetDraggingItem().Model;
            //    var manager = contentModel.Root.Manager;
            //    LayoutDocumentTabItem.ResetDraggingItem();
            //    System.Diagnostics.Trace.WriteLine("OnMouseLeave()");


            //    manager.StartDraggingFloatingWindowForContent(contentModel);
            //}

            base.OnMouseLeave(e);
        }

        #endregion
    }
}