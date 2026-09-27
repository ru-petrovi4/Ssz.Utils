using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     Choosing a font: its family, its face and its size, with a sample of what it will look like.
    ///     <para>
    ///         Ported from the WPF editor. There the faces a family was actually drawn in came from
    ///         FamilyTypeface; Avalonia does not say, so style, weight and stretch are each picked from
    ///         their own list. What the editor answers with is the same DsFont as before.
    ///     </para>
    /// </summary>
    public partial class DsFontEditorControl : UserControl
    {
        #region construction and destruction

        public DsFontEditorControl()
        {
            InitializeComponent();

            FontFamilyListBox.ItemsSource = FontManager.Current.SystemFonts
                .OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();

            FontStyleListBox.ItemsSource = new[] { FontStyle.Normal, FontStyle.Italic, FontStyle.Oblique };
            FontWeightListBox.ItemsSource = Enum.GetValues<FontWeight>().Distinct().ToArray();
            FontStretchListBox.ItemsSource = Enum.GetValues<FontStretch>().Distinct().ToArray();
        }

        #endregion

        #region public functions

        public DsFont? DsFont
        {
            get =>
                new()
                {
                    Family = FontFamilyListBox.SelectedItem as FontFamily,
                    Size = !String.IsNullOrWhiteSpace(FontSizeTextBox.Text) ? FontSizeTextBox.Text : null,
                    Style = FontStyleListBox.SelectedItem as FontStyle?,
                    Stretch = FontStretchListBox.SelectedItem as FontStretch?,
                    Weight = FontWeightListBox.SelectedItem as FontWeight?
                };
            set
            {
                DsFont selectedFont = value ?? new DsFont
                {
                    Family = new FontFamily(@"Arial"),
                    Size = @"12",
                    Style = FontStyle.Normal,
                    Stretch = FontStretch.Normal,
                    Weight = FontWeight.Normal
                };

                if (selectedFont.Family is not null)
                {
                    FontFamily? fontFamily = FontFamilyListBox.Items.OfType<FontFamily>()
                        .FirstOrDefault(f => StringHelper.CompareIgnoreCase(f.Name, selectedFont.Family.Name));
                    if (fontFamily is not null)
                    {
                        FontFamilyListBox.SelectedItem = fontFamily;
                        FontFamilyListBox.ScrollIntoView(fontFamily);
                    }
                }

                FontSizeTextBox.Text = selectedFont.Size;

                FontStyleListBox.SelectedItem = selectedFont.Style ?? FontStyle.Normal;
                FontWeightListBox.SelectedItem = selectedFont.Weight ?? FontWeight.Normal;
                FontStretchListBox.SelectedItem = selectedFont.Stretch ?? FontStretch.Normal;

                RefreshSample();
            }
        }

        #endregion

        #region private functions

        private void FontFamilyListBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            RefreshSample();
        }

        private void FaceListBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            RefreshSample();
        }

        private void FontSizeTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            var fontSize = new Any(FontSizeTextBox.Text).ValueAsDouble(false);
            if (fontSize > 0.0 && Math.Abs(FontSizeSlider.Value - fontSize) > 0.01)
                FontSizeSlider.Value = fontSize;

            RefreshSample();
        }

        private void FontSizeSliderOnValueChanged(object? sender,
            Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var fontSize = Math.Round(FontSizeSlider.Value, 1);
            var text = new Any(fontSize).ValueAsString(false);
            if (FontSizeTextBox.Text != text) FontSizeTextBox.Text = text;
        }

        /// <summary>
        ///     Shows the sample in the font as it stands, which is the whole point of the dialog.
        /// </summary>
        private void RefreshSample()
        {
            if (FontFamilyListBox.SelectedItem is FontFamily fontFamily)
                SampleTextTextBox.FontFamily = fontFamily;

            if (FontStyleListBox.SelectedItem is FontStyle fontStyle)
                SampleTextTextBox.FontStyle = fontStyle;

            if (FontWeightListBox.SelectedItem is FontWeight fontWeight)
                SampleTextTextBox.FontWeight = fontWeight;

            if (FontStretchListBox.SelectedItem is FontStretch fontStretch)
                SampleTextTextBox.FontStretch = fontStretch;

            var fontSize = new Any(FontSizeTextBox.Text).ValueAsDouble(false);
            if (fontSize > 0.0) SampleTextTextBox.FontSize = fontSize;
        }

        #endregion
    }
}
