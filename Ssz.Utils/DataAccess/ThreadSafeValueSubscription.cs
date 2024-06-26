﻿using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class ThreadSafeValueSubscription : IValueSubscription, IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Is used to subscribe for value updating and to write values.
        ///     valueUpdated(oldValue, newValue) is invoked when Value property Updated. Initial Value property is Any(null) and StatusCode is Unknown status.        
        /// </summary>
        public ThreadSafeValueSubscription(IDataAccessProvider dataAccessProvider, string elementId, Action<ValueStatusTimestamp, ValueStatusTimestamp>? valueUpdated = null)
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

        public void Update(string mappedElementIdOrConst)
        {
        }        

        /// <summary>
        ///     Can be called from any thread.
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        void IValueSubscription.Update(ValueStatusTimestamp valueStatusTimestamp)
        {
            ValueStatusTimestamp oldValueStatusTimestamp;
            lock (_valueStatusTimestampSyncRoot)
            {
                oldValueStatusTimestamp = _valueStatusTimestamp;
                _valueStatusTimestamp = valueStatusTimestamp;
            }
            if (!StatusCodes.IsUncertain(valueStatusTimestamp.StatusCode))
                ValueStatusTimestampUpdated.Set();
            if (_valueUpdated is not null) 
                _valueUpdated(oldValueStatusTimestamp, valueStatusTimestamp);
        }

        /// <summary>
        /// 
        /// </summary>
        public string ElementId { get; }

        /// <summary>
        ///     Is set when ValueStatusTimestamp.StatusCode != StatusCode.Unknown
        /// </summary>
        public readonly ManualResetEvent ValueStatusTimestampUpdated = new ManualResetEvent(false);

        /// <summary>
        /// 
        /// </summary>
        public ValueStatusTimestamp ValueStatusTimestamp
        {
            get
            {
                lock (_valueStatusTimestampSyncRoot)
                {
                    return _valueStatusTimestamp;
                }
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        public async Task<ResultInfo> WriteAsync(ValueStatusTimestamp valueStatusTimestamp)
        {
            return await DataAccessProvider.WriteAsync(this, valueStatusTimestamp);
        }

        #endregion

        #region private fields

        private Action<ValueStatusTimestamp, ValueStatusTimestamp>? _valueUpdated;

        private ValueStatusTimestamp _valueStatusTimestamp = new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain };

        private readonly object _valueStatusTimestampSyncRoot = new Object();

        #endregion
    }
}
