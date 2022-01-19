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
        public RelayCommand(Action<object?> executeMethod, Predicate<object?> canExecuteMethod)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;            
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
                    _canExecuteChanged += value;
                }                
            }
            remove
            {
                if (_canExecuteMethod is not null)
                {
                    _canExecuteChanged -= value;
                }
            }
        }
        
        public void NotifyCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region private fields

        private readonly Action<object?> _executeMethod;

        private readonly Predicate<object?>? _canExecuteMethod;

        private EventHandler? _canExecuteChanged;

        #endregion
    }
}