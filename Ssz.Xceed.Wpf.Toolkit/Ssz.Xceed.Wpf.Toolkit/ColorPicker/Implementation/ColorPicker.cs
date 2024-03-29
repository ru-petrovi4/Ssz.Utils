﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public enum ColorMode
    {
        ColorPalette,
        ColorCanvas
    }

    [TemplatePart(Name = PART_AvailableColors, Type = typeof(ListBox))]
    [TemplatePart(Name = PART_StandardColors, Type = typeof(ListBox))]
    [TemplatePart(Name = PART_RecentColors, Type = typeof(ListBox))]
    [TemplatePart(Name = PART_ColorPickerToggleButton, Type = typeof(ToggleButton))]
    [TemplatePart(Name = PART_ColorPickerPalettePopup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_ColorModeButton, Type = typeof(Button))]
    public class ColorPicker : Control
    {
        private const string PART_AvailableColors = "PART_AvailableColors";
        private const string PART_StandardColors = "PART_StandardColors";
        private const string PART_RecentColors = "PART_RecentColors";
        private const string PART_ColorPickerToggleButton = "PART_ColorPickerToggleButton";
        private const string PART_ColorPickerPalettePopup = "PART_ColorPickerPalettePopup";
        private const string PART_ColorModeButton = "PART_ColorModeButton";

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_availableColors is not null)
                _availableColors.SelectionChanged -= Color_SelectionChanged;

            _availableColors = GetTemplateChild(PART_AvailableColors) as ListBox;
            if (_availableColors is not null)
                _availableColors.SelectionChanged += Color_SelectionChanged;

            if (_standardColors is not null)
                _standardColors.SelectionChanged -= Color_SelectionChanged;

            _standardColors = GetTemplateChild(PART_StandardColors) as ListBox;
            if (_standardColors is not null)
                _standardColors.SelectionChanged += Color_SelectionChanged;

            if (_recentColors is not null)
                _recentColors.SelectionChanged -= Color_SelectionChanged;

            _recentColors = GetTemplateChild(PART_RecentColors) as ListBox;
            if (_recentColors is not null)
                _recentColors.SelectionChanged += Color_SelectionChanged;

            if (_popup is not null)
                _popup.Opened -= Popup_Opened;

            _popup = GetTemplateChild(PART_ColorPickerPalettePopup) as Popup;
            if (_popup is not null)
                _popup.Opened += Popup_Opened;

            _toggleButton = Template.FindName(PART_ColorPickerToggleButton, this) as ToggleButton;

            if (_colorModeButton is not null)
                _colorModeButton.Click -= ColorModeButton_Clicked;

            _colorModeButton = Template.FindName(PART_ColorModeButton, this) as Button;

            if (_colorModeButton is not null)
                _colorModeButton.Click += ColorModeButton_Clicked;
        }

        #endregion //Base Class Overrides

        #region Members

        private ListBox _availableColors;
        private ListBox _standardColors;
        private ListBox _recentColors;
        private ToggleButton _toggleButton;
        private Popup _popup;
        private Button _colorModeButton;

        #endregion //Members

        #region Properties

        #region AvailableColors

        public static readonly DependencyProperty AvailableColorsProperty =
            DependencyProperty.Register("AvailableColors", typeof(ObservableCollection<ColorItem>), typeof(ColorPicker),
                new UIPropertyMetadata(CreateAvailableColors()));

        public ObservableCollection<ColorItem> AvailableColors
        {
            get => (ObservableCollection<ColorItem>) GetValue(AvailableColorsProperty);
            set => SetValue(AvailableColorsProperty, value);
        }

        #endregion //AvailableColors

        #region AvailableColorsHeader

        public static readonly DependencyProperty AvailableColorsHeaderProperty =
            DependencyProperty.Register("AvailableColorsHeader", typeof(string), typeof(ColorPicker),
                new UIPropertyMetadata("Available Colors"));

        public string AvailableColorsHeader
        {
            get => (string) GetValue(AvailableColorsHeaderProperty);
            set => SetValue(AvailableColorsHeaderProperty, value);
        }

        #endregion //AvailableColorsHeader

        #region ButtonStyle

        public static readonly DependencyProperty ButtonStyleProperty =
            DependencyProperty.Register("ButtonStyle", typeof(Style), typeof(ColorPicker));

        public Style ButtonStyle
        {
            get => (Style) GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }

        #endregion //ButtonStyle

        #region DisplayColorAndName

        public static readonly DependencyProperty DisplayColorAndNameProperty =
            DependencyProperty.Register("DisplayColorAndName", typeof(bool), typeof(ColorPicker),
                new UIPropertyMetadata(false));

        public bool DisplayColorAndName
        {
            get => (bool) GetValue(DisplayColorAndNameProperty);
            set => SetValue(DisplayColorAndNameProperty, value);
        }

        #endregion //DisplayColorAndName

        #region ColorMode

        public static readonly DependencyProperty ColorModeProperty = DependencyProperty.Register("ColorMode",
            typeof(ColorMode), typeof(ColorPicker), new UIPropertyMetadata(ColorMode.ColorPalette));

        public ColorMode ColorMode
        {
            get => (ColorMode) GetValue(ColorModeProperty);
            set => SetValue(ColorModeProperty, value);
        }

        #endregion //ColorMode

        #region IsOpen

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(ColorPicker), new UIPropertyMetadata(false));

        public bool IsOpen
        {
            get => (bool) GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        #endregion //IsOpen

        #region RecentColors

        public static readonly DependencyProperty RecentColorsProperty = DependencyProperty.Register("RecentColors",
            typeof(ObservableCollection<ColorItem>), typeof(ColorPicker), new UIPropertyMetadata(null));

        public ObservableCollection<ColorItem> RecentColors
        {
            get => (ObservableCollection<ColorItem>) GetValue(RecentColorsProperty);
            set => SetValue(RecentColorsProperty, value);
        }

        #endregion //RecentColors

        #region RecentColorsHeader

        public static readonly DependencyProperty RecentColorsHeaderProperty =
            DependencyProperty.Register("RecentColorsHeader", typeof(string), typeof(ColorPicker),
                new UIPropertyMetadata("Recent Colors"));

        public string RecentColorsHeader
        {
            get => (string) GetValue(RecentColorsHeaderProperty);
            set => SetValue(RecentColorsHeaderProperty, value);
        }

        #endregion //RecentColorsHeader

        #region SelectedColor

        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register("SelectedColor",
            typeof(Color), typeof(ColorPicker),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedColorPropertyChanged));

        public Color SelectedColor
        {
            get => (Color) GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        private static void OnSelectedColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker) d;
            if (colorPicker is not null)
                colorPicker.OnSelectedColorChanged((Color) e.OldValue, (Color) e.NewValue);
        }

        private void OnSelectedColorChanged(Color oldValue, Color newValue)
        {
            SelectedColorText = GetFormatedColorString(newValue);

            var args = new RoutedPropertyChangedEventArgs<Color>(oldValue, newValue);
            args.RoutedEvent = SelectedColorChangedEvent;
            RaiseEvent(args);
        }

        #endregion //SelectedColor

        #region SelectedColorText

        public static readonly DependencyProperty SelectedColorTextProperty =
            DependencyProperty.Register("SelectedColorText", typeof(string), typeof(ColorPicker),
                new UIPropertyMetadata("Black"));

        public string SelectedColorText
        {
            get => (string) GetValue(SelectedColorTextProperty);
            protected set => SetValue(SelectedColorTextProperty, value);
        }

        #endregion //SelectedColorText

        #region ShowAdvancedButton

        public static readonly DependencyProperty ShowAdvancedButtonProperty =
            DependencyProperty.Register("ShowAdvancedButton", typeof(bool), typeof(ColorPicker),
                new UIPropertyMetadata(true));

        public bool ShowAdvancedButton
        {
            get => (bool) GetValue(ShowAdvancedButtonProperty);
            set => SetValue(ShowAdvancedButtonProperty, value);
        }

        #endregion //ShowAdvancedButton

        #region ShowAvailableColors

        public static readonly DependencyProperty ShowAvailableColorsProperty =
            DependencyProperty.Register("ShowAvailableColors", typeof(bool), typeof(ColorPicker),
                new UIPropertyMetadata(true));

        public bool ShowAvailableColors
        {
            get => (bool) GetValue(ShowAvailableColorsProperty);
            set => SetValue(ShowAvailableColorsProperty, value);
        }

        #endregion //ShowAvailableColors

        #region ShowRecentColors

        public static readonly DependencyProperty ShowRecentColorsProperty =
            DependencyProperty.Register("ShowRecentColors", typeof(bool), typeof(ColorPicker),
                new UIPropertyMetadata(false));

        public bool ShowRecentColors
        {
            get => (bool) GetValue(ShowRecentColorsProperty);
            set => SetValue(ShowRecentColorsProperty, value);
        }

        #endregion //DisplayRecentColors

        #region ShowStandardColors

        public static readonly DependencyProperty ShowStandardColorsProperty =
            DependencyProperty.Register("ShowStandardColors", typeof(bool), typeof(ColorPicker),
                new UIPropertyMetadata(true));

        public bool ShowStandardColors
        {
            get => (bool) GetValue(ShowStandardColorsProperty);
            set => SetValue(ShowStandardColorsProperty, value);
        }

        #endregion //DisplayStandardColors

        #region ShowDropDownButton

        public static readonly DependencyProperty ShowDropDownButtonProperty =
            DependencyProperty.Register("ShowDropDownButton", typeof(bool), typeof(ColorPicker),
                new UIPropertyMetadata(true));

        public bool ShowDropDownButton
        {
            get => (bool) GetValue(ShowDropDownButtonProperty);
            set => SetValue(ShowDropDownButtonProperty, value);
        }

        #endregion //ShowDropDownButton

        #region StandardColors

        public static readonly DependencyProperty StandardColorsProperty = DependencyProperty.Register("StandardColors",
            typeof(ObservableCollection<ColorItem>), typeof(ColorPicker),
            new UIPropertyMetadata(CreateStandardColors()));

        public ObservableCollection<ColorItem> StandardColors
        {
            get => (ObservableCollection<ColorItem>) GetValue(StandardColorsProperty);
            set => SetValue(StandardColorsProperty, value);
        }

        #endregion //StandardColors

        #region StandardColorsHeader

        public static readonly DependencyProperty StandardColorsHeaderProperty =
            DependencyProperty.Register("StandardColorsHeader", typeof(string), typeof(ColorPicker),
                new UIPropertyMetadata("Standard Colors"));

        public string StandardColorsHeader
        {
            get => (string) GetValue(StandardColorsHeaderProperty);
            set => SetValue(StandardColorsHeaderProperty, value);
        }

        #endregion //StandardColorsHeader

        #region UsingAlphaChannel

        public static readonly DependencyProperty UsingAlphaChannelProperty =
            DependencyProperty.Register("UsingAlphaChannel", typeof(bool), typeof(ColorPicker),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnUsingAlphaChannelPropertyChanged));

        public bool UsingAlphaChannel
        {
            get => (bool) GetValue(UsingAlphaChannelProperty);
            set => SetValue(UsingAlphaChannelProperty, value);
        }

        private static void OnUsingAlphaChannelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker) d;
            if (colorPicker is not null)
                colorPicker.OnUsingAlphaChannelChanged();
        }

        private void OnUsingAlphaChannelChanged()
        {
            SelectedColorText = GetFormatedColorString(SelectedColor);
        }

        #endregion //UsingAlphaChannel

        #endregion //Properties

        #region Constructors

        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker),
                new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        public ColorPicker()
        {
            RecentColors = new ObservableCollection<ColorItem>();
            Keyboard.AddKeyDownHandler(this, OnKeyDown);
            Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseDownOutsideCapturedElement);
        }

        #endregion //Constructors

        #region Event Handlers

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsOpen)
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    IsOpen = true;
                    // Focus will be on ListBoxItem in Popup_Opened().
                    e.Handled = true;
                }
            }
            else
            {
                if (KeyboardUtilities.IsKeyModifyingPopupState(e))
                {
                    CloseColorPicker(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    CloseColorPicker(true);
                    e.Handled = true;
                }
            }
        }

        private void OnMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
        {
            CloseColorPicker(false);
        }

        private void Color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = (ListBox) sender;

            if (e.AddedItems.Count > 0)
            {
                var colorItem = (ColorItem) e.AddedItems[0];
                SelectedColor = colorItem.Color;
                UpdateRecentColors(colorItem);
                CloseColorPicker(true);
                lb.SelectedIndex = -1; //for now I don't care about keeping track of the selected color
            }
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            if (_availableColors is not null && ShowAvailableColors)
                FocusOnListBoxItem(_availableColors);
            else if (_standardColors is not null && ShowStandardColors)
                FocusOnListBoxItem(_standardColors);
            else if (_recentColors is not null && ShowRecentColors)
                FocusOnListBoxItem(_recentColors);
        }

        private void FocusOnListBoxItem(ListBox listBox)
        {
            var listBoxItem = (ListBoxItem) listBox.ItemContainerGenerator.ContainerFromItem(listBox.SelectedItem);
            if (listBoxItem is null && listBox.Items.Count > 0)
                listBoxItem = (ListBoxItem) listBox.ItemContainerGenerator.ContainerFromItem(listBox.Items[0]);
            if (listBoxItem is not null)
                listBoxItem.Focus();
        }

        private void ColorModeButton_Clicked(object sender, RoutedEventArgs e)
        {
            ColorMode = ColorMode == ColorMode.ColorPalette ? ColorMode.ColorCanvas : ColorMode.ColorPalette;
        }

        #endregion //Event Handlers

        #region Events

        public static readonly RoutedEvent SelectedColorChangedEvent =
            EventManager.RegisterRoutedEvent("SelectedColorChanged", RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ColorPicker));

        public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged
        {
            add => AddHandler(SelectedColorChangedEvent, value);
            remove => RemoveHandler(SelectedColorChangedEvent, value);
        }

        #endregion //Events

        #region Methods

        private void CloseColorPicker(bool isFocusOnColorPicker)
        {
            if (IsOpen)
                IsOpen = false;
            ReleaseMouseCapture();

            if (isFocusOnColorPicker && _toggleButton is not null)
                _toggleButton.Focus();
            UpdateRecentColors(new ColorItem(SelectedColor, SelectedColorText));
        }

        private void UpdateRecentColors(ColorItem colorItem)
        {
            if (!RecentColors.Contains(colorItem))
                RecentColors.Add(colorItem);

            if (RecentColors.Count > 10) //don't allow more than ten, maybe make a property that can be set by the user.
                RecentColors.RemoveAt(0);
        }

        private string GetFormatedColorString(Color colorToFormat)
        {
            return ColorUtilities.FormatColorString(colorToFormat.GetColorName(), UsingAlphaChannel);
        }

        private static ObservableCollection<ColorItem> CreateStandardColors()
        {
            var standardColors = new ObservableCollection<ColorItem>();
            standardColors.Add(new ColorItem(Colors.Transparent, "Transparent"));
            standardColors.Add(new ColorItem(Colors.White, "White"));
            standardColors.Add(new ColorItem(Colors.Gray, "Gray"));
            standardColors.Add(new ColorItem(Colors.Black, "Black"));
            standardColors.Add(new ColorItem(Colors.Red, "Red"));
            standardColors.Add(new ColorItem(Colors.Green, "Green"));
            standardColors.Add(new ColorItem(Colors.Blue, "Blue"));
            standardColors.Add(new ColorItem(Colors.Yellow, "Yellow"));
            standardColors.Add(new ColorItem(Colors.Orange, "Orange"));
            standardColors.Add(new ColorItem(Colors.Purple, "Purple"));
            return standardColors;
        }

        private static ObservableCollection<ColorItem> CreateAvailableColors()
        {
            var standardColors = new ObservableCollection<ColorItem>();

            foreach (var item in ColorUtilities.KnownColors.OrderBy(c => c.Value.ToString()))
                if (item.Key != "Transparent")
                {
                    var colorItem = new ColorItem(item.Value, item.Key);
                    if (!standardColors.Contains(colorItem))
                        standardColors.Add(colorItem);
                }

            return standardColors;
        }

        #endregion //Methods
    }
}