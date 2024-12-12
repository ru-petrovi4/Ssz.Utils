using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ComboBoxDsShapeView : ControlDsShapeView<ComboBox>
    {
        #region construction and destruction

        public ComboBoxDsShapeView(ComboBoxDsShape dsShape, ControlsPlay.Frame? frame)
            : base(new ComboBox(), dsShape, frame)
        {
            Control.IsEditable = false;
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (ComboBoxDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.MenuItemInfosCollection))
                if (!VisualDesignMode)
                    Control.ItemsSource = GetMenuItems();
            if (propertyName is null || propertyName == nameof(dsShape.SelectedIndexInfo))
                Control.SetBindingOrConst(dsShape.Container, Selector.SelectedIndexProperty,
                    dsShape.SelectedIndexInfo,
                    BindingMode.TwoWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
        }

        #endregion

        #region private functions

        private IEnumerable<object> GetMenuItems()
        {
            var dsShape = (ComboBoxDsShape) DsShapeViewModel.DsShape;
            var container = dsShape.Container;
            var result = new List<object>();
            foreach (ICloneable item in dsShape.MenuItemInfosCollection)
            {
                /*
                var separatorMenuItemInfo = item as SeparatorMenuItemInfo;
                if (separatorMenuItemInfo is not null)
                {
                    var separator = new Separator();

                    separator.SetVisibilityBindingOrConst(dsShape.Container, VisibilityProperty, separatorMenuItemInfo.IsVisibleInfo,
                        Visibility.Collapsed,
                        VisualDesignMode);

                    result.Add(separator);
                    continue;
                }*/
                var menuItemInfo = item as MenuItemInfo;
                if (menuItemInfo is not null)
                {
                    menuItemInfo.ParentItem = dsShape;
                    menuItemInfo.DsCommand.ParentItem = dsShape;
                    var newMenuItem = new TextBlock();
                    newMenuItem.DataContext = DataContext;
                    newMenuItem.SetBindingOrConst(container, TextBlock.TextProperty, menuItemInfo.HeaderInfo,
                        BindingMode.OneWay,
                        UpdateSourceTrigger.Default);

                    newMenuItem.SetVisibilityBindingOrConst(container, VisibilityProperty, menuItemInfo.IsVisibleInfo,
                        Visibility.Collapsed,
                        VisualDesignMode);

                    if (!menuItemInfo.DsCommand.IsEmpty)
                    {
                        newMenuItem.Tag = new DsCommandView(Frame, menuItemInfo.DsCommand, DsShapeViewModel);
                        newMenuItem.MouseUp +=
                            (sender, args) =>
                                Dispatcher.BeginInvoke(new Action(((DsCommandView) ((FrameworkElement) sender).Tag)
                                    .DoCommand));
                    }

                    result.Add(newMenuItem);
                }
            }

            return result.ToArray();
        }

        #endregion
    }
}