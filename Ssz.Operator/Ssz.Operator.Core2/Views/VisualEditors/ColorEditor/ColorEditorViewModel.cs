using System;
using Avalonia.Media;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.VisualEditors.ColorEditor
{
    /// <summary>
    ///     One colour being picked, and the two palettes it can be picked from.
    ///     <para>
    ///         Ported from the WPF editor, palette for palette. Picking from a palette only sets the
    ///         colour: the palettes themselves never show a selection, which is why they answer null.
    ///     </para>
    /// </summary>
    public class ColorEditorViewModel : ViewModelBase
    {
        #region construction and destruction

        public ColorEditorViewModel()
        {
            AvailableColors = new[]
            {
                new ColorItem(Colors.Transparent),
                new ColorItem(Colors.White),
                new ColorItem(Colors.Red),
                new ColorItem(Colors.Orange),
                new ColorItem(Colors.Yellow),
                new ColorItem(Colors.Lime),
                new ColorItem(Colors.Cyan),
                new ColorItem(Colors.Blue),
                new ColorItem(Colors.Magenta),
                new ColorItem(Colors.Black),
                new ColorItem(Colors.Transparent),
                new ColorItem(Colors.White),
                new ColorItem(Colors.LightCoral),
                new ColorItem(Colors.LightSalmon),
                new ColorItem(Colors.LightYellow),
                new ColorItem(Colors.Green),
                new ColorItem(Colors.Cyan),
                new ColorItem(Colors.LightSkyBlue),
                new ColorItem(Colors.Violet),
                new ColorItem(Colors.LightGray)
            };

            StandardColors = new[]
            {
                new ColorItem(Colors.Transparent),
                new ColorItem(Colors.White),
                new ColorItem(Colors.Red),
                new ColorItem(Colors.Orange),
                new ColorItem(Colors.Yellow),
                new ColorItem(Colors.Green),
                new ColorItem(Colors.Cyan),
                new ColorItem(Colors.Blue),
                new ColorItem(Colors.Violet),
                new ColorItem(Colors.Black)
            };
        }

        #endregion

        #region public functions

        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (SetProperty(ref _selectedColor, value)) SelectedColorChanged?.Invoke();
            }
        }

        public ColorItem? SelectedAvailableColors
        {
            get => null;
            set => SelectedColor = value?.Color ?? default;
        }

        public ColorItem? SelectedStandardColors
        {
            get => null;
            set => SelectedColor = value?.Color ?? default;
        }

        public ColorItem[] AvailableColors { get; }

        public ColorItem[] StandardColors { get; }

        public event Action? SelectedColorChanged;

        #endregion

        #region private fields

        private Color _selectedColor;

        #endregion
    }

    /// <summary>
    ///     One swatch of a palette.
    /// </summary>
    public class ColorItem
    {
        #region construction and destruction

        public ColorItem(Color color)
        {
            Color = color;
            Brush = new SolidColorBrush(color);
            Name = color.ToString();
        }

        #endregion

        #region public functions

        public Color Color { get; }

        public IBrush Brush { get; }

        public string Name { get; }

        #endregion
    }
}
