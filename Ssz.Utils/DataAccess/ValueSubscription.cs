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
        ///     valueChangedAction(oldValue, newValue) is invoked when Value property changed. Initial Value property is Any(null).        
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
        /// <param name="value"></param>
        void IValueSubscription.Update(ValueStatusTimestamp vst)
        {
            if (Vst == vst) return;
            if (_valueChangedAction != null) _valueChangedAction(vst, Vst);
            Vst = vst;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 
        /// </summary>
        public ValueStatusTimestamp Vst { get; private set; } = new ValueStatusTimestamp();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Write(ValueStatusTimestamp vst)
        {
            DataAccessProvider.Write(this, vst);
        }

        #endregion

        #region private fields
        
        private Action<ValueStatusTimestamp, ValueStatusTimestamp>? _valueChangedAction;

        #endregion
    }
}
