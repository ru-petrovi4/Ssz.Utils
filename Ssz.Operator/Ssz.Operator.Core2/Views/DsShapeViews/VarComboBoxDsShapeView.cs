using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
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
        }

        #endregion

        #region public functions

        public static readonly AvaloniaProperty MenuItemsProperty = AvaloniaProperty.Register<VarComboBoxDsShapeView, string>(
            "MenuItems",
            @"");

        public string MenuItems
        {
            get { return (string)GetValue(MenuItemsProperty)!; }            
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (VarComboBoxDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.HorizontalContentAlignment))
                Control.SetConst(dsShape.Container,
                    ComboBox.HorizontalContentAlignmentProperty,
                    dsShape.HorizontalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.VerticalContentAlignment))
                Control.SetConst(dsShape.Container,
                    ComboBox.VerticalContentAlignmentProperty,
                    dsShape.VerticalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.MenuItemsInfo))
                this.SetBindingOrConst(dsShape.Container, MenuItemsProperty,
                    dsShape.MenuItemsInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.SelectedIndexInfo))
                Control.SetBindingOrConst(dsShape.Container, SelectingItemsControl.SelectedIndexProperty,
                    dsShape.SelectedIndexInfo,
                    BindingMode.TwoWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == MenuItemsProperty)
            {
                string menuItems = MenuItems;
                if (String.IsNullOrEmpty(menuItems))
                    Control.ItemsSource = null;
                else
                    Control.ItemsSource = CsvHelper.ParseCsvLine(@",", menuItems);
            }

            base.OnPropertyChanged(change);
        }

        #endregion
    }
}