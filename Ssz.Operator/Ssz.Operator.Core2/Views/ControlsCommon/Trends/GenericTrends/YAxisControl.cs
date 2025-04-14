using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    public class YAxisControl : TemplatedControl
    {
        #region public functions

        public static readonly AvaloniaProperty DataProperty =
            AvaloniaProperty.Register<YAxisControl, string>(nameof(Data));

        public string Data
        {
            get { return (string)GetValue(DataProperty)!; }
            set { SetValue(DataProperty, value); }
        }

        public override void Render(DrawingContext dc)
        {
            var dataValues = CsvHelper.ParseCsvLine(",", Data);
            int count = dataValues.Length / 4;
            if (count == 0)
                return;
            FormattedText formattedText =
                            GetFormattedText("0", Brushes.Black);
            double delta = formattedText.Height + 2;
            for (int i = 0; i < count; i++)
            {
                var brush = new SolidColorBrush(Color.Parse(dataValues[4 * i] ?? @""));
                formattedText =
                            GetFormattedText(dataValues[4 * i + 3], brush);
                dc.DrawText(formattedText, new Point(-formattedText.Width - 1, 2 + delta * i));

                formattedText =
                            GetFormattedText(dataValues[4 * i + 2], brush);
                double start = Bounds.Height / 2 - delta * count / 2;
                dc.DrawText(formattedText, new Point(-formattedText.Width - 1, start + delta * i));

                formattedText =
                            GetFormattedText(dataValues[4 * i + 1], brush);
                dc.DrawText(formattedText, new Point(-formattedText.Width - 1, Bounds.Height - delta * count - 2 + delta * i));
            }

            base.Render(dc);
        }

        #endregion

        #region protected functions

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == DataProperty)
                InvalidateVisual();
        }        

        #endregion

        #region private functions        

        private FormattedText GetFormattedText(string? text, IBrush? brush)
        {
            return new FormattedText(text ?? @"", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), 10, brush);
        }

        #endregion
    }
}
