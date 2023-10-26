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
        ///     valueUpdated is invoked when ValueStatusTimestamp property Updated. Initial Value property is Any(null) and ValueStatusCode is Unknown status. 
        /// </summary>
        /// <param name="dataAccessProvider"></param>
        /// <param name="elementId"></param>
        /// <param name="valueStatusTimestampUpdated"></param>        
        public ValueSubscription(IDataAccessProvider dataAccessProvider, 
            string elementId, 
            EventHandler<ValueStatusTimestampUpdatedEventArgs>? valueStatusTimestampUpdated = null)
        {
            DataAccessProvider = dataAccessProvider;
            ElementId = elementId;
            _valueStatusTimestampUpdated = valueStatusTimestampUpdated;            

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

        public ValueStatusTimestamp ValueStatusTimestamp { get; private set; } = new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.Unknown };

        public void Update(string mappedElementIdOrConst)
        {
            MappedElementIdOrConst = mappedElementIdOrConst;
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

        public void Write(ValueStatusTimestamp valueStatusTimestamp)
        {
            var t = DataAccessProvider.WriteAsync(this, valueStatusTimestamp, null);
        }

        #endregion

        #region private fields

        private EventHandler<ValueStatusTimestampUpdatedEventArgs>? _valueStatusTimestampUpdated;        

        #endregion
    }

    public class ValueStatusTimestampUpdatedEventArgs : EventArgs
    {
        public ValueStatusTimestamp OldValueStatusTimestamp { get; set; }

        public ValueStatusTimestamp NewValueStatusTimestamp { get; set; }
    }
}
