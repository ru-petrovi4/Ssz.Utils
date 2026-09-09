using System;
using System.Windows.Input;

namespace Ssz.Operator.Core.Utils
{
    /// <summary>
    ///     Avalonia counterpart of Ssz.Utils.Wpf.RelayCommand.
    ///     WPF's autoDetectCanExecuteChanged relied on CommandManager.RequerySuggested, which does not
    ///     exist outside WPF; the constructor overload is kept for source compatibility, but the
    ///     CanExecuteChanged event is always raised manually through NotifyCanExecuteChanged().
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region construction and destruction

        public RelayCommand(Action<object?> executeMethod)
        {
            _executeMethod = executeMethod;
        }

        public RelayCommand(Action<object?> executeMethod, Predicate<object?> canExecuteMethod)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        public RelayCommand(Action<object?> executeMethod, Predicate<object?> canExecuteMethod,
            bool autoDetectCanExecuteChanged)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        #endregion

        #region public functions

        public bool CanExecute(object? parameter)
        {
            if (_canExecuteMethod is null)
                return true;
            return _canExecuteMethod(parameter);
        }

        public void Execute(object? parameter)
        {
            _executeMethod(parameter);
        }

        public event EventHandler? CanExecuteChanged;

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region private fields

        private readonly Action<object?> _executeMethod;
        private readonly Predicate<object?>? _canExecuteMethod;

        #endregion
    }
}
