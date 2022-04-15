using System;

namespace Ssz.Utils.Net4.MonitoredUndo.Changes
{
    /// <summary>
    ///     Represents an individual change, with the commands to undo / redo the change as needed.
    /// </summary>
    public class DelegateChange : Change
    {
        #region construction and destruction

        /// <summary>
        ///     Create a new change item.
        /// </summary>
        /// <param name="target">The object that this change affects.</param>
        /// <param name="undoAction">The delegate that will do the Undo logic</param>
        /// <param name="redoAction">The delegate that will do the Redo logic</param>
        /// <param name="changeKey">
        ///     An object, that will be used to detect changes that affect the same "field".
        ///     This object should implement or override object.Equals() and return true if the changes are for the same field.
        ///     This is used when the undo UndoRoot has started a batch, or when the UndoRoot.ConsolidateChangesForSameInstance is
        ///     true.
        ///     A string will work, but should be sufficiently unique within the scope of changes that affect this Target instance.
        ///     Another good option is to use the Tuple class to uniquely identify the change. The Tuple could contain
        ///     the object, and a string representing the property name. For a collection change, you might include the
        ///     instance, the property name, and the item added/removed from the collection.
        /// </param>
        public DelegateChange(object target, Action undoAction, Action redoAction, object changeKey)
            : base(target, changeKey)
        {
            _undoAction = undoAction; // new WeakReference(undoAction);
            _redoAction = redoAction; // new WeakReference(redoAction);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     When consolidating events, we want to keep the original "Undo"
        ///     but use the most recent Redo. This will pull the Redo from the
        ///     specified Change and apply it to this instance.
        /// </summary>
        public override void MergeWith(Change latestChange)
        {
            var other = latestChange as DelegateChange;

            if (null != other)
                _redoAction = other._redoAction;
        }

        #endregion

        #region protected functions

        protected override void PerformUndo()
        {
            Action action = _undoAction;
            if (null != action)
                action();
        }

        protected override void PerformRedo()
        {
            Action action = _redoAction;
            if (null != action)
                action();
        }

        #endregion

        #region private fields

        private readonly Action _undoAction;
        private Action _redoAction;

        #endregion
    }
}