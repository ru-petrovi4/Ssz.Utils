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
    public class ValuesControl : Control
    {
        #region public functions

        public static readonly AvaloniaProperty DataProperty =
            AvaloniaProperty.Register("Data", typeof(string), typeof(ValuesControl),
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
            ((ValuesControl)d).InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            var dataValues = CsvHelper.ParseCsvLine(",", Data);
            if (dataValues.Length < 2)
                return;
            
            bool isOnPlot = new Any(dataValues[0]).ValueAsBoolean(false);
            string caption = dataValues[1] ?? @"";

            if (isOnPlot)
            {
                FormattedText formattedText =
                            GetFormattedText(caption, Brushes.Red);                
                var p0 = new Point(-formattedText.Width / 2 - 1, -formattedText.Height - 2);
                var p1 = new Point(formattedText.Width / 2 + 1, -formattedText.Height - 2);
                var p2 = new Point(formattedText.Width / 2 + 1, -1);
                var p3 = new Point(-formattedText.Width / 2 - 1, -1);                
                var geometry = Geometry.Parse("M " + p0.ToString() + " L " + p1.ToString() +
                    " L " + p2.ToString() +
                    " L " + p3.ToString() +                    
                    " Z");
                dc.DrawGeometry(new Any("#FFF0EEEF").ValueAs<Brush>(false), new Pen(Brushes.Black, 0.5), geometry);

                dc.DrawText(formattedText, new Point(-formattedText.Width / 2, -formattedText.Height - 1));
            }

            dataValues = dataValues.Skip(2).ToArray();
            int count = dataValues.Length / 2;
            if (count == 0)
                return;
            double delta = ActualHeight / count;
            for (int i = 0; i < count; i++)
            {
                DrawValue(dc, isOnPlot, 6, delta / 2 + delta * i, dataValues[2 * i + 1], new Any(dataValues[2 * i]).ValueAs<Brush>(false));
            }

            base.OnRender(dc);
        }

        #endregion

        #region private functions

        private void DrawValue(DrawingContext dc, bool drawEllipses, double x, double y, string? text, Brush? brush)
        {
            FormattedText formattedText =
                            GetFormattedText(text, brush);
            if (drawEllipses)
            {
                var p0 = new Point(x, y - formattedText.Height / 2 - 1);
                var p1 = new Point(x + formattedText.Width, y - formattedText.Height / 2 - 1);                
                var p2 = new Point(x + formattedText.Width, y + formattedText.Height / 2 + 1);
                var p3 = new Point(x, y + formattedText.Height / 2 + 1);
                var p12 = new Point(x + formattedText.Width + 5, y);
                var p30 = new Point(x - 5, y);
                var geometry = Geometry.Parse("M " + p0.ToString() + " L " + p1.ToString() +
                    " Q " + p12.ToString() + " " + p2.ToString() +
                    " L " + p3.ToString() +
                    " Q " + p30.ToString() + " " + p0.ToString() +
                    " Z");
                dc.DrawGeometry(new Any("#FFF0EEEF").ValueAs<Brush>(false), new Pen(Brushes.Black, 0.5), geometry);
            }
            
            dc.DrawText(formattedText, new Point(x, y - formattedText.Height / 2));
        }        

        private FormattedText GetFormattedText(string? text, Brush? brush)
        {
            return new FormattedText(text ?? @"", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), 10, brush);
        }

        #endregion
    }
}
