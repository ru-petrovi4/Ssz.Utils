using System.Collections;

namespace Ssz.Utils.MonitoredUndo.Changes
{
    // TODO collection changes need more tests

    public abstract class CollectionChange : Change
    {
        #region construction and destruction

        protected CollectionChange(object target, string propertyName, IList collection)
            : base(target)
        {
            _propertyName = propertyName;
            _collection = collection;
        }

        protected CollectionChange(object target, string propertyName, IList collection, object changeKey)
            : base(target, changeKey)
        {
            _propertyName = propertyName;
            _collection = collection;
        }

        #endregion

        #region public functions

        public IList Collection
        {
            get { return _collection; }
        }

        public string PropertyName
        {
            get { return _propertyName; }
        }

        #endregion

        #region private fields

        private readonly IList _collection;
        private readonly string _propertyName;

        #endregion
    }
}