using Ssz.Utils;
using System;

namespace Ssz.DataGrpc.Client
{
    public class ReadOnceValueSubscription : IValueSubscription
    {
        #region construction and destruction

        /// <summary>
        ///     Is used to one-time read value.
        ///     Callback is invoked when value is not null (value.ValueTypeCode != TypeCode.Empty)        
        /// </summary>
        public ReadOnceValueSubscription(IDataSource dataSource, string elementId, Action<Any>? setValueAction)
        {
            _dataSource = dataSource;
            _setValueAction = setValueAction;

            _dataSource.AddItem(elementId ?? @"", this);
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        object? IValueSubscription.Obj { get; set; }

        /// <summary>
        ///     Callback Thread.
        /// </summary>
        /// <param name="value"></param>
        void IValueSubscription.Update(Any value)
        {
            if (value.ValueTypeCode == TypeCode.Empty) return;

            _dataSource.RemoveItem(this);            

            if (_setValueAction != null)
            {
                _setValueAction(value);
                _setValueAction = null;
            }
        }

        #endregion

        #region private fields

        private IDataSource _dataSource;
        private Action<Any>? _setValueAction;

        #endregion
    }
}
