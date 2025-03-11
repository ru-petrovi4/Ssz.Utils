using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.DataAccess
{
    public class DataValueStyledElement : StyledElement, IDisposable
    {
        #region construction and destruction
        
        public DataValueStyledElement(IDsContainer container, IPlayWindowBase? playWindow,
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

            if (disposing) 
                _dataValueViewModel.Dispose();

            _valueChangedAction = null;

            Disposed = true;
        }

        ~DataValueStyledElement()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly AvaloniaProperty ValueProperty = AvaloniaProperty.Register<DataValueStyledElement, object?>(
            "Value",
            null);

        public object? Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }        

        /// <summary>
        ///     Invokes immediately with current value.
        /// </summary>
        public event Action<object?> ValueChanged
        {
            add
            {
                value(Value);

                _valueChangedAction += value;
            }
            remove => _valueChangedAction -= value;
        }

        #endregion        

        #region protected functions

        protected bool Disposed { get; private set; }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            if (args.Property == ValueProperty)
                _valueChangedAction?.Invoke(args.NewValue);
        }

        #endregion

        #region private fields

        private readonly DataValueViewModel _dataValueViewModel;

        private Action<object?>? _valueChangedAction;

        #endregion
    }
}