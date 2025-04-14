#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ssz.Operator.Core.Utils
{
    /// <summary>
    ///     A generic List that would only use object's reference,
    ///     ignoring any <see cref="IEquatable{T}" /> or <see cref="object.Equals(object)" />  overrides.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class ReferenceEqualityList<T> : IList<T>, IList, IReadOnlyList<T>
    {
        // Constructs a List. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to 16, and then increased in multiples of two as required.

        #region construction and destruction

        public ReferenceEqualityList()
        {
            _items = _emptyArray;
        }

        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        // 
        public ReferenceEqualityList(int capacity)
        {
            if (capacity < 0) throw new ArgumentException("capacity");

            if (capacity == 0)
                _items = _emptyArray;
            else
                _items = new T[capacity];
        }

        // Constructs a List, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        // 
        public ReferenceEqualityList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            var c = collection as ICollection<T>;
            if (c != null)
            {
                int count = c.Count;
                if (count == 0)
                {
                    _items = _emptyArray;
                }
                else
                {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _size = 0;
                _items = _emptyArray;
                // This enumerable could be empty.  Let Add allocate a new array, if needed.
                // Note it will also go to _defaultCapacity first, not 1, then 2, etc.

                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Add(en.Current);
                    }
                }
            }
        }

        #endregion

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal 
        // array of the list is reallocated to the given capacity.
        // 

        #region public functions

        public int Capacity
        {
            get { return _items.Length; }
            set
            {
                if (value < _size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        var newItems = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, 0, newItems, 0, _size);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        _items = _emptyArray;
                    }
                }
            }
        }

        // Read-only property describing how many elements are in the List.
        public int Count
        {
            get { return _size; }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }


        // Is this List read-only?
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        // Is this List synchronized (thread-safe)?
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        // Synchronization root for this object.
        Object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // Sets or Gets the element at the given index.
        // 
        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint) index >= (uint) _size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return _items[index];
            }

            set
            {
                if ((uint) index >= (uint) _size)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _items[index] = value;
                _version += 1;
            }
        }

        Object IList.this[int index]
        {
            get { return this[index]; }
            set
            {
                try
                {
                    this[index] = (T) value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException();
                }
            }
        }

        /// <summary>
        ///     Adds the given object to the end of this list. The size of the list is
        ///     increased by one. If required, the capacity of the list is doubled
        ///     before adding the new element.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {            
            if (_size == _items.Length) 
                EnsureCapacity(_size + 1);            
            _items[_size] = item;
            _size += 1;
            _version += 1;
        }

        int IList.Add(Object item)
        {
            try
            {
                Add((T) item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException();
            }

            return Count - 1;
        }


        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection)
        {
            InsertRange(_size, collection);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }

        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search.
        // 
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (_size - index < count)
                throw new ArgumentException();

            return Array.BinarySearch(_items, index, count, item, comparer);
        }

        public int BinarySearch(T item)
        {
            return BinarySearch(0, Count, item, null);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, Count, item, comparer);
        }


        // Clears the contents of List.
        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size);
                // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
                _size = 0;
            }
            _version += 1;
        }

        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // item.Equals().
        //
        public bool Contains(T item)
        {
            if (item == null)
            {
                for (int i = 0; i < _size; i += 1)
                    if (_items[i] == null)
                        return true;
                return false;
            }
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            for (int i = 0; i < _size; i += 1)
            {
                if (c.Equals(_items[i], item)) return true;
            }
            return false;
        }

        bool IList.Contains(Object item)
        {
            if (IsCompatibleObject(item))
            {
                return Contains((T) item);
            }
            return false;
        }

        public ReferenceEqualityList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null)
            {
                throw new ArgumentNullException("converter");
            }
            // @


            var list = new ReferenceEqualityList<TOutput>(_size);
            for (int i = 0; i < _size; i += 1)
            {
                list._items[i] = converter(_items[i]);
            }
            list._size = _size;
            return list;
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
            {
                throw new ArgumentException();
            }


            try
            {
                // Array.Copy will check for NULL.
                Array.Copy(_items, 0, array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException();
            }
        }

        // Copies a section of this list to the given array at the given index.
        // 
        // The method uses the Array.Copy method to copy the elements.
        // 
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (_size - index < count)
            {
                throw new ArgumentException();
            }


            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.

        public bool Exists(Predicate<T> match)
        {
            return FindIndex(match) != -1;
        }

        public T Find(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }


            for (int i = 0; i < _size; i += 1)
            {
                if (match(_items[i]))
                {
                    return _items[i];
                }
            }
            return default(T);
        }

        public List<T> FindAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }


            var list = new List<T>();
            for (int i = 0; i < _size; i += 1)
            {
                if (match(_items[i]))
                {
                    list.Add(_items[i]);
                }
            }
            return list;
        }

        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, _size, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return FindIndex(startIndex, _size - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if ((uint) startIndex > (uint) _size)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            if (count < 0 || startIndex > _size - count)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i += 1)
            {
                if (match(_items[i])) return i;
            }
            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }


            for (int i = _size - 1; i >= 0; i--)
            {
                if (match(_items[i]))
                {
                    return _items[i];
                }
            }
            return default(T);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return FindLastIndex(_size - 1, _size, match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            if (_size == 0)
            {
                // Special case for 0 length List
                if (startIndex != -1)
                {
                    throw new ArgumentOutOfRangeException("startIndex");
                }
            }
            else
            {
                // Make sure we're not out of range            
                if ((uint) startIndex >= (uint) _size)
                {
                    throw new ArgumentOutOfRangeException("startIndex");
                }
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            int endIndex = startIndex - count;
            for (int i = startIndex; i > endIndex; i--)
            {
                if (match(_items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }


            int version = _version;

            for (int i = 0; i < _size; i += 1)
            {
                if (version != _version)
                {
                    break;
                }
                action(_items[i]);
            }

            if (version != _version)
                throw new InvalidOperationException();
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        //
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <internalonly />
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public ReferenceEqualityList<T> GetRange(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (_size - index < count)
            {
                throw new ArgumentException();
            }

            var list = new ReferenceEqualityList<T>(count);
            Array.Copy(_items, index, list._items, 0, count);
            list._size = count;
            return list;
        }

        /// <summary>
        ///     Returns the index of the first occurrence of a given value in a range of
        ///     this list. The list is searched forwards from beginning to end.
        ///     The elements of the list are compared to the given value using the
        ///     Object.ReferenceEquals method.
        /// </summary>
        public int IndexOf(T item)
        {
            return IndexOf(_items, item, 0, _size);
        }

        int IList.IndexOf(Object item)
        {
            if (IsCompatibleObject(item))
            {
                return IndexOf((T) item);
            }
            return -1;
        }


        /// <summary>
        ///     Returns the index of the first occurrence of a given value in a range of
        ///     this list. The list is searched forwards, starting at index
        ///     index and ending at count number of elements. The
        ///     elements of the list are compared to the given value using the
        ///     Object.ReferenceEquals method.
        /// </summary>
        public int IndexOf(T item, int index)
        {
            if (index > _size)
                throw new ArgumentOutOfRangeException("index");

            return IndexOf(_items, item, index, _size - index);
        }

        /// <summary>
        ///     Returns the index of the first occurrence of a given value in a range of
        ///     this list. The list is searched forwards, starting at index
        ///     index and upto count number of elements. The
        ///     elements of the list are compared to the given value using the
        ///     Object.ReferenceEquals method.
        /// </summary>
        public int IndexOf(T item, int index, int count)
        {
            if (index > _size)
                throw new ArgumentOutOfRangeException("index");

            if (count < 0 || index > _size - count) throw new ArgumentOutOfRangeException();

            return IndexOf(_items, item, index, count);
        }

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint) index > (uint) _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size += 1;
            _version += 1;
        }

        void IList.Insert(int index, Object item)
        {
            try
            {
                Insert(index, (T) item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException();
            }
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if ((uint) index > (uint) _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }


            var c = collection as ICollection<T>;
            if (c != null)
            {
                // if collection is ICollection<T>
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_size + count);
                    if (index < _size)
                    {
                        Array.Copy(_items, index, _items, index + count, _size - index);
                    }

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c)
                    {
                        // Copy first part of _items to insert location
                        Array.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(_items, index + count, _items, index*2, _size - index);
                    }
                    else
                    {
                        var itemsToInsert = new T[count];
                        c.CopyTo(itemsToInsert, 0);
                        itemsToInsert.CopyTo(_items, index);
                    }
                    _size += count;
                }
            }
            else
            {
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Insert(index += 1, en.Current);
                    }
                }
            }
            _version += 1;
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list 
        // are compared to the given value using the Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item)
        {
            if (_size == 0)
            {
                // Special case for empty list
                return -1;
            }
            return LastIndexOf(item, _size - 1, _size);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index)
        {
            if (index >= _size)
                throw new ArgumentOutOfRangeException("index");

            return LastIndexOf(item, index, index + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index, int count)
        {
            if ((Count != 0) && (index < 0))
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if ((Count != 0) && (count < 0))
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (_size == 0)
            {
                // Special case for empty list
                return -1;
            }

            if (index >= _size)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (count > index + 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            return Array.LastIndexOf(_items, item, index, count);
        }
        
        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        void IList.Remove(Object item)
        {
            if (IsCompatibleObject(item))
            {
                Remove((T) item);
            }
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).   
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            int freeIndex = 0; // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < _size && !match(_items[freeIndex])) freeIndex += 1;
            if (freeIndex >= _size) return 0;

            int current = freeIndex + 1;
            while (current < _size)
            {
                // Find the first item which needs to be kept.
                while (current < _size && match(_items[current])) current += 1;

                if (current < _size)
                {
                    // copy item to the free slot.
                    _items[freeIndex += 1] = _items[current += 1];
                }
            }

            Array.Clear(_items, freeIndex, _size - freeIndex);
            int result = _size - freeIndex;
            _size = freeIndex;
            _version += 1;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public void RemoveAt(int index)
        {
            if ((uint) index >= (uint) _size)
            {
                throw new ArgumentOutOfRangeException();
            }

            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T);
            _version += 1;
        }

        // Removes a range of elements from this list.
        // 
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (_size - index < count)
                throw new ArgumentException();


            if (count > 0)
            {
                int i = _size;
                _size -= count;
                if (index < _size)
                {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                Array.Clear(_items, _size, count);
                _version += 1;
            }
        }

        // Reverses the elements in this list.
        public void Reverse()
        {
            Reverse(0, Count);
        }

        // Reverses the elements in a range of this list. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        // 
        // This method uses the Array.Reverse method to reverse the
        // elements.
        // 
        public void Reverse(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (_size - index < count)
                throw new ArgumentException();

            Array.Reverse(_items, index, count);
            _version += 1;
        }

        // Sorts the elements in this list.  Uses the default comparer and 
        // Array.Sort.
        public void Sort()
        {
            Sort(0, Count, null);
        }

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        // 
        // This method uses the Array.Sort method to sort the elements.
        // 
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (_size - index < count)
                throw new ArgumentException();


            Array.Sort(_items, index, count, comparer);
            _version += 1;
        }

        /*
        public void Sort(Comparison<T> comparison) {
            if( comparison == null) {
                throw new ArgumentNullException("comparison");
            }
            
 
            if( _size > 0) {
                IComparer<T> comparer = new Array.FunctorComparer<T>(comparison);
                Array.Sort(_items, 0, _size, comparer);
            }
        }*/

        // ToArray returns a new Object array containing the contents of the List.
        // This requires copying the List, which is an O(n) toolkitOperation.
        public T[] ToArray()
        {
            var array = new T[_size];
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }

        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        // 
        // list.Clear();
        // list.TrimExcess();
        // 
        public void TrimExcess()
        {
            var threshold = (int) (_items.Length*0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
        }

        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }


            for (int i = 0; i < _size; i += 1)
            {
                if (!match(_items[i]))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region internal functions

        internal static IList<T> Synchronized(ReferenceEqualityList<T> list)
        {
            return new SynchronizedList(list);
        }

        #endregion

        #region private functions

        private static bool IsCompatibleObject(object value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            return ((value is T) || (value == null && default(T) == null));
        }

        private static int IndexOf(T[] array, T value, int startIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            int lb = array.GetLowerBound(0);
            if (startIndex < lb || startIndex > array.Length + lb)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0 || count > array.Length - startIndex + lb)
                throw new ArgumentOutOfRangeException("count");
            if (array.Rank != 1)
                throw new RankException();

            int endIndex = startIndex + count;

            if (ReferenceEquals(value, null))
            {
                for (int i = startIndex; i < endIndex; i += 1)
                {
                    if (ReferenceEquals(array[i], null)) return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i += 1)
                {
                    if (ReferenceEquals(array[i], value)) return i;
                }
            }

            // Return one less than the lower bound of the array.  This way, 
            // for arrays with a lower bound of -1 we will not return -1 when the 
            // item was not found.  And for SZArrays (the vast majority), -1 still
            // works for them. 
            return lb - 1;
        }

        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length*2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                var elementSize = typeof (T).IsValueType ? Marshal.SizeOf(typeof (T)) : Marshal.SizeOf(typeof (IntPtr));
                int maxArrayLength = 2147483591 / elementSize; // max element count
                if ((uint) newCapacity > maxArrayLength) newCapacity = maxArrayLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        #endregion

        #region private fields

        private static readonly T[] _emptyArray = new T[0];
        private T[] _items;
        [ContractPublicPropertyName("Count")] private int _size;
        private int _version;
        [NonSerialized] private Object _syncRoot;
        private const int _defaultCapacity = 4;

        #endregion

        [Serializable]
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReferenceEqualityList<T> list;
            private int index;
            private readonly int version;
            private T current;

            internal Enumerator(ReferenceEqualityList<T> list)
            {
                this.list = list;
                index = 0;
                version = list._version;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ReferenceEqualityList<T> localList = list;

                if (version == localList._version && ((uint) index < (uint) localList._size))
                {
                    current = localList._items[index];
                    index += 1;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException();
                }

                index = list._size + 1;
                current = default(T);
                return false;
            }

            public T Current
            {
                get { return current; }
            }

            Object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list._size + 1)
                    {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException();
                }

                index = 0;
                current = default(T);
            }
        }

        [Serializable]
        internal class SynchronizedList : IList<T>
        {
            #region construction and destruction

            internal SynchronizedList(ReferenceEqualityList<T> list)
            {
                _list = list;
                _root = ((ICollection) list).SyncRoot;
            }

            #endregion

            #region public functions

            public int Count
            {
                get
                {
                    lock (_root)
                    {
                        return _list.Count;
                    }
                }
            }

            public bool IsReadOnly
            {
                get { return ((ICollection<T>) _list).IsReadOnly; }
            }

            public T this[int index]
            {
                get
                {
                    lock (_root)
                    {
                        return _list[index];
                    }
                }
                set
                {
                    lock (_root)
                    {
                        _list[index] = value;
                    }
                }
            }

            public void Add(T item)
            {
                lock (_root)
                {
                    _list.Add(item);
                }
            }

            public void Clear()
            {
                lock (_root)
                {
                    _list.Clear();
                }
            }

            public bool Contains(T item)
            {
                lock (_root)
                {
                    return _list.Contains(item);
                }
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                lock (_root)
                {
                    _list.CopyTo(array, arrayIndex);
                }
            }

            public bool Remove(T item)
            {
                lock (_root)
                {
                    return _list.Remove(item);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                lock (_root)
                {
                    return _list.GetEnumerator();
                }
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                lock (_root)
                {
                    return ((IEnumerable<T>) _list).GetEnumerator();
                }
            }

            public int IndexOf(T item)
            {
                lock (_root)
                {
                    return _list.IndexOf(item);
                }
            }

            public void Insert(int index, T item)
            {
                lock (_root)
                {
                    _list.Insert(index, item);
                }
            }

            public void RemoveAt(int index)
            {
                lock (_root)
                {
                    _list.RemoveAt(index);
                }
            }

            #endregion

            #region private fields

            private readonly ReferenceEqualityList<T> _list;
            private readonly Object _root;

            #endregion
        }
    }
}