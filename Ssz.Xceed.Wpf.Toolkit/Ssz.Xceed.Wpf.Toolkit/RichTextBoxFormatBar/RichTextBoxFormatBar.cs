/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Ssz.Xceed.Wpf.Toolkit.Core;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class RichTextBoxFormatBar : Control, IRichTextBoxFormatBar
    {
        #region Constructors

        static RichTextBoxFormatBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RichTextBoxFormatBar),
                new FrameworkPropertyMetadata(typeof(RichTextBoxFormatBar)));
        }

        #endregion //Constructors

        #region Properties

        public static double[] FontSizes
        {
            get
            {
                return new[]
                {
                    3.0, 4.0, 5.0, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.5,
                    10.0, 10.5, 11.0, 11.5, 12.0, 12.5, 13.0, 13.5, 14.0, 15.0,
                    16.0, 17.0, 18.0, 19.0, 20.0, 22.0, 24.0, 26.0, 28.0, 30.0,
                    32.0, 34.0, 36.0, 38.0, 40.0, 44.0, 48.0, 52.0, 56.0, 60.0, 64.0, 68.0, 72.0, 76.0,
                    80.0, 88.0, 96.0, 104.0, 112.0, 120.0, 128.0, 136.0, 144.0
                };
            }
        }

        #endregion

        #region Members

        private ComboBox _cmbFontFamilies;
        private ComboBox _cmbFontSizes;
        private ColorPicker _cmbFontBackgroundColor;
        private ColorPicker _cmbFontColor;

        private ToggleButton _btnNumbers;
        private ToggleButton _btnBullets;
        private ToggleButton _btnBold;
        private ToggleButton _btnItalic;
        private ToggleButton _btnUnderline;
        private ToggleButton _btnAlignLeft;
        private ToggleButton _btnAlignCenter;
        private ToggleButton _btnAlignRight;

        private Thumb _dragWidget;
        private bool _waitingForMouseOver;

        #endregion

        #region Event Hanlders

        private void FontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            var editValue = (FontFamily) e.AddedItems[0];
            ApplyPropertyValueToSelectedText(TextElement.FontFamilyProperty, editValue);
            _waitingForMouseOver = true;
        }

        private void FontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            ApplyPropertyValueToSelectedText(TextElement.FontSizeProperty, e.AddedItems[0]);
            _waitingForMouseOver = true;
        }

        private void FontColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            var selectedColor = e.NewValue;
            ApplyPropertyValueToSelectedText(TextElement.ForegroundProperty, new SolidColorBrush(selectedColor));
            _waitingForMouseOver = true;
        }

        private void FontBackgroundColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            var selectedColor = e.NewValue;
            ApplyPropertyValueToSelectedText(TextElement.BackgroundProperty, new SolidColorBrush(selectedColor));
            _waitingForMouseOver = true;
        }

        private void Bullets_Clicked(object sender, RoutedEventArgs e)
        {
            if (BothSelectionListsAreChecked()) _btnNumbers.IsChecked = false;
        }

        private void Numbers_Clicked(object sender, RoutedEventArgs e)
        {
            if (BothSelectionListsAreChecked()) _btnBullets.IsChecked = false;
        }

        private void DragWidget_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ProcessMove(e);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            _waitingForMouseOver = false;
        }

        #endregion //Event Hanlders

        #region Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_dragWidget is not null) _dragWidget.DragDelta -= DragWidget_DragDelta;

            if (_cmbFontFamilies is not null) _cmbFontFamilies.SelectionChanged -= FontFamily_SelectionChanged;

            if (_cmbFontSizes is not null) _cmbFontSizes.SelectionChanged -= FontSize_SelectionChanged;

            if (_btnBullets is not null) _btnBullets.Click -= Bullets_Clicked;

            if (_btnNumbers is not null) _btnNumbers.Click -= Numbers_Clicked;

            if (_cmbFontBackgroundColor is not null)
                _cmbFontBackgroundColor.SelectedColorChanged -= FontBackgroundColor_SelectedColorChanged;

            if (_cmbFontColor is not null) _cmbFontColor.SelectedColorChanged -= FontColor_SelectedColorChanged;

            GetTemplateComponent(ref _cmbFontFamilies, "_cmbFontFamilies");
            GetTemplateComponent(ref _cmbFontSizes, "_cmbFontSizes");
            GetTemplateComponent(ref _cmbFontBackgroundColor, "_cmbFontBackgroundColor");
            GetTemplateComponent(ref _cmbFontColor, "_cmbFontColor");
            GetTemplateComponent(ref _btnNumbers, "_btnNumbers");
            GetTemplateComponent(ref _btnBullets, "_btnBullets");
            GetTemplateComponent(ref _btnBold, "_btnBold");
            GetTemplateComponent(ref _btnItalic, "_btnItalic");
            GetTemplateComponent(ref _btnUnderline, "_btnUnderline");
            GetTemplateComponent(ref _btnAlignLeft, "_btnAlignLeft");
            GetTemplateComponent(ref _btnAlignCenter, "_btnAlignCenter");
            GetTemplateComponent(ref _btnAlignRight, "_btnAlignRight");
            GetTemplateComponent(ref _dragWidget, "_dragWidget");

            if (_dragWidget is not null) _dragWidget.DragDelta += DragWidget_DragDelta;

            if (_cmbFontFamilies is not null)
            {
                _cmbFontFamilies.ItemsSource = FontUtilities.Families.OrderBy(fontFamily => fontFamily.Source);
                _cmbFontFamilies.SelectionChanged += FontFamily_SelectionChanged;
            }

            if (_cmbFontSizes is not null)
            {
                _cmbFontSizes.ItemsSource = FontSizes;
                _cmbFontSizes.SelectionChanged += FontSize_SelectionChanged;
            }

            if (_btnBullets is not null) _btnBullets.Click += Bullets_Clicked;

            if (_btnNumbers is not null) _btnNumbers.Click += Numbers_Clicked;

            if (_cmbFontBackgroundColor is not null)
                _cmbFontBackgroundColor.SelectedColorChanged += FontBackgroundColor_SelectedColorChanged;

            if (_cmbFontColor is not null) _cmbFontColor.SelectedColorChanged += FontColor_SelectedColorChanged;

            // Update the ComboBoxes when changing themes.
            Update();
        }

        private void GetTemplateComponent<T>(ref T partMember, string partName) where T : class
        {
            partMember = Template is not null
                ? Template.FindName(partName, this) as T
                : null;
        }

        private void UpdateToggleButtonState()
        {
            UpdateItemCheckedState(_btnBold, TextElement.FontWeightProperty, FontWeights.Bold);
            UpdateItemCheckedState(_btnItalic, TextElement.FontStyleProperty, FontStyles.Italic);
            UpdateItemCheckedState(_btnUnderline, Inline.TextDecorationsProperty, TextDecorations.Underline);

            UpdateItemCheckedState(_btnAlignLeft, Block.TextAlignmentProperty, TextAlignment.Left);
            UpdateItemCheckedState(_btnAlignCenter, Block.TextAlignmentProperty, TextAlignment.Center);
            UpdateItemCheckedState(_btnAlignRight, Block.TextAlignmentProperty, TextAlignment.Right);
        }

        private void UpdateItemCheckedState(ToggleButton button, DependencyProperty formattingProperty,
            object expectedValue)
        {
            var currentValue = DependencyProperty.UnsetValue;
            if (Target is not null && Target.Selection is not null)
                currentValue = Target.Selection.GetPropertyValue(formattingProperty);
            button.IsChecked = currentValue is null || currentValue == DependencyProperty.UnsetValue
                ? false
                : currentValue is not null && currentValue.Equals(expectedValue);
        }

        private void UpdateSelectedFontFamily()
        {
            var value = DependencyProperty.UnsetValue;
            if (Target is not null && Target.Selection is not null)
                value = Target.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
            var currentFontFamily =
                (FontFamily) (value is null || value == DependencyProperty.UnsetValue ? null : value);
            if (currentFontFamily is not null) _cmbFontFamilies.SelectedItem = currentFontFamily;
        }

        private void UpdateSelectedFontSize()
        {
            var value = DependencyProperty.UnsetValue;
            if (Target is not null && Target.Selection is not null)
                value = Target.Selection.GetPropertyValue(TextElement.FontSizeProperty);

            _cmbFontSizes.SelectedValue = value is null || value == DependencyProperty.UnsetValue ? null : value;
        }

        private void UpdateFontColor()
        {
            var value = DependencyProperty.UnsetValue;
            if (Target is not null && Target.Selection is not null)
                value = Target.Selection.GetPropertyValue(TextElement.ForegroundProperty);

            var currentColor = value is null || value == DependencyProperty.UnsetValue
                ? Colors.Black
                : ((SolidColorBrush) value).Color;
            _cmbFontColor.SelectedColor = currentColor;
        }

        private void UpdateFontBackgroundColor()
        {
            var value = DependencyProperty.UnsetValue;
            if (Target is not null && Target.Selection is not null)
                value = Target.Selection.GetPropertyValue(TextElement.BackgroundProperty);
            var currentColor = value is null || value == DependencyProperty.UnsetValue
                ? Colors.Transparent
                : ((SolidColorBrush) value).Color;
            _cmbFontBackgroundColor.SelectedColor = currentColor;
        }

        /// <summary>
        ///     Updates the visual state of the List styles, such as Numbers and Bullets.
        /// </summary>
        private void UpdateSelectionListType()
        {
            //uncheck both
            _btnBullets.IsChecked = false;
            _btnNumbers.IsChecked = false;

            var startParagraph = Target is not null && Target.Selection is not null
                ? Target.Selection.Start.Paragraph
                : null;
            var endParagraph = Target is not null && Target.Selection is not null
                ? Target.Selection.End.Paragraph
                : null;
            if (startParagraph is not null && endParagraph is not null && startParagraph.Parent is ListItem &&
                endParagraph.Parent is ListItem && ReferenceEquals(((ListItem) startParagraph.Parent).List,
                    ((ListItem) endParagraph.Parent).List))
            {
                var markerStyle = ((ListItem) startParagraph.Parent).List.MarkerStyle;
                if (markerStyle == TextMarkerStyle.Disc) //bullets
                    _btnBullets.IsChecked = true;
                else if (markerStyle == TextMarkerStyle.Decimal) //numbers
                    _btnNumbers.IsChecked = true;
            }
        }

        /// <summary>
        ///     Checks to see if both selection lists are checked. (Bullets and Numbers)
        /// </summary>
        /// <returns></returns>
        private bool BothSelectionListsAreChecked()
        {
            return _btnBullets.IsChecked == true && _btnNumbers.IsChecked == true;
        }

        private void ApplyPropertyValueToSelectedText(DependencyProperty formattingProperty, object value)
        {
            if (value is null || Target is null || Target.Selection is null)
                return;

            Target.Selection.ApplyPropertyValue(formattingProperty, value);
        }

        private void ProcessMove(DragDeltaEventArgs e)
        {
            var layer = AdornerLayer.GetAdornerLayer(Target);
            var adorner = layer.GetAdorners(Target)[0] as UIElementAdorner<Control>;
            adorner.SetOffsets(adorner.OffsetLeft + e.HorizontalChange, adorner.OffsetTop + e.VerticalChange);
        }

        #endregion //Methods

        #region IRichTextBoxFormatBar Interface

        #region Target

        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target",
            typeof(System.Windows.Controls.RichTextBox), typeof(RichTextBoxFormatBar),
            new PropertyMetadata(null, OnRichTextBoxPropertyChanged));

        public System.Windows.Controls.RichTextBox Target
        {
            get => (System.Windows.Controls.RichTextBox) GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        private static void OnRichTextBoxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var formatBar = d as RichTextBoxFormatBar;
        }

        #endregion //Target

        public bool PreventDisplayFadeOut =>
            _cmbFontFamilies.IsDropDownOpen || _cmbFontSizes.IsDropDownOpen ||
            _cmbFontBackgroundColor.IsOpen || _cmbFontColor.IsOpen || _waitingForMouseOver;

        public void Update()
        {
            UpdateToggleButtonState();
            UpdateSelectedFontFamily();
            UpdateSelectedFontSize();
            UpdateFontColor();
            UpdateFontBackgroundColor();
            UpdateSelectionListType();
        }

        #endregion
    }
}