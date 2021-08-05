using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ssz.Utils.MonitoredUndo
{
    public class UndoService
    {
        #region public functions

        /// <summary>
        ///     Stores the "Current Instance" of a given object or document so that the rest of the model can access it.
        /// </summary>
        /// <typeparam name="T">The type of the root instance to store.</typeparam>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static object GetCurrentDocumentInstance<T>() where T : class
        {
            if (null == _currentRootInstances)
                return null;

            Type type = typeof (T);
            if (_currentRootInstances.ContainsKey(type))
            {
                WeakReference wr = _currentRootInstances[type];

                if (null == wr || !wr.IsAlive)
                {
                    _currentRootInstances.Remove(type);
                    return null;
                }

                return wr.Target;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///     Stores the "Current Instance" of a given object or document so that the rest of the model can access it.
        /// </summary>
        /// <typeparam name="T">The type of the root instance to store.</typeparam>
        /// <param name="instance">The document or object instance that is the "currently active" instance.</param>
        public static void SetCurrentDocumentInstance<T>(T instance) where T : class
        {
            Type type = typeof (T);

            if (null == _currentRootInstances)
            {
                if (null != instance) // The instance can be null if it is being cleared.
                {
                    _currentRootInstances = new Dictionary<Type, WeakReference>();
                    _currentRootInstances.Add(type, new WeakReference(instance));
                }
            }
            else
            {
                object existing = GetCurrentDocumentInstance<T>();

                if (null == existing && null != instance)
                    _currentRootInstances.Add(type, new WeakReference(instance));
                else if (null != instance)
                    _currentRootInstances[type] = new WeakReference(instance);
                else
                    _currentRootInstances.Remove(type);
            }
        }

        /// <summary>
        ///     Get (or create) the singleton instance of the UndoService.
        /// </summary>
        public static UndoService Current
        {
            get
            {
                if (null == _current)
                    _current = new UndoService();

                return _current;
            }
        }

        /// <summary>
        ///     Tells all UndoRoots that all subsequent changes should be part of a single ChangeSet.
        /// </summary>
        public void BeginChangeSetBatch(string batchDescription, bool consolidateChangesForSameInstance)
        {
            foreach (var r in _roots.Values.ToArray())
            {
                r.BeginChangeSetBatch(batchDescription, consolidateChangesForSameInstance);
            }
        }

        /// <summary>
        ///     Tells all UndoRoots that it can stop collecting Changes into a single ChangeSet.
        /// </summary>
        public void EndChangeSetBatch()
        {
            foreach (var r in _roots.Values.ToArray())
            {
                r.EndChangeSetBatch();
            }
        }

        /// <summary>
        ///     Clear the cached UndoRoots.
        /// </summary>
        public void Clear()
        {
            _roots.Clear();
        }

        public void Clear(object root)
        {
            if (root != null) _roots.Remove(root);
        }

        /// <summary>
        ///     Get (or create) an UndoRoot for the specified object or document instance.
        /// </summary>
        /// <param name="root">The object that represents the root of the document or object hierarchy.</param>
        /// <returns>An UndoRoot instance for this object.</returns>
        public UndoRoot this[object root]
        {
            get
            {
                if (null == root)
                    return DummyUndoRoot;

                UndoRoot ret = null;

                if (_roots.ContainsKey(root))
                    ret = _roots[root];

                if (null == ret)
                {
                    ret = new UndoRoot(root);
                    _roots.Add(root, ret);
                }

                return ret;
            }
        }

        #endregion

        #region private fields

        private static UndoService _current;
        private static IDictionary<Type, WeakReference> _currentRootInstances;
        private static readonly UndoRoot DummyUndoRoot = new UndoRoot(null);
        private readonly IDictionary<object, UndoRoot> _roots = new Dictionary<object, UndoRoot>(ReferenceEqualityComparer<object>.Default);

        #endregion
    }
}