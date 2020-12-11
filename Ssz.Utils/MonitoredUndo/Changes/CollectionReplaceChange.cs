using System.Collections;
using System.Diagnostics;

namespace Ssz.Utils.MonitoredUndo.Changes
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class CollectionReplaceChange : CollectionChange
    {
        #region construction and destruction

        public CollectionReplaceChange(object target, string propertyName, IList collection,
            int index, object oldItem, object newItem)
            : base(target, propertyName, collection,
                new ChangeKey<object, string, object>(target, propertyName,
                    new ChangeKey<object, object>(oldItem, newItem)))
        {
            _index = index;
            _oldItem = oldItem;
            _newItem = newItem;

            _redoIndex = index;
            _redoNewItem = newItem;
        }

        #endregion

        #region public functions

        public override void MergeWith(Change latestChange)
        {
            var other = latestChange as CollectionReplaceChange;

            if (null != other)
            {
                _redoIndex = other._redoIndex;
                _redoNewItem = other._redoNewItem;
            }
        }

        public int Index
        {
            get { return _index; }
        }

        public object OldItem
        {
            get { return _oldItem; }
        }

        public object NewItem
        {
            get { return _newItem; }
        }

        #endregion

        #region protected functions

        protected override void PerformUndo()
        {
            Collection[Index] = OldItem;
        }

        protected override void PerformRedo()
        {
            Collection[_redoIndex] = _redoNewItem;
        }

        #endregion

        #region private functions

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(
                    "CollectionReplaceChange(Property={0}, Target={{{1}}}, Index={2}, NewItem={{{3}}}, OldItem={{{4}}})",
                    PropertyName, Target, Index, NewItem, OldItem);
            }
        }

        #endregion

        #region private fields

        private readonly int _index;
        private readonly object _oldItem;
        private readonly object _newItem;

        private int _redoIndex;
        private object _redoNewItem;

        #endregion
    }
}