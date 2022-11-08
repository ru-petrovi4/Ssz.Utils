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
        public ValueSubscription(IDataAccessProvider dataAccessProvider, string elementId, EventHandler<ValueUpdatedEventArgs>? valueUpdated = null)
        {
            DataAccessProvider = dataAccessProvider;
            ElementId = elementId;
            _valueUpdated = valueUpdated;

            DataAccessProvider.AddItem(ElementId, this);
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
        public string MappedElementIdOrConst { get; set; } = @"";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        public void Update(ValueStatusTimestamp valueStatusTimestamp)
        {
            var oldValueStatusTimestamp = ValueStatusTimestamp;
            ValueStatusTimestamp = valueStatusTimestamp;            
            if (_valueUpdated is not null) 
                _valueUpdated(this, new ValueUpdatedEventArgs
                {
                    OldValueStatusTimestamp = oldValueStatusTimestamp,
                    NewValueStatusTimestamp = valueStatusTimestamp
                });
        }

        /// <summary>
        /// 
        /// </summary>
        public string ElementId { get; }

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
        
        private EventHandler<ValueUpdatedEventArgs>? _valueUpdated;

        #endregion
    }

    public class ValueUpdatedEventArgs : EventArgs
    {
        public ValueStatusTimestamp OldValueStatusTimestamp { get; set; }

        public ValueStatusTimestamp NewValueStatusTimestamp { get; set; }
    }
}
