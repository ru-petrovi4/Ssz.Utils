using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ssz.Utils
{
	/// <summary>
	/// The ViewModelBase is a core class in the MVVM pattern to allow a ViewModel class
	/// to reuse the PropertyChanged methods/pattern associated with the INotifyPropertyChanged
	/// </summary>
	/// <remarks>
	/// Every Model and every ViewModel should implement the INotifyPropertyChanged interface in
	/// order to support data binding.  In the MVVM pattern, the Model (or ViewModel) communicates
	/// up to interested parties exclusively through the property changed notification.
	/// 
	/// This base class provides some typical implementation for the PropertyChanged and SetValue
	/// methods.  This helps to reduce copy/paste code in the derived class.  The base class is
	/// expected to be used in the following manner in the Model or ViewModel
	/// <example><code>
	/// public class MyViewModel : ViewModelBase	//derive from INPC base class
	/// {
	///		private string _firstName;
	///		public string FirstName 
	///		{ 
	///			get { return _firstName; } 
	///			set { SetValue(ref _firstName, value); }	//Call base.SetValue to set the value and raise the PropertyChanged event
	///		}
	/// }
	/// </code></example>
	/// </remarks>	
    public class ViewModelBase : INotifyPropertyChanged
    {
        #region Events and Delgates

 		/// <summary>
		///     Notification that the value contained in a property has changed		
		/// </summary>		
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		///     Clears PropertyChange event from subscribers. PropertyChanged becomes empty but not null.
		/// </summary>
		public void ClearPropertyChangedEvent()
		{
			PropertyChanged = null;
		}

		#endregion

		#region protected functions

		/// <summary>
		/// Causes the PropertyChanged event to fire for the specified property name
		/// </summary>
		/// <remarks>
		/// This method is provided so that a class can force a property changed event
		/// to be raised on a specific property.  This will typically be done if one property 
		/// is changed and it is related to a derived property.  An example would be the Height
		/// property value changing also affects the Size property (even though Size was not 
		/// directly changed)
		/// 
		/// Note that using this method is error prone as demonstrated in the following example
		/// <example>
		/// //OnPropertyChanged("Size");		//Don't use - error prone - runtime validation only
		/// OnPropertyChanged(() => Size);		//Best practice - compile time validation
		/// </example>
		/// </remarks>
		/// <param name="propertyName">The property that needs to raise the PropertyChanged notification</param>		
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (propertyName is not null) VerifyPropertyName(propertyName);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

		/// <summary>
		/// Causes the PropertyChanged event to fire for the specified property
		/// </summary>
		/// <remarks>
		/// This method is provided so that a class can force a property changed event
		/// to be raised on a specific property.  This will typically be done if one property 
		/// is changed and it is related to a derived property.  An example would be the Height
		/// property value changing also affects the Size property (even though Size was not 
		/// directly changed)
		/// 
		/// This method is the preferred way to raise a property changed notification.  This 
		/// allows the caller to directly use the property which then allows for compile time 
		/// validation.
		/// <example>
		/// //OnPropertyChanged("Size");		//Don't use - error prone - runtime validation only
		/// OnPropertyChanged(() => Size);		//Best practice - compile time validation
		/// </example>
		/// </remarks>
        /// <param name="propertyNameExpression">The property that needs to raise the PropertyChanged notification</param>        
		protected virtual void OnPropertyChanged<T>(Expression<Func<T>> propertyNameExpression)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(((MemberExpression)propertyNameExpression.Body).Member.Name));
        }
        
		/// <summary>
		/// Causes the PropertyChanged event to fire for the current property
		/// </summary>
		/// <remarks>
		/// This method can be used when within a property to automatically raise its property changed event.
		/// <example>
		/// private string _firstName;
		/// public string FirstName
		/// {
		///		get { return _firstName; }
		///		set 
		///		{
		///			_firstName = value;
		///			OnPropertyChangedAuto();	//No need to specify "FirstName" as the argument
		///		}
		/// }
		/// </example>
		/// </remarks>
		/// <param name="propertyName">
		/// Automatically set via the [CallerMemberName] attribute.  If the property 
		/// calling this method is "FirstName", then propertyName is autmatically 
		/// populated with "FirstName".  This helps to avoid typos ... especially
		/// if the property name is changed or refactored.
		/// </param>        
        protected virtual void OnPropertyChangedAuto([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

		/// <summary>
		/// Sets the value on the backing field and raises the ProperyChanged notification
		/// if the value has actually changed
		/// </summary>
		/// <remarks>
		/// This method is expected to be used in the following manner by the derived class
		/// <example><code>
		/// public class MyViewModel : ViewModelBase	//derive from INPC base class
		/// {
		///		private string _firstName;
		///		public string FirstName 
		///		{ 
		///			get { return _firstName; } 
		///			set { SetValue(ref _firstName, value); }	//Call base.SetValue to set the value and raise the PropertyChanged event
		///		}
		/// }
		/// </code></example></remarks>
		/// <typeparam name="T">The field's type - makes this method generic</typeparam>
		/// <param name="backingField">
		/// A reference to the local member variable in the derived class 
		/// which contains the current value of the field
		/// </param>
		/// <param name="value">The value that this field is being set to</param>
		/// <param name="propertyName">
		/// Automatically set via the [CallerMemberName] attribute.  If the property 
		/// calling this method is "FirstName", then propertyName is autmatically 
		/// populated with "FirstName".  This helps to avoid typos ... especially
		/// if the property name is changed or refactored.
		/// </param>
		/// <returns>
		/// true if the new value is different from the old value
		/// false if the new and old values are equal (and PropertyChanged notifcation was not sent)
		/// </returns>        
        protected virtual bool SetValue<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        #endregion

		#region private functions

		/// <summary>
		/// Verifies that the property name that is specified is a valid property on the object.
		/// </summary>
		/// <remarks>
		/// This is done because typos in the property name could cause problems
		/// or if the property name changes but the caller doesn't update the related
		/// string.  We use reflection to verify that a property of the specified 
		/// name is on this class.
		/// </remarks>
		/// <param name="propertyName">The name of the property to validate</param>
		[Conditional("DEBUG")]
		[DebuggerStepThrough]
		private void VerifyPropertyName(string propertyName)
		{
			if (propertyName == @"Item[]") return;
			// Verify that the property name matches a real,  
			// public, instance property on this object.

			var parts = propertyName.Split('.');
			var currentObjectType = GetType();
			foreach (var part in parts)
			{
				var prop = TypeDescriptor.GetProperties(currentObjectType)[part];
				if (prop is null)
				{
					throw new Exception("Invalid property name: " + propertyName);
				}
				else
				{
					currentObjectType = prop.PropertyType;
				}
			}
		}

		#endregion
	}
}