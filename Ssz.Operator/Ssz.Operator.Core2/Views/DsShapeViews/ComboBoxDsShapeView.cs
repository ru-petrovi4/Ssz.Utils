using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Threading;
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
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (ComboBoxDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.HorizontalContentAlignment))
                Control.SetConst(dsShape.Container,
                    ComboBox.HorizontalContentAlignmentProperty,
                    dsShape.HorizontalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.VerticalContentAlignment))
                Control.SetConst(dsShape.Container,
                    ComboBox.VerticalContentAlignmentProperty,
                    dsShape.VerticalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.MenuItemInfosCollection))
                if (!VisualDesignMode)
                    Control.ItemsSource = GetMenuItems();
            if (propertyName is null || propertyName == nameof(dsShape.SelectedIndexInfo))
                Control.SetBindingOrConst(dsShape.Container, ComboBox.SelectedIndexProperty,
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

                    separator.SetVisibilityBindingOrConst(dsShape.Container, IsVisibleProperty, separatorMenuItemInfo.IsVisibleInfo,
                        false,
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

                    newMenuItem.SetVisibilityBindingOrConst(container, IsVisibleProperty, menuItemInfo.IsVisibleInfo,
                        false,
                        VisualDesignMode);

                    if (!menuItemInfo.DsCommand.IsEmpty)
                    {
                        newMenuItem.Tag = new DsCommandView(Frame, menuItemInfo.DsCommand, DsShapeViewModel);
                        newMenuItem.PointerReleased +=
                            (sender, args) =>
                                Dispatcher.UIThread.InvokeAsync(new Action(((DsCommandView) ((Control) sender!).Tag!)
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