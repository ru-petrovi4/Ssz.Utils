using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ssz.Utils.MonitoredUndo.Changes;

namespace Ssz.Utils.MonitoredUndo
{
    /// <summary>
    ///     Tracks the ChangeSets and behavior for a single root object (or document).
    /// </summary>
    public class UndoRoot
    {
        // WeakReference because we don't want the undo stack to keep something locked in memory.

        #region construction and destruction

        /// <summary>
        ///     Create a new UndoRoot to track undo / redo actions for a given instance / document.
        /// </summary>
        /// <param name="root">
        ///     The "root" instance of the object hierarchy. All changesets will
        ///     need to passs a reference to this instance when they track changes.
        /// </param>
        public UndoRoot(object? root)
        {
            if (root == null) return;

            _root = new WeakReference(root);
            _undoStack = new Stack<ChangeSet>();
            _redoStack = new Stack<ChangeSet>();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Tells the UndoRoot that all subsequent changes should be part of a single ChangeSet.
        /// </summary>
        public void BeginChangeSetBatch(string batchDescription, bool consolidateChangesForSameInstance)
        {
            if (_root == null) return;

            // We don't want to add additional changes representing the operations that happen when undoing or redoing a change.
            if (_isUndoingOrRedoing)
                return;

            _isInBatchCounter++;

            if (_isInBatchCounter == 1)
            {
                _consolidateChangesForSameInstance = consolidateChangesForSameInstance;
                _currentBatchChangeSet = new ChangeSet(this, batchDescription, null);
            }
        }

        /// <summary>
        ///     Tells the UndoRoot that it can stop collecting Changes into a single ChangeSet.
        /// </summary>
        public void EndChangeSetBatch()
        {
            if (_root == null) return;

            // We don't want to add additional changes representing the operations that happen when undoing or redoing a change.
            if (_isUndoingOrRedoing)
                return;

            _isInBatchCounter--;

            if (_isInBatchCounter == 0)
            {
                if (_currentBatchChangeSet == null)
                    throw new InvalidOperationException(
                        "Cannot perform an EndChangeSetBatch when the Undo Service is not collecting a batch of changes. The batch must be started first.");

                if (_currentBatchChangeSet.Changes.Any())
                {
                    if (_undoStack == null) throw new InvalidOperationException();
                    _undoStack.Push(_currentBatchChangeSet);
                    OnUndoStackChanged();
                }
                _consolidateChangesForSameInstance = false;
                _currentBatchChangeSet = null;
            }
            else
            {
                if (_isInBatchCounter < 0) _isInBatchCounter = 0;
            }
        }

        /// <summary>
        ///     Undo the first available ChangeSet.
        /// </summary>
        public void Undo()
        {
            if (_root == null) return;

            ChangeSet? last = _undoStack?.FirstOrDefault();
            if (null != last)
                Undo(last);
        }


        /// <summary>
        ///     Undo all changesets up to and including the lastChangeToUndo.
        /// </summary>
        public void Undo(ChangeSet lastChangeToUndo)
        {
            if (_root == null) return;

            if (IsInBatch)
                throw new InvalidOperationException(
                    "Cannot perform an Undo when the Undo Service is collecting a batch of changes. The batch must be completed first.");

            if (_undoStack == null) throw new InvalidOperationException();
            if (!_undoStack.Contains(lastChangeToUndo))
                throw new InvalidOperationException(
                    "The specified change does not exist in the list of undoable changes. Perhaps it has already been undone.");

            Debug.WriteLine("Starting UNDO: " + lastChangeToUndo.Description);

            bool done = false;
            _isUndoingOrRedoing = true;

            try
            {
                do
                {
                    ChangeSet changeSet = _undoStack.Pop();
                    OnUndoStackChanged();

                    if (changeSet == lastChangeToUndo || _undoStack.Count == 0)
                        done = true;

                    changeSet.Undo();

                    if (_redoStack == null) throw new InvalidOperationException();
                    _redoStack.Push(changeSet);
                    OnRedoStackChanged();
                } while (!done);
            }
            finally
            {
                _isUndoingOrRedoing = false;
            }
        }

        /// <summary>
        ///     Redo the first available ChangeSet.
        /// </summary>
        public void Redo()
        {
            if (_root == null) return;

            ChangeSet? last = _redoStack?.FirstOrDefault();
            if (null != last)
                Redo(last);
        }

        /// <summary>
        ///     Redo ChangeSets up to and including the lastChangeToRedo.
        /// </summary>
        public void Redo(ChangeSet lastChangeToRedo)
        {
            if (_root == null) return;

            if (IsInBatch)
                throw new InvalidOperationException(
                    "Cannot perform a Redo when the Undo Service is collecting a batch of changes. The batch must be completed first.");

            if (_redoStack == null) throw new InvalidOperationException();
            if (!_redoStack.Contains(lastChangeToRedo))
                throw new InvalidOperationException(
                    "The specified change does not exist in the list of redoable changes. Perhaps it has already been redone.");

            Debug.WriteLine("Starting REDO: " + lastChangeToRedo.Description);

            bool done = false;
            _isUndoingOrRedoing = true;
            try
            {
                do
                {
                    ChangeSet changeSet = _redoStack.Pop();
                    OnRedoStackChanged();

                    if (changeSet == lastChangeToRedo || _redoStack.Count == 0)
                        done = true;

                    changeSet.Redo();

                    if (_undoStack == null) throw new InvalidOperationException();
                    _undoStack.Push(changeSet);
                    OnUndoStackChanged();
                } while (!done);
            }
            finally
            {
                _isUndoingOrRedoing = false;
            }
        }

        /// <summary>
        ///     Add a change to the Undo history. The change will be added to the existing batch, if in a batch.
        ///     Otherwise, a new ChangeSet will be created.
        /// </summary>
        /// <param name="change">The change to add to the history.</param>
        /// <param name="description">The description of this change.</param>
        public void AddChange(Change change, string description)
        {
            if (_root == null) return;

            // System.Diagnostics.Debug.WriteLine("Starting AddChange: " + description);

            // We don't want to add additional changes representing the operations that happen when undoing or redoing a change.
            if (_isUndoingOrRedoing)
                return;

            //  If batched, add to the current ChangeSet, otherwise add a new ChangeSet.
            if (IsInBatch)
            {
                if (_currentBatchChangeSet == null) throw new InvalidOperationException();
                _currentBatchChangeSet.AddChange(change);
                //System.Diagnostics.Debug.WriteLine("AddChange: BATCHED " + description);
            }
            else
            {
                if (_undoStack == null) throw new InvalidOperationException();
                _undoStack.Push(new ChangeSet(this, description, change));
                OnUndoStackChanged();
                //System.Diagnostics.Debug.WriteLine("AddChange: " + description);
            }

            // Prune the RedoStack
            if (_redoStack == null) throw new InvalidOperationException();
            _redoStack.Clear();
            OnRedoStackChanged();
        }

        /// <summary>
        ///     Adds a new changeset to the undo history. The change set will be added to the existing batch, if in a batch.
        /// </summary>
        /// <param name="changeSet">The ChangeSet to add.</param>
        public void AddChange(ChangeSet changeSet)
        {
            if (_root == null) return;

            // System.Diagnostics.Debug.WriteLine("Starting AddChange: " + description);

            // We don't want to add additional changes representing the operations that happen when undoing or redoing a change.
            if (_isUndoingOrRedoing)
                return;

            //  If batched, add to the current ChangeSet, otherwise add a new ChangeSet.
            if (IsInBatch)
            {
                if (_currentBatchChangeSet == null) throw new InvalidOperationException();
                foreach (Change chg in changeSet.Changes)
                {
                    _currentBatchChangeSet.AddChange(chg);
                    //System.Diagnostics.Debug.WriteLine("AddChange: BATCHED " + description);
                }
            }
            else
            {
                if (_undoStack == null) throw new InvalidOperationException();
                _undoStack.Push(changeSet);
                OnUndoStackChanged();
                //System.Diagnostics.Debug.WriteLine("AddChange: " + description);
            }

            // Prune the RedoStack
            if (_redoStack == null) throw new InvalidOperationException();
            _redoStack.Clear();
            OnRedoStackChanged();
        }

        public void Clear()
        {
            if (_root == null) return;

            if (IsInBatch || _isUndoingOrRedoing)
                throw new InvalidOperationException(
                    "Unable to clear the undo history because the system is collecting a batch of changes, or is in the process of undoing / redoing a change.");

            if (_undoStack == null) throw new InvalidOperationException();
            _undoStack.Clear();
            if (_redoStack == null) throw new InvalidOperationException();
            _redoStack.Clear();
            OnUndoStackChanged();
            OnRedoStackChanged();
        }

        public event EventHandler? UndoStackChanged;

        public event EventHandler? RedoStackChanged;

        /// <summary>
        ///     The instance that represents the root (or document) for this set of changes.
        /// </summary>
        /// <remarks>
        ///     This is needed so that a single instance of the application can track undo histories
        ///     for multiple "root" or "document" instances at the same time. These histories should not
        ///     overlap or show in the same undo history.
        /// </remarks>
        public object? Root
        {
            get
            {
                if (null != _root && _root.IsAlive)
                    return _root.Target;
                else
                    return null;
            }
        }

        /*
        /// <summary>
        ///     A collection of undoable change sets for the current Root.
        /// </summary>
        public IEnumerable<ChangeSet> UndoStack
        {
            get { return _undoStack; }
        }

        /// <summary>
        ///     A collection of redoable change sets for the current Root.
        /// </summary>
        public IEnumerable<ChangeSet> RedoStack
        {
            get { return _redoStack; }
        }*/

        /// <summary>
        ///     Is this UndoRoot currently collecting changes as part of a batch.
        /// </summary>
        public bool IsInBatch
        {
            get { return _isInBatchCounter > 0; }
        }

        /// <summary>
        ///     Is this UndoRoot currently undoing or redoing a change set.
        /// </summary>
        public bool IsUndoingOrRedoing
        {
            get { return _isUndoingOrRedoing; }
        }

        /// <summary>
        ///     Should changes to the same property be consolidated (de-duped).
        /// </summary>
        public bool ConsolidateChangesForSameInstance
        {
            get { return _consolidateChangesForSameInstance; }
        }

        public bool CanUndo
        {
            get
            {
                if (_root == null) return false;
                if (_undoStack == null) throw new InvalidOperationException();
                return _undoStack.Count > 0 && !IsInBatch;
            }
        }

        public bool CanRedo
        {
            get
            {
                if (_root == null) return false;
                if (_redoStack == null) throw new InvalidOperationException();
                return _redoStack.Count > 0 && !IsInBatch;
            }
        }

        #endregion

        #region private functions

        private void OnUndoStackChanged()
        {
            if (null != UndoStackChanged)
                UndoStackChanged(this, EventArgs.Empty);
        }

        private void OnRedoStackChanged()
        {
            if (null != RedoStackChanged)
                RedoStackChanged(this, EventArgs.Empty);
        }

        #endregion

        #region private fields

        private readonly WeakReference? _root;

        // The list of undo / redo actions.
        private readonly Stack<ChangeSet>? _undoStack;
        private readonly Stack<ChangeSet>? _redoStack;

        // Tracks whether a batch (or batches) has been started.
        private int _isInBatchCounter = 0;

        // Determines whether the undo framework will consolidate (or de-dupe) changes to the same property within the batch.
        private bool _consolidateChangesForSameInstance = false;

        // When in a batch, changes are grouped into this ChangeSet.
        private ChangeSet? _currentBatchChangeSet;

        // Is the system currently undoing or redoing a changeset.
        private bool _isUndoingOrRedoing = false;

        #endregion
    }
}