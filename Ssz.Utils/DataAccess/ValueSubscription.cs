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
        ///     valueUpdated(oldValue, newValue) is invoked when Value property Updated. Initial Value property is new ValueStatusTimestamp(), Any(null) and Unknown status.        
        /// </summary>
        public ValueSubscription(IDataAccessProvider dataAccessProvider, string id, Action<ValueStatusTimestamp, ValueStatusTimestamp>? valueUpdated = null)
        {
            DataAccessProvider = dataAccessProvider;
            Id = id;
            _valueUpdated = valueUpdated;

            ModelId = DataAccessProvider.AddItem(Id, this);
        }

        public void Dispose()
        {
            DataAccessProvider.RemoveItem(this);

            _valueUpdated = null;
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
            var oldValueStatusTimestamp = ValueStatusTimestamp;
            ValueStatusTimestamp = valueStatusTimestamp;            
            if (_valueUpdated is not null) _valueUpdated(oldValueStatusTimestamp, valueStatusTimestamp);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 
        /// </summary>
        public ValueStatusTimestamp ValueStatusTimestamp { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        public void Write(ValueStatusTimestamp valueStatusTimestamp)
        {
            DataAccessProvider.Write(this, valueStatusTimestamp, null);
        }

        #endregion

        #region private fields
        
        private Action<ValueStatusTimestamp, ValueStatusTimestamp>? _valueUpdated;

        #endregion
    }
}
