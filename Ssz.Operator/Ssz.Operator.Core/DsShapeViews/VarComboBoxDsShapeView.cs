using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapes;
using Ssz.Utils;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class VarComboBoxDsShapeView : ControlDsShapeView<ComboBox>
    {
        #region construction and destruction

        public VarComboBoxDsShapeView(VarComboBoxDsShape dsShape, ControlsPlay.Frame? frame)
            : base(new ComboBox(), dsShape, frame)
        {
            Control.IsEditable = false;
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty MenuItemsProperty = DependencyProperty.Register(
            "MenuItems",
            typeof(string),
            typeof(VarComboBoxDsShapeView),
            new FrameworkPropertyMetadata(@"", MenuItemsProperty_OnChanged));

        public string MenuItems
        {
            get { return (string)GetValue(MenuItemsProperty); }            
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (VarComboBoxDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.MenuItemsInfo))
                this.SetBindingOrConst(dsShape.Container, MenuItemsProperty,
                    dsShape.MenuItemsInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.SelectedIndexInfo))
                Control.SetBindingOrConst(dsShape.Container, Selector.SelectedIndexProperty,
                    dsShape.SelectedIndexInfo,
                    BindingMode.TwoWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
        }

        #endregion

        #region private functions

        private static void MenuItemsProperty_OnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var thisDsShapeView = (VarComboBoxDsShapeView)sender;
            string menuItems = thisDsShapeView.MenuItems;
            if (String.IsNullOrEmpty(menuItems))
                thisDsShapeView.Control.ItemsSource = null;
            else
                thisDsShapeView.Control.ItemsSource = CsvHelper.ParseCsvLine(@",", menuItems);
        }

        #endregion
    }
}