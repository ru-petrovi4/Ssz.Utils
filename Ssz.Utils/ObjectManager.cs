using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ssz.Utils
{
    /// <summary>
    ///     Manage list of objects, allows access through handlers, designed for effective insertion and deletion
    /// </summary>    
    public class ObjectManager<T> :
        IEnumerable<T>,
        IEnumerable<KeyValuePair<UInt32, T>>
        where T : class
    {
        #region construction and destruction
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        public ObjectManager(int capacity)
        {
            if (capacity < 1) capacity = 1;
            _items = new ValueWrapper[capacity];
            _items[0] = new ValueWrapper();
            _valueWrapperCount = 1;
            Count = 0;
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        ///     Reserve space in the internal list of objects
        /// </summary>
        public void Reserve(int capacity)
        {
            if (capacity > _items.Length)
            {
                ValueWrapper[] oldItems = _items;
                _items = new ValueWrapper[capacity];
                oldItems.CopyTo(_items, 0);
            }
        }

        /// <summary>
        ///     Sets the capacity to the actual number of elements in the internal list of objects,
        ///     if that number is less than a threshold value.
        /// </summary>
        public void TrimExcess()
        {
            if (_valueWrapperCount == _items.Length) return;
            ValueWrapper[] oldItems = _items;
            _items = new ValueWrapper[_valueWrapperCount];
            oldItems.CopyTo(_items, 0); // TODO: Verify
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public UInt32 Add(T value)
        {
            return IndexToHandle(AddInternal(value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public bool Contains(UInt32 handle)
        {
            int index = HandleToIndex(handle);

            return index != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(UInt32 handle, [MaybeNullWhen(false)] out T value)
        {
            int index = HandleToIndex(handle);
            if (index == 0)
            {
                value = null;
                return false;
            }
            value = _items[index].Value;
            if (value is null) throw new InvalidOperationException();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public bool Remove(UInt32 handle)
        {
            int index = HandleToIndex(handle);
            if (index == 0) return false;

            RemoveInternal(index);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Assign(UInt32 handle, T value)
        {
            int index = HandleToIndex(handle);
            if (index == 0) return false;

            _items[index].Value = value;

            return true;
        }

        /// <summary>
        ///     Releases all objects from objects from internal list. Its handlers become invalid
        /// </summary>
        public void Clear()
        {
            if (_valueWrapperCount == 1) return;

            ValueWrapper item0 = _items[0];
            item0.NextIndex = 0;
            item0.PrevIndex = 1;

            for (int index = 1; index < _valueWrapperCount; index++)
            {
                ValueWrapper item = _items[index];
                item.Value = null;
                item.NextIndex = index - 1;
                item.PrevIndex = index + 1 < _valueWrapperCount ? index + 1 : 0;
            }

            Count = 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<KeyValuePair<UInt32, T>> IEnumerable<KeyValuePair<UInt32, T>>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<T> ToList()
        {
            var result = new List<T>(Count);

            foreach (T obj in this)
            {
                result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            var result = new T[Count];

            int i = 0;
            foreach (T obj in this)
            {
                result[i] = obj;
                i++;
            }

            return result;
        }

        /// <summary>
        /// </summary>
        public T this[UInt32 handle]
        {
            get
            {
                int index = HandleToIndex(handle);

                if (index == 0)
                {
                    throw new ArgumentException();
                }
                else
                {
                    T? value = _items[index].Value;
                    if (value is null) throw new ArgumentException();
                    return value;
                }
            }
            set
            {
                int index = HandleToIndex(handle);

                if (index == 0)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _items[index].Value = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt32[] Handles
        {
            get
            {
                var result = new UInt32[Count];

                int i = 0;
                foreach (KeyValuePair<uint, T> kvp in (this as IEnumerable<KeyValuePair<UInt32, T>>))
                {
                    result[i] = kvp.Key;
                    i++;
                }

                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count { get; private set; }

        #endregion

        #region private functions

        /// <summary>
        ///     Makes instance-specific handler from index
        ///     index must be valid
        /// </summary>
        private UInt32 IndexToHandle(int index)
        {
            return ((UInt32)(_items[index].InstanceId) << 24) + (UInt32)index;
        }

        /// <summary>
        ///     Verifyes that handle corresponds instance of object and returns index of object.
        ///     Returns 0, if invalid handle.
        /// </summary>
        private int HandleToIndex(UInt32 handle)
        {
            if (handle == 0) return 0;
            var id = (byte)(handle >> 24);
            var index = (int)(handle & 0xFFFFFF);
            if (index >= _valueWrapperCount) return 0;
            ValueWrapper item = _items[index];
            if (item.Value is null) return 0;
            if (id != item.InstanceId) return 0;
            return index;
        }

        /// <summary>
        ///     Puts object in internal list and returns its index there.        
        /// </summary>
        private int AddInternal(T value)
        {
            ValueWrapper item0 = _items[0];

            if (item0.PrevIndex == 0)
            {
                if (_valueWrapperCount == _items.Length)
                {
                    ValueWrapper[] oldItems = _items;
                    _items = new ValueWrapper[oldItems.Length*2];
                    oldItems.CopyTo(_items, 0);
                }
                _items[_valueWrapperCount] = new ValueWrapper();
                item0.PrevIndex = _valueWrapperCount;
                _valueWrapperCount++;
            }

            int currentIndex = item0.PrevIndex;

            ValueWrapper currentItem = _items[currentIndex];

            int nextIndex = currentItem.NextIndex;
            int prevIndex = currentItem.PrevIndex;
            int nextIndex0 = item0.NextIndex;
            int prevIndex0 = item0.PrevIndex;

            currentItem.Value = value;            
            if (currentItem.InstanceId == 0xFF) currentItem.InstanceId = 1;
            else currentItem.InstanceId++;
            currentItem.NextIndex = nextIndex0;
            currentItem.PrevIndex = 0;

            item0.NextIndex = currentIndex;
            item0.PrevIndex = prevIndex;

            if (prevIndex > 0)
            {
                _items[prevIndex].NextIndex = 0;
            }

            if (nextIndex0 > 0)
            {
                _items[nextIndex0].PrevIndex = currentIndex;
            }

            Count++;

            return currentIndex;
        }

        /// <summary>
        ///     Releases object from internal list. Its handle becomes invalid.
        ///     index must be valid
        /// </summary>
        private void RemoveInternal(int index)
        {
            int nextIndex = _items[index].NextIndex;
            int prevIndex = _items[index].PrevIndex;
            int nextIndex0 = _items[0].NextIndex;
            int prevIndex0 = _items[0].PrevIndex;

            ValueWrapper item = _items[index];
            item.Value = null;
            item.NextIndex = 0;
            item.PrevIndex = prevIndex0;

            _items[0].PrevIndex = index;

            if (prevIndex0 > 0)
            {
                _items[prevIndex0].NextIndex = index;
            }

            _items[prevIndex].NextIndex = nextIndex;

            if (nextIndex > 0)
            {
                _items[nextIndex].PrevIndex = prevIndex;
            }

            Count--;
        }

        #endregion

        #region private fields

        private ValueWrapper[] _items;
        private int _valueWrapperCount;

        #endregion

        private class ValueWrapper
        {
            #region public functions

            public T? Value; // { get; set; }
            public UInt16 InstanceId; // { get; set; }            
            public int NextIndex; // { get; set; }        
            public int PrevIndex; // { get; set; }

            #endregion
        }

        private class Enumerator : IEnumerator<T>,
            IEnumerator<KeyValuePair<UInt32, T>>
        {
            #region construction and destruction

            public Enumerator(ObjectManager<T> objectManager)
            {
                _objectManager = objectManager;
                _currentIndex = 0;
            }

            public void Dispose()
            {
            }

            #endregion

            #region public functions

            public void Reset()
            {
                // TODO throw new InvalidOperationException();
                _currentIndex = 0;
            }

            public bool MoveNext()
            {
                // TODO throw new InvalidOperationException();
                _currentIndex = _objectManager._items[_currentIndex].NextIndex;
                if (_currentIndex == 0) return false;
                return true;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_currentIndex == 0) throw new InvalidOperationException();
                    T? value = _objectManager._items[_currentIndex].Value;
                    if (value is null) throw new InvalidOperationException();
                    return value;
                }
            }

            T IEnumerator<T>.Current
            {
                get
                {
                    if (_currentIndex == 0) throw new InvalidOperationException();
                    T? value = _objectManager._items[_currentIndex].Value;
                    if (value is null) throw new InvalidOperationException();
                    return value;
                }
            }

            KeyValuePair<UInt32, T> IEnumerator<KeyValuePair<UInt32, T>>.Current
            {
                get
                {
                    if (_currentIndex == 0) throw new InvalidOperationException();
                    T? value = _objectManager._items[_currentIndex].Value;
                    if (value is null) throw new InvalidOperationException();                    
                    return new KeyValuePair<UInt32, T>(_objectManager.IndexToHandle(_currentIndex),
                        value);
                }
            }

            #endregion

            #region private fields

            private readonly ObjectManager<T> _objectManager;
            private int _currentIndex;

            #endregion
        }
    }
}