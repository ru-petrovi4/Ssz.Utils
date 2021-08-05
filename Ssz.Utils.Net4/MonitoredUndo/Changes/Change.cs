namespace Ssz.Utils.MonitoredUndo.Changes
{
    /// <summary>
    ///     Represents an individual change, with the commands to undo / redo the change as needed.
    /// </summary>
    public abstract class Change
    {
        #region construction and destruction

        /// <summary>
        ///     Create a new change item.
        /// </summary>
        /// <param name="target">The object that this change affects.</param>
        protected Change(object target)
        {
            _target = target; // new WeakReference(target);
            _changeKey = this;
        }

        /// <summary>
        ///     Create a new change item.
        /// </summary>
        /// <param name="target">The object that this change affects.</param>
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
        protected Change(object target, object changeKey)
        {
            _target = target; // new WeakReference(target);
            _changeKey = changeKey;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     When consolidating events, we want to keep the original (first) "Undo"
        ///     but use the most recent Redo. This will pull the Redo from the
        ///     specified Change and apply it to this instance.
        /// </summary>
        public abstract void MergeWith(Change latestChange);

        /// <summary>
        ///     A reference to the object that this change is for.
        /// </summary>
        public object Target
        {
            get { return _target; }
        }

        /// <summary>
        ///     The change "key" that uniquely identifies this instance. (see commends on the constructor.)
        /// </summary>
        public object ChangeKey
        {
            get { return _changeKey; }
        }

        /// <summary>
        ///     Has this change been undone.
        /// </summary>
        public bool Undone
        {
            get { return _undone; }
        }

        #endregion

        #region internal functions

        /// <summary>
        ///     Apply the undo logic from this instance, and raise the ISupportsUndoNotification.UndoHappened event.
        /// </summary>
        internal void Undo()
        {
            PerformUndo();

            _undone = true;

            var notify = Target as ISupportUndoNotification;
            if (null != notify)
                notify.UndoHappened(this);
        }

        /// <summary>
        ///     Apply the redo logic from this instance, and raise the ISupportsUndoNotification.RedoHappened event.
        /// </summary>
        internal void Redo()
        {
            PerformRedo();

            _undone = false;

            var notify = Target as ISupportUndoNotification;
            if (null != notify)
                notify.RedoHappened(this);
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     Overridden in derived classes to contain the actual Undo logic.
        /// </summary>
        protected abstract void PerformUndo();

        /// <summary>
        ///     Overridden in derived classes to contain the actual Redo logic.
        /// </summary>
        protected abstract void PerformRedo();

        #endregion

        #region private fields

        private readonly object _target;
        private bool _undone = false;
        private readonly object _changeKey;

        #endregion
    }
}