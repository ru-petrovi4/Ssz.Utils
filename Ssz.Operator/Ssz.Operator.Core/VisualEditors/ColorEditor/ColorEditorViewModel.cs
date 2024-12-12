using System;
using System.Windows.Media;
using Ssz.Utils;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit;

namespace Ssz.Operator.Core.VisualEditors.ColorEditor
{
    internal class ColorEditorViewModel : ViewModelBase
    {
        #region private fields

        private Color _selectedColor;

        #endregion

        #region construction and destruction

        public ColorEditorViewModel()
        {
            AvailableColors = new[]
            {
                new ColorItem(Colors.Transparent, Colors.Transparent.ToString()),
                new ColorItem(Colors.White, Colors.White.ToString()),
                new ColorItem(Colors.Red, Colors.Red.ToString()),
                new ColorItem(Colors.Orange, Colors.Orange.ToString()),
                new ColorItem(Colors.Yellow, Colors.Yellow.ToString()),
                new ColorItem(Colors.Lime, Colors.Lime.ToString()),
                new ColorItem(Colors.Cyan, Colors.Cyan.ToString()),
                new ColorItem(Colors.Blue, Colors.Blue.ToString()),
                new ColorItem(Colors.Magenta, Colors.Magenta.ToString()),
                new ColorItem(Colors.Black, Colors.Black.ToString()),
                new ColorItem(Colors.Transparent, Colors.Transparent.ToString()),
                new ColorItem(Colors.White, Colors.White.ToString()),
                new ColorItem(Colors.LightCoral, Colors.LightCoral.ToString()),
                new ColorItem(Colors.LightSalmon, Colors.LightSalmon.ToString()),
                new ColorItem(Colors.LightYellow, Colors.LightYellow.ToString()),
                new ColorItem(Colors.Green, Colors.Green.ToString()),
                new ColorItem(Colors.Cyan, Colors.Cyan.ToString()),
                new ColorItem(Colors.LightSkyBlue, Colors.LightSkyBlue.ToString()),
                new ColorItem(Colors.Violet, Colors.Violet.ToString()),
                new ColorItem(Colors.LightGray, Colors.LightGray.ToString())
            };

            /*
            // ------------------ First Row -----------------------------
        internal static Color Black = Color.Black;
        internal static Color Brown = Color.FromArgb(153, 51, 0);
        internal static Color OliveGreen = Color.FromArgb(51, 51, 0);
        internal static Color DarkGreen = Color.FromArgb(0, 51, 0);

        internal static Color DarkTeal = Color.FromArgb(0, 51, 102);
        internal static Color DarkBlue = Color.FromArgb(0, 0, 128);
        internal static Color Indigo = Color.FromArgb(51, 51, 153);
        internal static Color Gray80 = Color.FromArgb(51, 51, 51);
        // ------------------ Second Row ----------------------------
        internal static Color DarkRed = Color.FromArgb(128, 0, 0);
        internal static Color Orange = Color.FromArgb(255, 102, 0);
        internal static Color DarkYellow = Color.FromArgb(128, 128, 0);
        internal static Color Green = Color.Green;

        internal static Color Teal = Color.Teal;
        internal static Color Blue = Color.Blue;
        internal static Color BlueGray = Color.FromArgb(102, 102, 153);
        internal static Color Gray50 = Color.FromArgb(128, 128, 128);
        // ------------------ Third Row -----------------------------       
        internal static Color Red = Color.Red;
        internal static Color LightOrange = Color.FromArgb(255, 153, 0);
        internal static Color Lime = Color.FromArgb(153, 204, 0);
        internal static Color SeaGreen = Color.FromArgb(51, 153, 102);

        internal static Color Aqua = Color.FromArgb(51, 204, 204);
        internal static Color LightBlue = Color.FromArgb(51, 102, 255);
        internal static Color Violet = Color.FromArgb(128, 0, 128);
        internal static Color Gray40 = Color.FromArgb(153, 153, 153);
        // ----------------- Forth Row ------------------------------
        internal static Color Pink = Color.FromArgb(255, 0, 255);
        internal static Color Gold = Color.FromArgb(255, 204, 0);
        internal static Color Yellow = Color.FromArgb(255, 255, 0);
        internal static Color BrightGreen = Color.FromArgb(0, 255, 0);

        internal static Color Turquoise = Color.FromArgb(0, 255, 255);
        internal static Color SkyBlue = Color.FromArgb(0, 204, 255);
        internal static Color Plum = Color.FromArgb(153, 51, 102);
        internal static Color Gray25 = Color.FromArgb(192, 192, 192);     
        // ----------------- Fifth Row ------------------------------
        internal static Color Rose = Color.FromArgb(255, 153, 204);
        internal static Color Tan = Color.FromArgb(255, 204, 153);
        internal static Color LightYellow = Color.FromArgb(255, 255, 153);
        internal static Color LightGreen = Color.FromArgb(204, 255, 204);

        internal static Color LightTurquoise = Color.FromArgb(204, 255, 255);
        internal static Color PaleBlue = Color.FromArgb(153, 204, 255);
        internal static Color Lavender = Color.FromArgb(204, 153, 255);
        internal static Color White = Color.White;
             */


            StandardColors = new[]
            {
                new ColorItem(Colors.Transparent, Colors.Transparent.ToString()),
                new ColorItem(Colors.White, Colors.White.ToString()),
                new ColorItem(Colors.Red, Colors.Red.ToString()),
                new ColorItem(Colors.Orange, Colors.Orange.ToString()),
                new ColorItem(Colors.Yellow, Colors.Yellow.ToString()),
                new ColorItem(Colors.Green, Colors.Green.ToString()),
                new ColorItem(Colors.Cyan, Colors.Cyan.ToString()),
                new ColorItem(Colors.Blue, Colors.Blue.ToString()),
                new ColorItem(Colors.Violet, Colors.Violet.ToString()),
                new ColorItem(Colors.Black, Colors.Black.ToString())
            };
        }

        #endregion

        #region public functions

        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (SetValue(ref _selectedColor, value))
                {
                    var selectedColorChanged = SelectedColorChanged;
                    if (selectedColorChanged is not null) selectedColorChanged();
                }
            }
        }

        public ColorItem? SelectedAvailableColors
        {
            get => null;
            set
            {
                if (value is null) SelectedColor = default;
                else SelectedColor = value.Color;
            }
        }

        public ColorItem? SelectedStandardColors
        {
            get => null;
            set
            {
                if (value is null) SelectedColor = default;
                else SelectedColor = value.Color;
            }
        }

        public ColorItem[] AvailableColors { get; }
        public ColorItem[] StandardColors { get; }

        public event Action? SelectedColorChanged;

        #endregion
    }
}