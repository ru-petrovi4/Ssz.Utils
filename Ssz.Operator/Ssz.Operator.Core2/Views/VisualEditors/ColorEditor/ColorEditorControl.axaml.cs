using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors.ColorEditor
{
    /// <summary>
    ///     Picking one colour: from a palette, from the colour wheel, or by naming a constant of the
    ///     project.
    ///     <para>
    ///         Ported from the WPF editor. What is gone is the eyedropper that picked a colour from
    ///         anywhere on screen: it worked by swapping the cursors of Windows in the registry and
    ///         grabbing the screen, neither of which a cross platform editor can do, and neither of
    ///         which a browser allows at all.
    ///     </para>
    /// </summary>
    public partial class ColorEditorControl : UserControl
    {
        #region construction and destruction

        public ColorEditorControl()
        {
            InitializeComponent();

            _colorEditorViewModel = new ColorEditorViewModel();

            MainStackPanel.DataContext = _colorEditorViewModel;

            _colorEditorViewModel.SelectedColorChanged += OnSelectedColorChanged;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The chequerboard the palettes show their swatches over.
        /// </summary>
        public static IBrush CheckerBrush => BrushAndNameControl.CheckerBrush;

        public static readonly StyledProperty<Color> SelectedColorProperty =
            AvaloniaProperty.Register<ColorEditorControl, Color>(nameof(SelectedColor),
                defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public Color SelectedColor
        {
            get => GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        /// <summary>
        ///     The colour as the project stores it: either a colour, or the constant that will name one
        ///     when the project runs.
        /// </summary>
        public string? SelectedColorString
        {
            get
            {
                var constant = ConstantTextBox.Text ?? @"";
                if (ConstantsHelper.ContainsQuery(constant)) return constant;

                return ObsoleteAnyHelper.ConvertTo<string>(_colorEditorViewModel.SelectedColor, false);
            }
            set
            {
                if (ConstantsHelper.ContainsQuery(value))
                {
                    _colorEditorViewModel.SelectedColor = default;
                    ConstantTextBox.Text = value;
                }
                else
                {
                    if (String.IsNullOrWhiteSpace(value))
                    {
                        _colorEditorViewModel.SelectedColor = Colors.White;
                    }
                    else
                    {
                        var color = ObsoleteAnyHelper.ConvertTo<Color>(value, false);
                        _colorEditorViewModel.SelectedColor = color == default ? Colors.White : color;
                    }

                    ConstantTextBox.Text = @"";
                }
            }
        }

        public void HideConstants()
        {
            ConstantTextBlock.IsVisible = false;
            ConstantTextBox.IsVisible = false;
        }

        #endregion

        #region protected functions

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectedColorProperty)
                _colorEditorViewModel.SelectedColor = change.GetNewValue<Color>();
        }

        #endregion

        #region private functions

        private void ConstantTextBoxOnKeyUp(object? sender, KeyEventArgs e)
        {
            if ((ConstantTextBox.Text ?? @"").Length > 0) _colorEditorViewModel.SelectedColor = default;
        }

        private void OnSelectedColorChanged()
        {
            if (_colorEditorViewModel.SelectedColor != default) ConstantTextBox.Text = @"";

            SelectedColor = _colorEditorViewModel.SelectedColor;
        }

        #endregion

        #region private fields

        private readonly ColorEditorViewModel _colorEditorViewModel;

        #endregion
    }
}
