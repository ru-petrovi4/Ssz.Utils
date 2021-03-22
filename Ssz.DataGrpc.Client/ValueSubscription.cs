using Ssz.DataGrpc.Common;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Client
{    
    public class ValueSubscription : IValueSubscription, IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Is used to subscribe for value updating and to write values.
        ///     valueChangedAction(oldValue, newValue) is invoked when Value property changed. Initial Value property is Any(null).        
        /// </summary>
        public ValueSubscription(IDataSource dataSource, string elementId, Action<Any, Any>? valueChangedAction = null)
        {
            _dataSource = dataSource;
            ElementId = elementId;
            _valueChangedAction = valueChangedAction;

            _dataSource.AddItem(ElementId, this);
        }

        public void Dispose()
        {
            _dataSource.RemoveItem(this);

            _valueChangedAction = null;
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        object? IValueSubscription.Obj { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        void IValueSubscription.Update(Any value)
        {
            if (Value == value) return;
            if (_valueChangedAction != null) _valueChangedAction(Value, value);
            Value = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public string ElementId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Any Value { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Write(Any value)
        {
            _dataSource.Write(this, value);
        }

        #endregion

        #region private fields

        private readonly IDataSource _dataSource;
        private Action<Any, Any>? _valueChangedAction;

        #endregion
    }
}
