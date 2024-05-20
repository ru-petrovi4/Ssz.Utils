using Ssz.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class ReadOnceValueSubscription : IValueSubscription
    {
        #region construction and destruction

        /// <summary>
        ///     Is used to one-time read value.
        ///     Callback is invoked when valueStatusTimestamp.StatusCode != StatusCodes.Uncertain       
        /// </summary>
        public ReadOnceValueSubscription(IDataAccessProvider dataProvider, string elementId, Action<ValueStatusTimestamp>? setValueAction)
        {
            _dataProvider = dataProvider;
            ElementId = elementId;
            _setValueAction = setValueAction;

            _dataProvider.AddItem(elementId, this);
        }

        #endregion

        #region public functions

        public string ElementId { get; }

        public string MappedElementIdOrConst { get; private set; } = @"";

        public SemaphoreSlim SemaphoreSlim { get; private set; } = new(0);

        public void Update(string mappedElementIdOrConst)
        {
            MappedElementIdOrConst = mappedElementIdOrConst;
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueStatusTimestamp"></param>
        void IValueSubscription.Update(ValueStatusTimestamp valueStatusTimestamp)
        {
            if (StatusCodes.IsUncertain(valueStatusTimestamp.StatusCode) || _dataProvider is null)
                return;

            _dataProvider.RemoveItem(this);
            _dataProvider = null;

            if (_setValueAction is not null)
            {
                _setValueAction(valueStatusTimestamp);
                _setValueAction = null;
            }

            SemaphoreSlim.Release();
        }

        #endregion

        #region private fields

        private IDataAccessProvider? _dataProvider;

        private Action<ValueStatusTimestamp>? _setValueAction;

        #endregion
    }
}
