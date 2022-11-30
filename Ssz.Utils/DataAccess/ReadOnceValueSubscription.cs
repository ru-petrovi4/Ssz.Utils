using Ssz.Utils;
using System;

namespace Ssz.Utils.DataAccess
{
    public class ReadOnceValueSubscription : IValueSubscription
    {
        #region construction and destruction

        /// <summary>
        ///     Is used to one-time read value.
        ///     Callback is invoked when valueStatusTimestamp.ValueStatusCode != StatusCodes.Unknown       
        /// </summary>
        public ReadOnceValueSubscription(IDataAccessProvider dataProvider, string elementId, Action<ValueStatusTimestamp>? setValueAction)
        {
            _dataProvider = dataProvider;
            _setValueAction = setValueAction;

            _dataProvider.AddItem(elementId, this);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     ElementId actually used for subscription.
        /// </summary>
        public string MappedElementIdOrConst { get; set; } = @"";

        public TypeId? DataTypeId { get; set; }

        public bool? IsReadable { get; set; }

        public bool? IsWritable { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        void IValueSubscription.Update(ValueStatusTimestamp valueStatusTimestamp)
        {
            if (valueStatusTimestamp.ValueStatusCode == ValueStatusCodes.Unknown) return;

            _dataProvider.RemoveItem(this);            

            if (_setValueAction is not null)
            {
                _setValueAction(valueStatusTimestamp);
                _setValueAction = null;
            }
        }

        #endregion

        #region private fields

        private IDataAccessProvider _dataProvider;

        private Action<ValueStatusTimestamp>? _setValueAction;

        #endregion
    }
}
