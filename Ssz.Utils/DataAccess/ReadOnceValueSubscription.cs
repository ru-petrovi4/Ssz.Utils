using Ssz.Utils;
using System;

namespace Ssz.Utils.DataAccess
{
    public class ReadOnceValueSubscription : IValueSubscription
    {
        #region construction and destruction

        /// <summary>
        ///     Is used to one-time read value.
        ///     Callback is invoked when value is not null (value.ValueTypeCode != TypeCode.Empty)        
        /// </summary>
        public ReadOnceValueSubscription(IDataAccessProvider dataProvider, string id, Action<ValueStatusTimestamp>? setValueAction)
        {
            _dataProvider = dataProvider;
            _setValueAction = setValueAction;

            ModelId = _dataProvider.AddItem(id ?? @"", this);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Id actually used for subscription. Initialized after constructor.
        /// </summary>
        public string ModelId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        object? IValueSubscription.Obj { get; set; }

        /// <summary>
        ///     Callback Thread.
        /// </summary>
        /// <param name="value"></param>
        void IValueSubscription.Update(ValueStatusTimestamp vst)
        {
            if (vst.StatusCode == StatusCodes.Unknown) return;

            _dataProvider.RemoveItem(this);            

            if (_setValueAction != null)
            {
                _setValueAction(vst);
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
