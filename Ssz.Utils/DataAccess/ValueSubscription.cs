using Microsoft.Extensions.Logging;
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
        ///     valueUpdated is invoked when ValueStatusTimestamp property Updated. Initial Value property is new ValueStatusTimestamp(), Any(null) and Unknown status. 
        /// </summary>
        /// <param name="dataAccessProvider"></param>
        /// <param name="elementId"></param>
        /// <param name="valueStatusTimestampUpdated"></param>
        /// <param name="addItemResultUpdated"></param>
        public ValueSubscription(IDataAccessProvider dataAccessProvider, 
            string elementId, 
            EventHandler<ValueStatusTimestampUpdatedEventArgs>? valueStatusTimestampUpdated = null, 
            EventHandler? addItemResultUpdated = null)
        {
            DataAccessProvider = dataAccessProvider;
            ElementId = elementId;
            _valueStatusTimestampUpdated = valueStatusTimestampUpdated;
            _addItemResultUpdated = addItemResultUpdated;

            DataAccessProvider.AddItem(ElementId, this);
        }

        public void Dispose()
        {
            DataAccessProvider.RemoveItem(this);

            _valueStatusTimestampUpdated = null;
        }

        #endregion

        #region public functions

        public IDataAccessProvider DataAccessProvider { get; }
        
        public string ElementId { get; }

        /// <summary>
        ///     Id actually used for subscription.
        /// </summary>
        public string MappedElementIdOrConst { get; private set; } = @"";        

        public AddItemResult AddItemResult { get; private set; } = AddItemResult.UnknownAddItemResult;
        
        public ValueStatusTimestamp ValueStatusTimestamp { get; private set; }

        public void Update(string mappedElementIdOrConst)
        {
            MappedElementIdOrConst = mappedElementIdOrConst;
        }

        public void Update(AddItemResult addItemResult)
        {
            AddItemResult = addItemResult;
            if (_addItemResultUpdated is not null)
                _addItemResultUpdated(this, EventArgs.Empty);
        }
        
        public void Update(ValueStatusTimestamp valueStatusTimestamp)
        {
            var oldValueStatusTimestamp = ValueStatusTimestamp;
            ValueStatusTimestamp = valueStatusTimestamp;            
            if (_valueStatusTimestampUpdated is not null) 
                _valueStatusTimestampUpdated(this, new ValueStatusTimestampUpdatedEventArgs
                {
                    OldValueStatusTimestamp = oldValueStatusTimestamp,
                    NewValueStatusTimestamp = valueStatusTimestamp
                });
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        public async Task<ResultInfo> WriteAsync(ValueStatusTimestamp valueStatusTimestamp, ILogger? userFriendlyLogger = null)
        {
            return await DataAccessProvider.WriteAsync(this, valueStatusTimestamp, userFriendlyLogger);
        }

        #endregion

        #region private fields

        private EventHandler<ValueStatusTimestampUpdatedEventArgs>? _valueStatusTimestampUpdated;
        private EventHandler? _addItemResultUpdated;

        #endregion
    }

    public class ValueStatusTimestampUpdatedEventArgs : EventArgs
    {
        public ValueStatusTimestamp OldValueStatusTimestamp { get; set; }

        public ValueStatusTimestamp NewValueStatusTimestamp { get; set; }
    }
}
