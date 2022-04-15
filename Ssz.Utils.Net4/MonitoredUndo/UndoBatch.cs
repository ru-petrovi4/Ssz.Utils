using System;

namespace Ssz.Utils.Net4.MonitoredUndo
{
    /// <summary>
    ///     Provides a simplified way to start and end a batch via a "using" block.
    ///     When the UndoBatch is disposed (at the end of the using block) it will end the batch.
    ///     NOTE: Nested blocks _are_ supported.
    /// </summary>
    public class UndoBatch : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Starts an undo batch, which is ended when this instance is disposed. Designed for use in a using statement.
        /// </summary>
        /// <param name="instance">An object that implements ISupportsUndo. The batch will call GetUndoRoot() to get the root.</param>
        /// <param name="description">The description of this batch of changes.</param>
        /// <param name="consolidateChangesForSameInstance">Should the batch consolidate changes.</param>
        public UndoBatch(ISupportsUndo instance, string description, bool consolidateChangesForSameInstance)
            : this(UndoService.Current[instance.GetUndoRoot()], description, consolidateChangesForSameInstance)
        {
        }

        /// <summary>
        ///     Starts an undo batch, which is ended when this instance is disposed. Designed for use in a using statement.
        /// </summary>
        /// <param name="root">The UndoRoot related to this instance.</param>
        /// <param name="description">The description of this batch of changes.</param>
        /// <param name="consolidateChangesForSameInstance">Should the batch consolidate changes.</param>
        public UndoBatch(UndoRoot root, string description, bool consolidateChangesForSameInstance)
        {
            if (null == root)
                return;

            _undoRoot = root;
            root.BeginChangeSetBatch(description, consolidateChangesForSameInstance);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _undoRoot)
                    _undoRoot.EndChangeSetBatch();
            }
        }

        /// <summary>
        ///     Disposing this instance will end the associated Undo batch.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region private fields

        private readonly UndoRoot _undoRoot;

        #endregion
    }
}