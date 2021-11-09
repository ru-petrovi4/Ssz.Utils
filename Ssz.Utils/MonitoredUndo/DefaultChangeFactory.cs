using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Ssz.Utils.MonitoredUndo.Changes;

namespace Ssz.Utils.MonitoredUndo
{
    public class DefaultChangeFactory
    {
        #region public functions

        public static readonly DefaultChangeFactory Instance = new DefaultChangeFactory();

        /// <summary>
        ///     Construct a Change instance with actions for undo / redo.
        /// </summary>
        /// <param name="instance">The instance that changed.</param>
        /// <param name="propertyName">The property name that changed. (Case sensitive, used by reflection.)</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <returns>A Change that can be added to the UndoRoot's undo stack.</returns>
        public virtual Change? GetChange(object instance, string propertyName, object? oldValue, object? newValue)
        {
            var undoMetadata = instance as IUndoMetadata;
            if (null != undoMetadata)
            {
                if (!undoMetadata.CanUndoProperty(propertyName, oldValue, newValue))
                    return null;
            }

            var change = new PropertyChange(instance, propertyName, oldValue, newValue);

            return change;
        }

        /// <summary>
        ///     Construct a Change instance with actions for undo / redo.
        /// </summary>
        /// <param name="instance">The instance that changed.</param>
        /// <param name="propertyName">The property name that changed. (Case sensitive, used by reflection.)</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        public virtual void OnChanging(object instance, string propertyName, object? oldValue, object? newValue)
        {
            OnChanging(instance, propertyName, oldValue, newValue, propertyName);
        }

        /// <summary>
        ///     Construct a Change instance with actions for undo / redo.
        /// </summary>
        /// <param name="instance">The instance that changed.</param>
        /// <param name="propertyName">The property name that changed. (Case sensitive, used by reflection.)</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="descriptionOfChange">A description of this change.</param>
        public virtual void OnChanging(object instance, string propertyName, object? oldValue, object? newValue,
            string descriptionOfChange)
        {
            var supportsUndo = instance as ISupportsUndo;
            if (null == supportsUndo) return;

            object? root = supportsUndo.GetUndoRoot();
            if (null == root) return;

            // Add the changes to the UndoRoot
            UndoRoot undoRoot = UndoService.Current[root];

            if (undoRoot.IsUndoingOrRedoing) return;

            Change? change = GetChange(instance, propertyName, oldValue, newValue);
            if (change is null) return;
            undoRoot.AddChange(change, descriptionOfChange);
        }

        /// <summary>
        ///     Construct a Change instance with actions for undo / redo.
        /// </summary>
        /// <param name="instance">The instance that changed.</param>
        /// <param name="propertyName">
        ///     The property name that exposes the collection that changed. (Case sensitive, used by
        ///     reflection.)
        /// </param>
        /// <param name="collection">The collection that had an item added / removed.</param>
        /// <param name="e">The NotifyCollectionChangedEventArgs event args parameter, with info about the collection change.</param>
        /// <returns>A Change that can be added to the UndoRoot's undo stack.</returns>
        public virtual IList<Change>? GetCollectionChange(object instance, string propertyName, object collection,
            NotifyCollectionChangedEventArgs e)
        {
            var undoMetadata = instance as IUndoMetadata;
            if (null != undoMetadata)
            {
                if (!undoMetadata.CanUndoCollectionChange(propertyName, collection, e))
                    return null;
            }

            var ret = new List<Change>();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                        foreach (object item in e.NewItems)
                        {
                            var change = new CollectionAddChange(instance, propertyName, (IList) collection,
                                e.NewStartingIndex, item);

                            ret.Add(change);
                        }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                        foreach (object item in e.OldItems)
                        {
                            var change = new CollectionRemoveChange(instance, propertyName, (IList) collection,
                                e.OldStartingIndex, item);

                            ret.Add(change);
                        }

                    break;

#if !SILVERLIGHT
                case NotifyCollectionChangedAction.Move:
                    var moveChange = new CollectionMoveChange(instance, propertyName, (IList) collection,
                        e.NewStartingIndex,
                        e.OldStartingIndex);
                    ret.Add(moveChange);
                    break;
#endif
                case NotifyCollectionChangedAction.Replace:
                    // FIXME handle multi-item replace event
                    if (e.OldItems is null || e.OldItems.Count == 0 ||
                        e.NewItems is null || e.NewItems.Count == 0) throw new InvalidOperationException();
                    var n = e.NewItems[0];
                    var o = e.OldItems[0];
                    if (n is null ||
                        o is null) throw new InvalidOperationException();
                    var replaceChange = new CollectionReplaceChange(instance, propertyName, (IList) collection,
                        e.NewStartingIndex, n, o);
                    ret.Add(replaceChange);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    if (ThrowExceptionOnCollectionResets)
                        throw new NotSupportedException(
                            "Undoing collection resets is not supported via the CollectionChanged event. The collection is already null, so the Undo system has no way to capture the set of elements that were previously in the collection.");
                    else
                        break;

                    //IList collectionClone = collection.GetType().GetConstructor(new Type[] { collection.GetType() }).Invoke(new object[] { collection }) as IList;

                    //var resetChange = new DelegateChange(
                    //                        instance,
                    //                        () =>
                    //                        {
                    //                            for (int i = 0; i < collectionClone.Count; i++) //for instead foreach to preserve the order
                    //                                ((IList)collection).Add(collectionClone[i]);
                    //                        },
                    //                        () => collection.GetType().GetMethod("Clear").Invoke(collection, null),
                    //                        new ChangeKey<object, string, object>(instance, propertyName, collectionClone)
                    //                    );
                    //ret.Add(resetChange);
                    //break;

                default:
                    throw new NotSupportedException();
            }

