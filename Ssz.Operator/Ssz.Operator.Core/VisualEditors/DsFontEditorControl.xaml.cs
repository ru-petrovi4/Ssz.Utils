using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace Ssz.Operator.Core.VisualEditors
{
    public partial class DsFontEditorControl : UserControl
    {
        #region construction and destruction

        public DsFontEditorControl()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public DsFont? DsFont
        {
            get =>
                new()
                {
                    Family = SampleTextTextBox.FontFamily,
                    Size = !string.IsNullOrWhiteSpace(FontSizeTextBox.Text) ? FontSizeTextBox.Text : null,
                    Style = SampleTextTextBox.FontStyle,
                    Stretch = SampleTextTextBox.FontStretch,
                    Weight = SampleTextTextBox.FontWeight
                };
            set
            {
                var selectedFont = value;
                if (selectedFont is null)
                    selectedFont = new DsFont
                    {
                        Family = new FontFamily("Arial"),
                        Size = @"12",
                        Style = FontStyles.Normal,
                        Stretch = FontStretches.Normal,
                        Weight = FontWeights.Normal
                    };
                if (selectedFont.Family is not null)
                {
                    string fontFamilyName = selectedFont.Family.Source;
                    var idx = 0;
                    foreach (object item in FontFamilyListBox.Items)
                    {
                        var itemName = item.ToString();
                        if (fontFamilyName == itemName) break;
                        idx += 1;
                    }

                    if (idx < FontFamilyListBox.Items.Count)
                    {
                        FontFamilyListBox.SelectedIndex = idx;
                        FontFamilyListBox.ScrollIntoView(FontFamilyListBox.Items[idx]);
                    }
                }

                FontSizeTextBox.Text = selectedFont.Size;

                var i = 0;
                foreach (var familyTypeface in FamilyTypefacesListBox.Items.OfType<FamilyTypeface>())
                {
                    if (selectedFont.Stretch == familyTypeface.Stretch &&
                        selectedFont.Weight == familyTypeface.Weight &&
                        selectedFont.Style == familyTypeface.Style)
                        break;
                    i += 1;
                }

                if (i < FamilyTypefacesListBox.Items.Count) FamilyTypefacesListBox.SelectedIndex = i;
            }
        }

        #endregion

        #region private functions

        private void FontSizeTextBoxOnPreviewMouseUp(object? sender, MouseButtonEventArgs e)
        {
            FontSizeTextBox.SelectAll();
        }

        #endregion
    }
}