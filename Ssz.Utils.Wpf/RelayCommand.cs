/////////////////////////////////////////////////////////////////////////////
//
//                              COPYRIGHT (c) 2016
//                          HONEYWELL INTERNATIONAL INC.
//                              ALL RIGHTS RESERVED
//
//	Legal rights of Ssz International Inc. in this software is distinct from
//  ownership of any medium in which the software is embodied. Copyright notices 
//  must be reproduced in any copies authorized by Ssz International Inc.
//
///////////////////////////////////////////////////////////////////////////
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Ssz.Utils.Wpf
{
	/// <summary>
	/// The RelayCommand is a core class in the MVVM pattern to allow a ViewModel class
	/// to implement the code associated with a command request that has been made by the View
	/// </summary>
	/// <remarks>
	/// The RelayCommand implements the ICommand interface with its three methods (Execute, 
	/// CanExecute, and CanExecuteChanged).
	/// The intent is for the ViewModel class to create an instance of the RelayCommand class for 
	/// each of the commands that are supported in the ViewModel so that when the DataBinding
	/// from the View access the command property, the associated callback is made on the 
	/// ViewModel so that it can execute the command.
	/// 
	/// <example><code>
	/// public class MyViewModel : INotifyPropertyChanged
	/// {
	///		public MyViewModel()
	///		{
	///			//Hook the databound property to the method which does the work
	///			DoSomethingCommand = new RelayCommand(DoSomething);
	///		}
	///		
	///		///<summary>The property for the View to databind to.</summary>
	///		public RelayCommand DoSomethingCommand { get; private set; }
	///
	///     ///<summary>The method that does the work associated with the DoSomethingCommand databound property</summary>
	///		public void DoSomething()
	///		{
	///			//TODO - Implement the command here in my viewmodel class
	///		}
	/// }
	/// </code></example>
	/// </remarks>
    public class RelayCommand : ICommand
    {
        #region construction and destruction
		/// <summary>
		/// Will callback on the provided method when Execute() is called
		/// </summary>
		/// <param name="executeMethod">The method to call which will execute the command</param>
		public RelayCommand(Action executeMethod) :
			this(arg => executeMethod())
        {
		}

		/// <summary>
		/// Will callback on the provided method when Execute() is called
		/// </summary>
		/// <param name="executeMethod">The method to call which will execute the command</param>
		public RelayCommand(Action<object?> executeMethod)
			: this(executeMethod, null)
        {
        }

		/// <summary>
		/// Will callback on the provided method when Execute() is called.
		/// Will callback on the provided method when CanExecute() is called
		/// </summary>
		/// <param name="executeMethod">The method to call which will execute the command</param>
		/// <param name="canExecuteMethod">The method to call to determine if Execute() can be called</param>
		public RelayCommand(Action executeMethod, Func<bool> canExecuteMethod) :
			this(arg => executeMethod(), arg => canExecuteMethod())
        {
		}

		/// <summary>
		/// Will callback on the provided method when Execute() is called.
		/// Will callback on the provided method when CanExecute() is called
		/// </summary>
		/// <param name="executeMethod">The method to call which will execute the command</param>
		/// <param name="canExecuteMethod">The method to call to determine if Execute() can be called</param>
		public RelayCommand(Action<object?> executeMethod, Predicate<object?>? canExecuteMethod)
        {
            _targetExecuteMethod = executeMethod;
            _targetCanExecuteMethod = canExecuteMethod;
        }
        #endregion

        #region public functions
		/// <summary>
		/// Invokes the Func as defined in the RelayCommand's constructor 
		/// </summary>
		/// <remarks>
		/// If the CanExecute callback has not been defined, then the Execute method
		/// is always available to be called.
		/// </remarks>
		/// <param name="parameter">The parameter passed in from the caller</param>
		/// <returns>
		/// true if Execute() can be called
		/// false if Execute() cannot be called
		/// </returns>
		[DebuggerStepThrough]
        public bool CanExecute(object? parameter)
        {
			if (_targetExecuteMethod == null)
			{
				return false;  //No execute method defined so always fail
			}
            return _targetCanExecuteMethod == null || _targetCanExecuteMethod(parameter);
        }
		/// <summary>
		/// Invokes the action as defined in the RelayCommand's constructor 
		/// </summary>
		/// <remarks>
		/// Calls back on the defined method
		/// </remarks>
		/// <param name="parameter">The object to pass along to the execute command</param>
        public void Execute(object? parameter)
        {
			if (_targetExecuteMethod != null)
			{
				_targetExecuteMethod(parameter);
			}
		}
		/// <summary>
		/// Notification when changes occur that affect whether or not the command should execute
		/// </summary>
		public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        #endregion

        #region private fields
		private readonly Action<object?> _targetExecuteMethod;
		private readonly Predicate<object?>? _targetCanExecuteMethod;
        #endregion
    }
}