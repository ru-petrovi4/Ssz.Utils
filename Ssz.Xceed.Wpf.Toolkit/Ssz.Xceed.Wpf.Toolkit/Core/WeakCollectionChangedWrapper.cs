/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Collections;
using System.Collections.Specialized;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.Core
{
    internal class WeakCollectionChangedWrapper : IList, ICollection, INotifyCollectionChanged
    {
        private readonly IList _innerList;
        private WeakEventListener<NotifyCollectionChangedEventArgs> _innerListListener;

        public WeakCollectionChangedWrapper(IList sourceList)
        {
            _innerList = sourceList;
            var notifyList = _innerList as INotifyCollectionChanged;
            if (notifyList is not null)
            {
                _innerListListener = new WeakEventListener<NotifyCollectionChangedEventArgs>(OnInnerCollectionChanged);
                CollectionChangedEventManager.AddListener(notifyList, _innerListListener);
            }
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        #endregion

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnInnerCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (CollectionChanged is not null) CollectionChanged(this, args);
        }

        internal void ReleaseEvents()
        {
            if (_innerListListener is not null)
            {
                CollectionChangedEventManager.RemoveListener((INotifyCollectionChanged) _innerList, _innerListListener);
                _innerListListener = null;
            }
        }

        #region IList Members

        int IList.Add(object value)
        {
            return _innerList.Add(value);
        }

        void IList.Clear()
        {
            _innerList.Clear();
        }

        bool IList.Contains(object value)
        {
            return _innerList.Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return _innerList.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            _innerList.Insert(index, value);
        }

        bool IList.IsFixedSize => _innerList.IsFixedSize;

        bool IList.IsReadOnly => _innerList.IsReadOnly;

        void IList.Remove(object value)
        {
            _innerList.Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            _innerList.RemoveAt(index);
        }

        object IList.this[int index]
        {
            get => _innerList[index];
            set => _innerList[index] = value;
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            _innerList.CopyTo(array, index);
        }

        int ICollection.Count => _innerList.Count;

        bool ICollection.IsSynchronized => _innerList.IsSynchronized;

        object ICollection.SyncRoot => _innerList.SyncRoot;

        #endregion
    }
}