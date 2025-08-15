using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils;

public class FastList<T> : IList<T>, IReadOnlyList<T>
{
    #region construction and destruction

    public FastList(int capacity = DefaultCapacity)
    {
        _items = new T[capacity];
        _count = 0;
    }

    #endregion

    #region public functions

    public Span<T> Items => _items.AsSpan(0, _count);

    public int Count => _count;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        if (_count >= _items.Length)
            Resize(_items.Length * 2);

        _items[_count] = item;
        _count += 1;
    }

    public void AddRange(ReadOnlySpan<T> span)
    {
        int required = _count + span.Length;

        if (required > _items.Length)
        {
            int newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length;
            while (newCapacity < required)
                newCapacity *= 2;

            Resize(newCapacity);
        }

        span.CopyTo(_items.AsSpan(_count));
        _count += span.Length;
    }

    public void Swap(FastList<T> that)
    {
        (that._items, that._count, _items, _count) = 
            (_items, _count, that._items, that._count);
    }

    public void Clear()
    {
        _count = 0;
    }

    public T this[int index]
    {
        get => _items[index]; // Без проверок на выход за пределы
        set => _items[index] = value; // Без проверок на выход за пределы
    }

    private void Resize(int newSize)
    {
        var newArray = new T[newSize];
        for (int i = 0; i < _count; i++)
            newArray[i] = _items[i];

        _items = newArray;
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf(_items, item, 0, _count);
    }

    public void Insert(int index, T item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    public bool Contains(T item)
    {
        return Array.IndexOf(_items, item, 0, _count) > -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_items, 0, array, arrayIndex, _count);
    }

    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);    

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    #endregion

    #region private fields

    private const int DefaultCapacity = 16;

    private T[] _items;

    private int _count;

    #endregion

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        internal static IEnumerator<T>? s_emptyEnumerator;

        private readonly FastList<T> _list;
        private int _index;        
        private T? _current;

        internal Enumerator(FastList<T> list)
        {
            _list = list;
            _index = 0;            
            _current = default;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            FastList<T> localList = _list;

            if ((uint)_index < (uint)localList._count)
            {
                _current = localList._items[_index];
                _index++;
                return true;
            }
            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            _index = _list._count + 1;
            _current = default;
            return false;
        }

        public T Current => _current!;

        object? IEnumerator.Current => _current;
        
        void IEnumerator.Reset()
        {   
            _index = 0;
            _current = default;
        }
    }
}
