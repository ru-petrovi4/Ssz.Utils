/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Collections.Generic;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    internal class WeakDictionary<K, V> where K : class
    {
        #region constructors

        #endregion

        #region Private Methods

        private void CollectGarbage()
        {
            var vIndex = 0;

            do
            {
                vIndex = _keys.FindIndex(vIndex, k => !k.IsAlive);
                if (vIndex >= 0)
                {
                    _keys.RemoveAt(vIndex);
                    _values.RemoveAt(vIndex);
                }
            } while (vIndex >= 0);
        }

        #endregion

        #region Members

        private readonly List<WeakReference> _keys = new();
        private readonly List<V> _values = new();

        #endregion

        #region Public Methods

        public V this[K key]
        {
            get
            {
                V valueToReturn;
                if (!GetValue(key, out valueToReturn))
                    throw new ArgumentException();
                return valueToReturn;
            }
            set => SetValue(key, value);
        }

        public bool ContainsKey(K key)
        {
            CollectGarbage();
            return -1 != _keys.FindIndex(k => k.GetValueOrDefault<K>() == key);
        }

        public void SetValue(K key, V value)
        {
            CollectGarbage();
            var vIndex = _keys.FindIndex(k => k.GetValueOrDefault<K>() == key);
            if (vIndex > -1)
            {
                _values[vIndex] = value;
            }
            else
            {
                _values.Add(value);
                _keys.Add(new WeakReference(key));
            }
        }

        public bool GetValue(K key, out V value)
        {
            CollectGarbage();
            var vIndex = _keys.FindIndex(k => k.GetValueOrDefault<K>() == key);
            value = default;
            if (vIndex == -1)
                return false;
            value = _values[vIndex];
            return true;
        }

        #endregion
    }
}