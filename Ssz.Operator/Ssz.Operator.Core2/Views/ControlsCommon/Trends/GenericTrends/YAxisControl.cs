using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    public class YAxisControl : Control
    {
        #region public functions

        public static readonly AvaloniaProperty DataProperty =
            AvaloniaProperty.Register("Data", typeof(string), typeof(YAxisControl),
                new PropertyMetadata(null, OnPropertyChanged));

        public string Data
        {
            get { return (string)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        #endregion

        #region protected functions

        protected static void OnPropertyChanged(DependencyObject d, AvaloniaPropertyChangedEventArgs e)
        {
            ((YAxisControl)d).InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
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
                var brush = new Any(dataValues[4 * i]).ValueAs<Brush>(false);
                formattedText =
                            GetFormattedText(dataValues[4 * i + 3], brush);
                dc.DrawText(formattedText, new Point(-formattedText.Width - 1, 2 + delta * i));

                formattedText =
                            GetFormattedText(dataValues[4 * i + 2], brush);
                double start = ActualHeight / 2 - delta * count / 2;
                dc.DrawText(formattedText, new Point(-formattedText.Width - 1, start + delta * i));

                formattedText =
                            GetFormattedText(dataValues[4 * i + 1], brush);
                dc.DrawText(formattedText, new Point(-formattedText.Width - 1, ActualHeight - delta * count - 2 + delta * i));
            }

            base.OnRender(dc);
        }

        #endregion

        #region private functions        

        private FormattedText GetFormattedText(string text, Brush brush)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), 10, brush);
        }

        #endregion
    }
}
