using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     Says that something long is going on over what it wraps, how far it has come, and offers to
    ///     stop it.
    ///     <para>
    ///         Ported from the WPF editor, where it was the toolkit's BusyIndicator with a content
    ///         template of its own. Avalonia has no such control, so the notice is drawn here. It is a
    ///         content control rather than a user control, because what it wraps is given to it from
    ///         outside and a user control has no room for both.
    ///     </para>
    /// </summary>
    public class BusyWithStopControl : ContentControl
    {
        #region construction and destruction

        public BusyWithStopControl()
        {
            Template = new FuncControlTemplate<BusyWithStopControl>((control, scope) =>
            {
                var panel = new Panel();

                var contentPresenter = new ContentPresenter
                {
                    Name = @"PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = control[!ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = control[!ContentTemplateProperty]
                };
                contentPresenter.RegisterInNameScope(scope);
                panel.Children.Add(contentPresenter);

                // What is going on is over what it wraps, and dims it.
                var veil = new Rectangle
                {
                    Fill = Brushes.Blue,
                    Opacity = 0.05,
                    [!IsVisibleProperty] = control[!IsBusyProperty]
                };
                panel.Children.Add(veil);

                var text1 = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                    [!TextBlock.TextProperty] = control[!Text1Property]
                };
                var text2 = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    [!TextBlock.TextProperty] = control[!Text2Property]
                };
                var progressBar = new ProgressBar
                {
                    Height = 15,
                    Minimum = 0,
                    Maximum = 100,
                    [!RangeBase.ValueProperty] = control[!ProgressPercentProperty]
                };

                var inner = new StackPanel { Margin = new Thickness(5) };
                inner.Children.Add(text2);
                inner.Children.Add(progressBar);

                var stopButton = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Content = Res.StopButtonText
                };
                stopButton.Click += (sender, e) => control.Stopped?.Invoke(control, EventArgs.Empty);

                var notice = new StackPanel { Margin = new Thickness(5), MinWidth = 240 };
                notice.Children.Add(text1);
                notice.Children.Add(inner);
                notice.Children.Add(stopButton);

                var border = new Border
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Background = Brushes.White,
                    Child = notice,
                    [!IsVisibleProperty] = control[!IsBusyProperty]
                };
                panel.Children.Add(border);

                return panel;
            });
        }

        #endregion

        #region public functions

        public static readonly StyledProperty<bool> IsBusyProperty =
            AvaloniaProperty.Register<BusyWithStopControl, bool>(nameof(IsBusy));

        public bool IsBusy
        {
            get => GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public static readonly StyledProperty<string?> Text1Property =
            AvaloniaProperty.Register<BusyWithStopControl, string?>(nameof(Text1));

        public string? Text1
        {
            get => GetValue(Text1Property);
            set => SetValue(Text1Property, value);
        }

        public static readonly StyledProperty<string?> Text2Property =
            AvaloniaProperty.Register<BusyWithStopControl, string?>(nameof(Text2));

        public string? Text2
        {
            get => GetValue(Text2Property);
            set => SetValue(Text2Property, value);
        }

        public static readonly StyledProperty<double> ProgressPercentProperty =
            AvaloniaProperty.Register<BusyWithStopControl, double>(nameof(ProgressPercent));

        public double ProgressPercent
        {
            get => GetValue(ProgressPercentProperty);
            set => SetValue(ProgressPercentProperty, value);
        }

        public event EventHandler? Stopped;

        #endregion
    }
}
