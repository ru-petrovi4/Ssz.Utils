using System.Collections.Generic;
using System.Linq;
using Ssz.Utils.Net4.MonitoredUndo.Changes;

namespace Ssz.Utils.Net4.MonitoredUndo
{
    /// <summary>
    ///     A set of changes that represent a single "unit of change".
    /// </summary>
    public class ChangeSet
    {
        #region construction and destruction

        /// <summary>
        ///     Create a ChangeSet for the specified UndoRoot.
        /// </summary>
        /// <param name="undoRoot">The UndoRoot that this ChangeSet belongs to.</param>
        /// <param name="description">A description of the change.</param>
        /// <param name="change">The Change instance that can perform the undo / redo as needed.</param>
        public ChangeSet(UndoRoot undoRoot, string description, Change change)
        {
            _undoRoot = undoRoot;
            _changes = new List<Change>();
            _description = description;

            if (null != change)
                AddChange(change);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The associated UndoRoot.
        /// </summary>
        public UndoRoot UndoRoot
        {
            get { return _undoRoot; }
        }

        /// <summary>
        ///     A description of this set of changes.
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        /// <summary>
        ///     Has this ChangeSet been undone.
        /// </summary>
        public bool Undone
        {
            get { return _undone; }
        }

        /// <summary>
        ///     The changes that are part of this ChangeSet
        /// </summary>
        public IEnumerable<Change> Changes
        {
            get { return _changes; }
        }

        #endregion

        #region internal functions

        /// <summary>
        ///     Add a change to this ChangeSet.
        /// </summary>
        /// <param name="change"></param>
        internal void AddChange(Change change)
        {
            if (_undoRoot.ConsolidateChangesForSameInstance)
            {
                //var dupes = _Changes.Where(c => null != c.ChangeKey && c.ChangeKey.Equals(change.ChangeKey)).ToList();
                //if (null != dupes && dupes.Count > 0)
                //    dupes.ForEach(c => _Changes.Remove(c));

                Change dupe = _changes.FirstOrDefault(c => null != c.ChangeKey && c.ChangeKey.Equals(change.ChangeKey));
                if (null != dupe)
                {
                    dupe.MergeWith(change);
                    // System.Diagnostics.Debug.WriteLine("AddChange: MERGED");
                }
                else
                {
                    _changes.Add(change);
                }
            }
            else
            {
                _changes.Add(change);
            }
        }

        /// <summary>
        ///     Undo all Changes in this ChangeSet.
        /// </summary>
        internal void Undo()
        {
            foreach (Change change in _changes.Reverse())
                change.Undo();

            _undone = true;
        }

        /// <summary>
        ///     Redo all Changes in this ChangeSet.
        /// </summary>
        internal void Redo()
        {
            foreach (Change change in _changes)
                change.Redo();

            _undone = false;
        }

        #endregion

        #region private fields

        private readonly UndoRoot _undoRoot;
        private readonly string _description;
        private readonly IList<Change> _changes;
        private bool _undone = false;

        #endregion
    }
}