            return ret;
        }

        /// <summary>
        ///     Construct a Change instance with actions for undo / redo.
        ///     Returns True, if is Undoing or Redoing, otherwise False.
        /// </summary>
        /// <param name="instance">The instance that changed.</param>
        /// <param name="propertyName">
        ///     The property name that exposes the collection that changed. (Case sensitive, used by
        ///     reflection.)
        /// </param>
        /// <param name="collection">The collection that had an item added / removed.</param>
        /// <param name="e">The NotifyCollectionChangedEventArgs event args parameter, with info about the collection change.</param>
        public virtual bool OnCollectionChanged(object instance, string propertyName, object collection,
            NotifyCollectionChangedEventArgs e)
        {
            return OnCollectionChanged(instance, propertyName, collection, e, propertyName);
        }

        /// <summary>     
        ///     Construct a Change instance with actions for undo / redo.
        ///     Returns True, if is Undoing or Redoing, otherwise False.
        /// </summary>
        /// <param name="instance">The instance that changed.</param>
        /// <param name="propertyName">
        ///     The property name that exposes the collection that changed. (Case sensitive, used by
        ///     reflection.)
        /// </param>
        /// <param name="collection">The collection that had an item added / removed.</param>
        /// <param name="e">The NotifyCollectionChangedEventArgs event args parameter, with info about the collection change.</param>
        /// <param name="descriptionOfChange">A description of the change.</param>
        public virtual bool OnCollectionChanged(object instance, string propertyName, object collection,
            NotifyCollectionChangedEventArgs e, string descriptionOfChange)
        {
            ISupportsUndo? supportsUndo = instance as ISupportsUndo;
            if (null == supportsUndo)
                return false;

            object? root = supportsUndo.GetUndoRoot();
            if (null == root)
                return false;

            // Add the changes to the UndoRoot
            UndoRoot undoRoot = UndoService.Current[root];

            if (undoRoot.IsUndoingOrRedoing) return true;

            // Create the Change instances.
            IList<Change>? changes = GetCollectionChange(instance, propertyName, collection, e);
            if (null == changes)
                return false;

            foreach (Change change in changes)
            {
                undoRoot.AddChange(change, descriptionOfChange);
            }

            return false;
        }

        public bool ThrowExceptionOnCollectionResets
        {
            get { return _throwExceptionOnCollectionResets; }
            set { _throwExceptionOnCollectionResets = value; }
        }

        #endregion

        #region private fields

        private bool _throwExceptionOnCollectionResets = true;

        #endregion
    }
}