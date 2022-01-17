using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Ssz.Utils.Wpf
{    
    public class RelayCommand : ICommand
    {
        #region construction and destruction

        /// <summary>
        ///     If autoDetectCanExecuteChanged = true, determines automatically when CanExecute changed, using CommandManager.RequerySuggested.
        ///     Otherwise we must call NotifyCanExecuteChanged()
        /// </summary>
        /// <param name="executeMethod"></param>
        public RelayCommand(Action<object?> executeMethod)
        {
            _executeMethod = executeMethod;
        }

        /// <summary>
        ///     If autoDetectCanExecuteChanged = true, determines automatically when CanExecute changed, using CommandManager.RequerySuggested.
        ///     Otherwise we must call NotifyCanExecuteChanged()
        /// </summary>
        /// <param name="executeMethod"></param>
        /// <param name="canExecuteMethod"></param>
        /// <param name="autoDetectCanExecuteChanged"></param>
        public RelayCommand(Action<object?> executeMethod, Predicate<object?> canExecuteMethod, bool autoDetectCanExecuteChanged)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
            _autoDetectCanExecuteChanged = autoDetectCanExecuteChanged;
        }

        #endregion

        #region public functions
        
        [DebuggerStepThrough]
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
        
        public event EventHandler? CanExecuteChanged
        {
            add 
            {
                if (_canExecuteMethod is not null)
                {
                    if (_autoDetectCanExecuteChanged)
                        CommandManager.RequerySuggested += value;
                    else
                        _canExecuteChanged += value;
                }                
            }
            remove
            {
                if (_canExecuteMethod is not null)
                {
                    if (_autoDetectCanExecuteChanged)
                        CommandManager.RequerySuggested -= value;
                    else
                        _canExecuteChanged -= value;
                }
            }
        }

        /// <summary>
        ///     You need to call this method only when autoDetectCanExecuteChanged = false.
        /// </summary>
        public void NotifyCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region private fields

        private readonly Action<object?> _executeMethod;

        private readonly Predicate<object?>? _canExecuteMethod;

        private readonly bool _autoDetectCanExecuteChanged;

        private EventHandler? _canExecuteChanged;

        #endregion
    }
}