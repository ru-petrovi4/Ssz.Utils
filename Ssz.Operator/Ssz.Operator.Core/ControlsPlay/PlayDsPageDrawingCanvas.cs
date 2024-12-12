using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Ssz.Operator.Core.Commands;

using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay
{
    public class PlayDsPageDrawingCanvas : PlayDrawingCanvas
    {
        #region construction and destruction

        public PlayDsPageDrawingCanvas(DsPageDrawing dsPageDrawing, Frame? frame)
            : base(dsPageDrawing, frame)
        {
            ContextMenu = new ContextMenu();

            ContextMenuOpening += OnContextMenuOpening;
            
            Background = dsPageDrawing.ComputeDsPageBackgroundBrush();

            ClipToBounds = true;
        }        

        #endregion        

        #region private functions

        private static IEnumerable<object> GetMenuItems(
            IEnumerable<Tuple<ContextMenuDsShapeView, ICloneable>> infosList)
        {
            var items = new List<(string, object)>();
            foreach (var i in infosList)
            {
                ContextMenuDsShapeView contextMenuDsShapeView = i.Item1;                
                var container = contextMenuDsShapeView.DsShapeViewModel.DsShape.Container;

                var separatorMenuItemInfo = i.Item2 as SeparatorMenuItemInfo;
                if (separatorMenuItemInfo is not null)
                {
                    var separator = new Separator();

                    separatorMenuItemInfo.IsVisibleInfo.FallbackValue = Visibility.Collapsed;
                    separator.SetVisibilityBindingOrConst(container, VisibilityProperty,
                        separatorMenuItemInfo.IsVisibleInfo,
                        Visibility.Collapsed,
                        contextMenuDsShapeView.VisualDesignMode);

                    items.Add((separatorMenuItemInfo.Path, separator));
                    continue;
                }

                var menuItemInfo = i.Item2 as MenuItemInfo;
                if (menuItemInfo is not null)
                {
                    var newMenuItem = new MenuItem();
                    newMenuItem.DataContext = contextMenuDsShapeView.DataContext;
                    newMenuItem.SetBindingOrConst(container, HeaderedItemsControl.HeaderProperty,
                        menuItemInfo.HeaderInfo,
                        BindingMode.OneWay,
                        UpdateSourceTrigger.Default);

                    menuItemInfo.IsVisibleInfo.FallbackValue = Visibility.Collapsed;
                    newMenuItem.SetVisibilityBindingOrConst(container, VisibilityProperty, menuItemInfo.IsVisibleInfo,
                        Visibility.Collapsed, contextMenuDsShapeView.VisualDesignMode);

                    newMenuItem.Tag = new DsCommandView(contextMenuDsShapeView.Frame, menuItemInfo.DsCommand,
                        contextMenuDsShapeView.DsShapeViewModel);
                    newMenuItem.Click +=
                        (sender, args) =>
                            contextMenuDsShapeView.Dispatcher.BeginInvoke(
                                new Action(((DsCommandView) ((FrameworkElement) sender).Tag).DoCommand));

                    items.Add((menuItemInfo.Path, newMenuItem));
                }
            }

            var result = new List<object>();
            foreach (var it in items.GroupBy(i => i.Item1))
            {
                if (!String.IsNullOrEmpty(it.Key))
                {
                    var newMenuItem = new MenuItem();
                    newMenuItem.Header = it.Key;
                    foreach (var i in it)
                    {
                        newMenuItem.Items.Add(i.Item2);
                    }
                    result.Add(newMenuItem);
                }
            }
            foreach (var it in items)
            {
                if (String.IsNullOrEmpty(it.Item1))
                {
                    result.Add(it.Item2);
                }
            }
            return result;
        }

        private void OnContextMenuOpening(object? sender, ContextMenuEventArgs e)
        {
            var pt = Mouse.GetPosition(this);

            var infosList = new List<Tuple<ContextMenuDsShapeView, ICloneable>>();
            var menuItemsInfosHash = new HashSet<ICloneable>();
            foreach (
                ContextMenuDsShapeView contextMenuDsShapeView in
                TreeHelper.FindChilds<ContextMenuDsShapeView>(this,
                        sv => sv.DsShapeViewModel.DsShape.Contains(pt, false))
                    .OrderBy(shv => shv.DsShapeViewModel.DsShape.Index)
            )
            {
                var dsShape = (ContextMenuDsShape) contextMenuDsShapeView.DsShapeViewModel.DsShape;
                foreach (ICloneable menuItem in dsShape.MenuItemInfosCollection)
                {
                    if (menuItemsInfosHash.Contains(menuItem)) continue;
                    var menuItemInfo = menuItem as MenuItemInfo;
                    if (menuItemInfo is not null)
                    {
                        menuItemInfo.ParentItem = dsShape;
                        menuItemInfo.DsCommand.ParentItem = dsShape;
                    }

                    infosList.Add(Tuple.Create(contextMenuDsShapeView, menuItem));
                    menuItemsInfosHash.Add(menuItem);
                }
            }

            ContextMenu.Items.Clear();
            foreach (object menuItem in GetMenuItems(infosList))
            {
                ContextMenu.Items.Add(menuItem);
            }
            if (ContextMenu.Items.Count == 0)
            {
                var parentWithContextMenu = TreeHelper.FindParent<FrameworkElement>(this, fe => fe.ContextMenu is not null);
                if (parentWithContextMenu is not null)
                {
                    ContextMenu parentContextMenu = parentWithContextMenu.ContextMenu;
                    if (parentContextMenu.Items.Count > 0) parentContextMenu.IsOpen = true;
                }

                e.Handled = true;
            }
        }

        /*



        private IEnumerable<T> GetDsShapeViewsAtCurrentMousePosition<T>()
            where T : DsShapeViewBase
        {
            var result = new List<T>();

            Point pt = Mouse.GetPosition(this);
            //var pt = new Point(contextMenuEventArgs.CursorLeft, contextMenuEventArgs.CursorTop);

            // Clear the contents of the list used for hit test results.
            _hitResultsList.Clear();

            // Set up a callback to receive the hit test result enumeration.
            VisualTreeHelper.HitTest(this, null,
                MyHitTestResult,
                new PointHitTestParameters(pt));

            // Perform actions on the hit test results list. 
            foreach (DependencyObject hitResult in _hitResultsList)
            {
                var dsShapeView = TreeHelper.FindParentOrSelf<T>(hitResult);
                if (dsShapeView is not null && dsShapeView.IsVisible && dsShapeView.IsEnabled)
                {
                    result.Add(dsShapeView);
                }
            }

            return result.Distinct();
        }

        private HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            _hitResultsList.Add(result.VisualHit);

            // Set the behavior to return visuals at all z-order levels. 
            return HitTestResultBehavior.Continue;
        }

        private readonly List<DependencyObject> _hitResultsList = new List<DependencyObject>();
        */

        #endregion
    }
}