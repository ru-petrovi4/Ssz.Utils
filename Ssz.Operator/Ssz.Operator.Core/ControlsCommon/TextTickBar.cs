using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.ControlsCommon
{
    public class TextTickBar : TickBar
    {
        #region public functions

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(TextTickBar),
                new PropertyMetadata(new FontFamily("Courier New"), OnPropertyChanged));

        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(TextTickBar),
                new PropertyMetadata(FontStyles.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(TextTickBar),
                new PropertyMetadata(FontWeights.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontStretchProperty =
            DependencyProperty.Register("FontStretch", typeof(FontStretch), typeof(TextTickBar),
                new PropertyMetadata(FontStretches.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(double), typeof(TextTickBar),
                new PropertyMetadata(14.0, OnPropertyChanged));

        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register("Format", typeof(string), typeof(TextTickBar),
                new PropertyMetadata(@"F02", OnPropertyChanged));

        public static readonly DependencyProperty EngUnitProperty =
            DependencyProperty.Register("EngUnit", typeof(string), typeof(TextTickBar),
                new PropertyMetadata(@"", OnPropertyChanged));

        public static readonly DependencyProperty TextTickFrequencyProperty =
            DependencyProperty.Register("TextTickFrequency", typeof(double), typeof(TextTickBar),
                new PropertyMetadata(0.2, OnPropertyChanged));

        public static readonly DependencyProperty TickStrokeThicknessProperty =
            DependencyProperty.Register("TickStrokeThickness", typeof(double), typeof(TextTickBar),
                new PropertyMetadata(1.0, OnPropertyChanged));

        public static readonly DependencyProperty TickLengthProperty =
            DependencyProperty.Register("TickLength", typeof(double), typeof(TextTickBar),
                new PropertyMetadata(10.0, OnPropertyChanged));

        public FontFamily FontFamily
        {
            get => (FontFamily) GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public FontStyle FontStyle
        {
            get => (FontStyle) GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public FontWeight FontWeight
        {
            get => (FontWeight) GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public FontStretch FontStretch
        {
            get => (FontStretch) GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        public double FontSize
        {
            get => (double) GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public string Format
        {
            get => (string) GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        public string EngUnit
        {
            get => (string) GetValue(EngUnitProperty);
            set => SetValue(EngUnitProperty, value);
        }

        public double TextTickFrequency
        {
            get => (double) GetValue(TextTickFrequencyProperty);
            set => SetValue(TextTickFrequencyProperty, value);
        }

        public double TickStrokeThickness
        {
            get => (double) GetValue(TickStrokeThicknessProperty);
            set => SetValue(TickStrokeThicknessProperty, value);
        }

        public double TickLength
        {
            get => (double) GetValue(TickLengthProperty);
            set => SetValue(TickLengthProperty, value);
        }

        public string GetValueWithEngUnit(double value)
        {
            return ObsoleteAnyHelper.ConvertTo<string>(value, true, Format) + @" " + EngUnit;
        }

        #endregion

        #region protected functions

        protected static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TextTickBar) d).InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            var delta = Maximum - Minimum; //100-0 = 100

            if (delta < double.Epsilon * 1000) return;

            if (Placement == TickBarPlacement.Left)
            {
                var textTickDelta = ActualHeight * TextTickFrequency / delta; //100/10=10
                if (textTickDelta > 1)
                {
                    var v = Minimum;
                    for (var y = ActualHeight; y > -1; y -= textTickDelta)
                    {
                        DrawTick(dc, -TickLength, y, 0, y);

                        FormattedText formattedText =
                            GetFormattedText(GetValueWithEngUnit(v));
                        dc.DrawText(formattedText,
                            new Point(-formattedText.Width - 1.5 * TickLength, y - formattedText.Height / 2));

                        v += TextTickFrequency;
                    }
                }

                var tickDelta = ActualHeight * TickFrequency / delta; //100/10=10
                if (tickDelta > 1)
                    for (var y = ActualHeight; y > -1; y -= tickDelta)
                        DrawTick(dc, -TickLength / 2, y, 0, y);

                return;
            }

            if (Placement == TickBarPlacement.Right)
            {
                var textTickDelta = ActualHeight * TextTickFrequency / delta; //100/10=10
                if (textTickDelta > 1)
                {
                    var v = Minimum;
                    for (var y = ActualHeight; y > -1; y -= textTickDelta)
                    {
                        DrawTick(dc, 0, y, TickLength, y);

                        FormattedText formattedText =
                            GetFormattedText(GetValueWithEngUnit(v));
                        dc.DrawText(formattedText, new Point(1.5 * TickLength, y - formattedText.Height / 2));

                        v += TextTickFrequency;
                    }
                }

                var tickDelta = ActualHeight * TickFrequency / delta; //100/10=10
                if (tickDelta > 1)
                    for (var y = ActualHeight; y > -1; y -= tickDelta)
                        DrawTick(dc, 0, y, TickLength / 2, y);

                return;
            }

            if (Placement == TickBarPlacement.Top)
            {
                var textTickDelta = ActualWidth * TextTickFrequency / delta; //100/10=10
                if (textTickDelta > 1)
                {
                    var v = Minimum;
                    for (double x = 0; x < ActualWidth + 1; x += textTickDelta)
                    {
                        DrawTick(dc, x, -TickLength, x, 0);

                        FormattedText formattedText =
                            GetFormattedText(GetValueWithEngUnit(v));
                        dc.DrawText(formattedText,
                            new Point(x - formattedText.Width / 2, -formattedText.Height - 1.5 * TickLength));

                        v += TextTickFrequency;
                    }
                }

                var tickDelta = ActualWidth * TickFrequency / delta; //100/10=10
                if (tickDelta > 1)
                    for (double x = 0; x < ActualWidth + 1; x += tickDelta)
                        DrawTick(dc, x, -TickLength / 2, x, 0);

                return;
            }

            if (Placement == TickBarPlacement.Bottom)
            {
                var textTickDelta = ActualWidth * TextTickFrequency / delta; //100/10=10
                if (textTickDelta > 1)
                {
                    var v = Minimum;
                    for (double x = 0; x < ActualWidth + 1; x += textTickDelta)
                    {
                        DrawTick(dc, x, 0, x, TickLength);

                        FormattedText formattedText =
                            GetFormattedText(GetValueWithEngUnit(v));
                        dc.DrawText(formattedText, new Point(x - formattedText.Width / 2, 1.5 * TickLength));

                        v += TextTickFrequency;
                    }
                }

                var tickDelta = ActualWidth * TickFrequency / delta; //100/10=10
                if (tickDelta > 1)
                    for (double x = 0; x < ActualWidth + 1; x += tickDelta)
                        DrawTick(dc, x, 0, x, TickLength / 2);

                return;
            }

            base.OnRender(dc);
        }

        #endregion

        #region private functions

        private void DrawTick(DrawingContext dc, double x1, double y1, double x2, double y2)
        {
            var tickStrokeThickness = TickStrokeThickness;
            var halfPenWidth = tickStrokeThickness / 2;

            // Create a guidelines set
            GuidelineSet guidelines = new();
            guidelines.GuidelinesX.Add(x1 - halfPenWidth);
            guidelines.GuidelinesX.Add(x2 + halfPenWidth);
            guidelines.GuidelinesY.Add(y1 - halfPenWidth);
            guidelines.GuidelinesY.Add(y2 + halfPenWidth);

            dc.PushGuidelineSet(guidelines);
            dc.DrawGeometry(null, new Pen(Fill, tickStrokeThickness),
                new LineGeometry(new Point(x1, y1), new Point(x2, y2)));
            dc.Pop();
        }

        private FormattedText GetFormattedText(string text)
        {
            return new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Fill,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        #endregion
    }
}

// if (tickStrokeThickness < 1.1) tickStrokeThickness = 1.1;