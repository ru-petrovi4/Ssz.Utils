using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    public class ClickOrTouchButton : Button
    {
        #region public functions

        public static readonly DependencyProperty IsClickedOrTouchedProperty = DependencyProperty.Register(
            "IsClickedOrTouched",
            typeof(bool),
            typeof(ClickOrTouchButton),
            new FrameworkPropertyMetadata(false));


        public static readonly DependencyProperty ClickOrTouchDownCommandProperty =
            DependencyProperty.Register("ClickOrTouchDownCommand",
                typeof(ICommand),
                typeof(ClickOrTouchButton),
                new FrameworkPropertyMetadata(null, OnPushDownCommandPropertyChanged));


        public static readonly DependencyProperty ClickOrTouchUpCommandProperty =
            DependencyProperty.Register("ClickOrTouchUpCommand",
                typeof(ICommand),
                typeof(ClickOrTouchButton),
                new FrameworkPropertyMetadata(null, OnPushUpCommandPropertyChanged));

        public bool IsClickedOrTouched
        {
            get => (bool) GetValue(IsClickedOrTouchedProperty);
            set => SetValue(IsClickedOrTouchedProperty, value);
        }

        public ICommand ClickOrTouchDownCommand
        {
            get => (ICommand) GetValue(ClickOrTouchDownCommandProperty);
            set => SetValue(ClickOrTouchDownCommandProperty, value);
        }

        public ICommand ClickOrTouchUpCommand
        {
            get => (ICommand) GetValue(ClickOrTouchUpCommandProperty);
            set => SetValue(ClickOrTouchUpCommandProperty, value);
        }

        #endregion

        #region protected functions

        protected override void OnIsPressedChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsPressedChanged(e);

            if (IsPressed)
            {
                if (_isClickedOrTouched) return;
                _isClickedOrTouched = true;
                IsClickedOrTouched = true;
                if (_pushDownCommand is not null) _pushDownCommand.Execute(CommandParameter);
            }
            else
            {
                if (!_isClickedOrTouched) return;
                _isClickedOrTouched = false;
                IsClickedOrTouched = false;
                if (_pushUpCommand is not null) _pushUpCommand.Execute(CommandParameter);
            }
        }

        protected override void OnTouchEnter(TouchEventArgs e)
        {
            base.OnTouchEnter(e);

            if (_isClickedOrTouched) return;
            _isClickedOrTouched = true;
            IsClickedOrTouched = true;
            if (_pushDownCommand is not null) _pushDownCommand.Execute(CommandParameter);
        }

        protected override void OnTouchLeave(TouchEventArgs e)
        {
            base.OnTouchLeave(e);

            if (!_isClickedOrTouched) return;
            _isClickedOrTouched = false;
            IsClickedOrTouched = false;
            if (_pushUpCommand is not null) _pushUpCommand.Execute(CommandParameter);
        }

        #endregion

        #region private functions

        private static void OnPushDownCommandPropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            var command = e.NewValue as ICommand;
            var button = sender as ClickOrTouchButton;
            if (command is null || button is null) return;

            button._pushDownCommand = command;
        }


        private static void OnPushUpCommandPropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            var command = e.NewValue as ICommand;
            var button = sender as ClickOrTouchButton;
            if (command is null || button is null) return;

            button._pushUpCommand = command;
        }

        #endregion

        #region private fields

        private bool _isClickedOrTouched;
        private ICommand? _pushDownCommand;
        private ICommand? _pushUpCommand;

        #endregion
    }
}