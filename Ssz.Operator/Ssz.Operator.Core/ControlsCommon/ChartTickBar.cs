using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.ControlsCommon
{
    public class ChartTickBar : TickBar
    {
        #region construction and destruction

        public ChartTickBar()
        {
            IsHitTestVisible = false;
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(ChartTickBar),
                new PropertyMetadata(new FontFamily("Courier New"), OnPropertyChanged));

        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(ChartTickBar),
                new PropertyMetadata(FontStyles.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(ChartTickBar),
                new PropertyMetadata(FontWeights.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontStretchProperty =
            DependencyProperty.Register("FontStretch", typeof(FontStretch), typeof(ChartTickBar),
                new PropertyMetadata(FontStretches.Normal, OnPropertyChanged));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(double), typeof(ChartTickBar),
                new PropertyMetadata(14.0, OnPropertyChanged));

        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register("Format", typeof(string), typeof(ChartTickBar),
                new PropertyMetadata(@"F02", OnPropertyChanged));

        public static readonly DependencyProperty EngUnitProperty =
            DependencyProperty.Register("EngUnit", typeof(string), typeof(ChartTickBar),
                new PropertyMetadata(@"", OnPropertyChanged));

        public static readonly DependencyProperty TickStrokeThicknessProperty =
            DependencyProperty.Register("TickStrokeThickness", typeof(double), typeof(ChartTickBar),
                new PropertyMetadata(1.0, OnPropertyChanged));

        public static readonly DependencyProperty LabelsBeginIsVisibleProperty =
            DependencyProperty.Register("LabelsBeginIsVisible", typeof(bool), typeof(ChartTickBar),
                new PropertyMetadata(false, OnPropertyChanged));

        public static readonly DependencyProperty LabelsEndIsVisibleProperty =
            DependencyProperty.Register("LabelsEndIsVisible", typeof(bool), typeof(ChartTickBar),
                new PropertyMetadata(false, OnPropertyChanged));

        public static readonly DependencyProperty LabelsFillProperty =
            DependencyProperty.Register("LabelsFill", typeof(Brush), typeof(ChartTickBar),
                new PropertyMetadata(null, OnPropertyChanged));

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

        public double TickStrokeThickness
        {
            get => (double) GetValue(TickStrokeThicknessProperty);
            set => SetValue(TickStrokeThicknessProperty, value);
        }

        public bool LabelsBeginIsVisible
        {
            get => (bool) GetValue(LabelsBeginIsVisibleProperty);
            set => SetValue(LabelsBeginIsVisibleProperty, value);
        }

        public bool LabelsEndIsVisible
        {
            get => (bool) GetValue(LabelsEndIsVisibleProperty);
            set => SetValue(LabelsEndIsVisibleProperty, value);
        }

        public Brush LabelsFill
        {
            get => (Brush) GetValue(LabelsFillProperty);
            set => SetValue(LabelsFillProperty, value);
        }

        public string GetValueWithEngUnit(double value)
        {
            return ObsoleteAnyHelper.ConvertTo<string>(value, true, Format) + @" " + EngUnit;
        }

        #endregion

        #region protected functions

        protected static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ChartTickBar) d).InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            var delta = Maximum - Minimum; //100-0 = 100

            if (delta < double.Epsilon * 1000) return;

            var m = PresentationSource.FromVisual(this)
                .CompositionTarget.TransformToDevice;
            var dpiFactor = 1 / m.M11;

            if (Placement == TickBarPlacement.Left || Placement == TickBarPlacement.Right)
            {
                var tickDelta = ActualHeight * TickFrequency / delta; //100/10=10
                if (tickDelta > 1)
                {
                    var v = Minimum;
                    for (var y = ActualHeight; y > -1; y -= tickDelta)
                    {
                        DrawTick(dc, dpiFactor, 0, y, ActualWidth, y);

                        if (LabelsBeginIsVisible || LabelsEndIsVisible)
                        {
                            FormattedText formattedText = GetFormattedText(GetValueWithEngUnit(v));
                            var margin = formattedText.Height * 0.2;
                            if (y - margin > 0 && y + margin + formattedText.Height < ActualHeight)
                            {
                                if (LabelsBeginIsVisible) dc.DrawText(formattedText, new Point(margin, y + margin));
                                if (LabelsEndIsVisible)
                                    dc.DrawText(formattedText,
                                        new Point(ActualWidth - margin - formattedText.Width, y + margin));
                            }

                            v += TickFrequency;
                        }
                    }
                }

                return;
            }

            if (Placement == TickBarPlacement.Bottom || Placement == TickBarPlacement.Top)
            {
                var tickDelta = ActualWidth * TickFrequency / delta; //100/10=10
                if (tickDelta > 1)
                {
                    var v = Minimum;
                    for (double x = 0; x < ActualWidth + 1; x += tickDelta)
                    {
                        DrawTick(dc, dpiFactor, x, 0, x, ActualHeight);

                        if (LabelsBeginIsVisible || LabelsEndIsVisible)
                        {
                            FormattedText formattedText = GetFormattedText(GetValueWithEngUnit(v));
                            var margin = formattedText.Height * 0.2;
                            if (x - margin > 0 && x + margin + formattedText.Width < ActualWidth)
                            {
                                if (LabelsBeginIsVisible) dc.DrawText(formattedText, new Point(x + margin, margin));
                                if (LabelsEndIsVisible)
                                    dc.DrawText(formattedText,
                                        new Point(x + margin, ActualHeight - margin - formattedText.Height));
                            }

                            v += TickFrequency;
                        }
                    }
                }

                return;
            }

            base.OnRender(dc);
        }

        #endregion

        #region private functions

        private void DrawTick(DrawingContext dc, double dpiFactor, double x1, double y1, double x2, double y2)
        {
            dc.DrawGeometry(null, new Pen(Fill, TickStrokeThickness),
                new LineGeometry(new Point(x1, y1), new Point(x2, y2)));
        }

        private FormattedText GetFormattedText(string text)
        {
            return new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, LabelsFill,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        #endregion
    }
}

//x1 = Math.Round(x1) + 0.5;
//x2 = Math.Round(x2) + 0.5;
//y1 = Math.Round(y1) + 0.5;
//y2 = Math.Round(y2) + 0.5;