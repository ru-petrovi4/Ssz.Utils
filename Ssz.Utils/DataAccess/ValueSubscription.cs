using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{    
    public class ValueSubscription : IValueSubscription, IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Is used to subscribe for value updating and to write values.
        ///     valueChangedAction(oldValue, newValue) is invoked when Value property changed. Initial Value property is new ValueStatusTimestamp(), Any(null) and Unknown status.        
        /// </summary>
        public ValueSubscription(IDataAccessProvider dataAccessProvider, string id, Action<ValueStatusTimestamp, ValueStatusTimestamp>? valueChangedAction = null)
        {
            DataAccessProvider = dataAccessProvider;
            Id = id;
            _valueChangedAction = valueChangedAction;

            ModelId = DataAccessProvider.AddItem(Id, this);
        }

        public void Dispose()
        {
            DataAccessProvider.RemoveItem(this);

            _valueChangedAction = null;
        }

        #endregion

        #region public functions

        public IDataAccessProvider DataAccessProvider { get; }

        /// <summary>
        ///     Id actually used for subscription. Initialized after constructor.       
        /// </summary>
        public string ModelId { get; }

        /// <summary>
        /// 
        /// </summary>
        object? IValueSubscription.Obj { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        void IValueSubscription.Update(ValueStatusTimestamp valueStatusTimestamp)
        {
            if (ValueStatusTimestamp == valueStatusTimestamp) return;
            if (_valueChangedAction != null) _valueChangedAction(ValueStatusTimestamp, valueStatusTimestamp);
            ValueStatusTimestamp = valueStatusTimestamp;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 
        /// </summary>
        public ValueStatusTimestamp ValueStatusTimestamp { get; private set; } = new ValueStatusTimestamp();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        public void Write(ValueStatusTimestamp valueStatusTimestamp)
        {
            DataAccessProvider.Write(this, valueStatusTimestamp);
        }

        #endregion

        #region private fields
        
        private Action<ValueStatusTimestamp, ValueStatusTimestamp>? _valueChangedAction;

        #endregion
    }
}
