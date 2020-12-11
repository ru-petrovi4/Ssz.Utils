using System;
using System.Collections;
using System.Diagnostics;

namespace Ssz.Utils.MonitoredUndo.Changes
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class CollectionMoveChange : CollectionChange
    {
        #region construction and destruction

        public CollectionMoveChange(object target, string propertyName, IList collection, int newIndex, int oldIndex)
            : base(target, propertyName, collection,
                new ChangeKey<object, string, object>(
                    target, propertyName, new ChangeKey<int, int>(oldIndex, newIndex)))
        {
            _newIndex = newIndex;
            _oldIndex = oldIndex;

            _redoNewIndex = newIndex;
            _redoOldIndex = oldIndex;
        }

        #endregion

        #region public functions

        public override void MergeWith(Change latestChange)
        {
            var other = latestChange as CollectionMoveChange;

            if (null != other)
            {
                _redoOldIndex = other._redoOldIndex;
                _redoNewIndex = other._redoNewIndex;
            }
            // FIXME should only affect undo
        }

        public int NewIndex
        {
            get { return _newIndex; }
        }

        public int OldIndex
        {
            get { return _oldIndex; }
        }

        #endregion

        #region protected functions

        protected override void PerformUndo()
        {
            var m = Collection.GetType().GetMethod("Move");
            if (m == null) throw new InvalidOperationException();
            m.Invoke(Collection, new object[] {NewIndex, OldIndex});
        }

        protected override void PerformRedo()
        {
            var m = Collection.GetType().GetMethod("Move");
            if (m == null) throw new InvalidOperationException();
            m.Invoke(Collection, new object[] {_redoOldIndex, _redoNewIndex});
        }

        #endregion

        #region private functions

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(
                    "CollectionMoveChange(Property={0}, Target={{{1}}}, OldIndex={2}, NewIndex={{{3}}})",
                    PropertyName, Target, OldIndex, NewIndex);
            }
        }

        #endregion

        #region private fields

        private readonly int _newIndex;
        private readonly int _oldIndex;

        private int _redoNewIndex;
        private int _redoOldIndex;

        #endregion
    }
}