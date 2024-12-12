using System;
using System.Windows;
using System.Windows.Data;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.DataAccess
{
    public class DataValueFrameworkElement : FrameworkElement, IDisposable
    {
        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region private functions

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {            
            if (d is DataValueFrameworkElement thisDataValueFrameworkElement)
            {
                thisDataValueFrameworkElement._valueChangedAction?.Invoke(e.NewValue);
            }
        }

        #endregion

        #region construction and destruction
        
        public DataValueFrameworkElement(IDsContainer container, IPlayWindowBase? playWindow,
            IValueDataBinding dataSourceInfo)
        {
            _dataValueViewModel = new DataValueViewModel(playWindow, false);
            DataContext = _dataValueViewModel;            

            dataSourceInfo.FallbackValue = "";
            this.SetBindingOrConst(container, ValueProperty, dataSourceInfo, BindingMode.TwoWay,
                UpdateSourceTrigger.Default);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing) _dataValueViewModel.Dispose();

            _valueChangedAction = null;

            Disposed = true;
        }


        ~DataValueFrameworkElement()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(object),
            typeof(DataValueFrameworkElement),
            new FrameworkPropertyMetadata(null, OnValuePropertyChanged));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }        

        /// <summary>
        ///     Invokes immediately with current value.
        /// </summary>
        public event Action<object> ValueChanged
        {
            add
            {
                value(Value);

                _valueChangedAction += value;
            }
            remove => _valueChangedAction -= value;
        }        

        #endregion

        #region private fields

        private readonly DataValueViewModel _dataValueViewModel;

        private Action<object>? _valueChangedAction;

        #endregion
    }
}