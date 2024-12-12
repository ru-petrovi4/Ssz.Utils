using System;
using System.Windows;
using Ssz.Xceed.Wpf.Toolkit;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class BusyWithStopControl : BusyIndicator
    {
        #region construction and destruction

        public BusyWithStopControl()
        {
            InitializeComponent();
        }

        #endregion

        #region private functions

        private void StopButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (Stopped is not null) Stopped(this, EventArgs.Empty);
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty Text1Property =
            DependencyProperty.Register("Text1", typeof(string), typeof(BusyWithStopControl));

        public static readonly DependencyProperty Text2Property =
            DependencyProperty.Register("Text2", typeof(string), typeof(BusyWithStopControl));

        public static readonly DependencyProperty ProgressPercentProperty =
            DependencyProperty.Register("ProgressPercent", typeof(double), typeof(BusyWithStopControl));

        public string Text1
        {
            get => (string) GetValue(Text1Property);
            set => SetValue(Text1Property, value);
        }

        public string Text2
        {
            get => (string) GetValue(Text2Property);
            set => SetValue(Text2Property, value);
        }

        public double ProgressPercent
        {
            get => (double) GetValue(ProgressPercentProperty);
            set => SetValue(ProgressPercentProperty, value);
        }

        public event EventHandler? Stopped;

        #endregion
    }
}