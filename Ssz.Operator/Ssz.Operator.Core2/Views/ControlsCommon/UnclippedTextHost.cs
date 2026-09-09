using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsCommon
{
    /// <summary>
    ///     The values WPF used for a drawing element that does not specify them itself.
    /// </summary>
    public static class WpfTextDefaults
    {
        /// <summary>
        ///     WPF's TextBlock defaults to 12. Avalonia's Fluent theme defaults to 14, which is 17%
        ///     larger and wraps captions that used to fit on one line.
        /// </summary>
        public const double FontSize = 12.0;

        /// <summary>
        ///     WPF fell back to the system UI font. Avalonia falls back to the family the app
        ///     registered - Inter here - whose composite Cyrillic fallback is about 8% wider, so a
        ///     caption that used to fit its shape wraps. The list keeps the WPF metrics on Windows
        ///     and the app default everywhere else.
        /// </summary>
        public static readonly FontFamily FontFamily = FontFamily.Parse(@"Segoe UI, $Default");
    }

    /// <summary>
    ///     Hosts a TextBlock whose shape is a little too short for the wrapped text.
    ///     WPF laid out every line and let the drawing clip whatever stuck out. Avalonia's TextBlock
    ///     instead drops the lines that do not fit the available height, so the tail of a caption
    ///     disappears - a caption reading "АГРЕГАТ ТА1 X101.1, S102.1, TK102.1" in the WPF build
    ///     silently loses "TK102.1". Measuring and arranging the child with the height it actually
    ///     wants restores the WPF behaviour; the overflow is centred, as WPF centred it.
    /// </summary>
    public class UnclippedTextHost : Decorator
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Child is null)
                return default;

            Child.Measure(new Size(availableSize.Width, Double.PositiveInfinity));
            return Child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child is null)
                return finalSize;

            var height = Math.Max(finalSize.Height, Child.DesiredSize.Height);
            Child.Arrange(new Rect(0, (finalSize.Height - height) / 2, finalSize.Width, height));
            return finalSize;
        }
    }
}
