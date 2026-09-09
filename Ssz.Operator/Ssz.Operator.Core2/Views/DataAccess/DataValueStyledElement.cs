using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.MultiValueConverters;

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
            (_, _valueMultiBinding) = this.SetBindingOrConst(container, ValueProperty, dataSourceInfo,
                BindingMode.TwoWay, UpdateSourceTrigger.Default);
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
        ///     Writes the value to the data source and to this element.
        ///     Avalonia's MultiBinding is one way only: its IMultiValueConverter has no ConvertBack, so
        ///     assigning Value alone would stay in the element and never reach the model. ValueConverterBase
        ///     carries an Avalonia-shaped ConvertBack that writes straight into the DataValueViewModel, and
        ///     it has to be called explicitly.
        /// </summary>
        public void WriteValueToSource(object? value)
        {
            Value = value;

            if (_valueMultiBinding?.Converter is ValueConverterBase valueConverter)
                valueConverter.ConvertBack(value, _dataValueViewModel, null, CultureInfo.InvariantCulture);
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

        private readonly MultiBinding? _valueMultiBinding;

        private Action<object?>? _valueChangedAction;

        #endregion
    }
}