using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     Says that something long is going on over what it wraps, how far it has come, and offers to
    ///     stop it.
    ///     <para>
    ///         Ported from the WPF editor, where it was the toolkit's BusyIndicator with a content
    ///         template of its own. Avalonia has no such control, so the notice is drawn here.
    ///     </para>
    /// </summary>
    public partial class BusyWithStopControl : UserControl
    {
        #region construction and destruction

        public BusyWithStopControl()
        {
            InitializeComponent();
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

        #region private functions

        private void StopButtonOnClick(object? sender, RoutedEventArgs e)
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
