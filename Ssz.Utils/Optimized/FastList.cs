using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils;

public class FastList<T>
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

    public void Add(T item)
    {
        if (_count >= _items.Length)
            Resize(_items.Length * 2);

        _items[_count++] = item;
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

    #endregion    

    #region private fields

    private const int DefaultCapacity = 16;

    private T[] _items;

    private int _count;

    #endregion
}
