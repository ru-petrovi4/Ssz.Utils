using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils;

public class FastList<T>
{
    private const int DefaultCapacity = 16;

    private T[] _items;  // Публичный доступ к внутреннему массиву
    public int Count { get; private set; }

    public Span<T> Items => _items.AsSpan(0, Count);

    public FastList(int capacity = DefaultCapacity)
    {
        _items = new T[capacity];
        Count = 0;
    }

    public void Add(T item)
    {
        if (Count >= _items.Length)
            Resize(_items.Length * 2);

        _items[Count++] = item;
    }

    public void AddRange(ReadOnlySpan<T> span)
    {
        int required = Count + span.Length;

        if (required > _items.Length)
        {
            int newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length;
            while (newCapacity < required)
                newCapacity *= 2;

            Resize(newCapacity);
        }

        span.CopyTo(_items.AsSpan(Count));
        Count += span.Length;
    }

    public void Clear()
    {
        Count = 0;
    }

    public T this[int index]
    {
        get => _items[index]; // Без проверок на выход за пределы
        set => _items[index] = value; // Без проверок на выход за пределы
    }

    private void Resize(int newSize)
    {
        var newArray = new T[newSize];
        for (int i = 0; i < Count; i++)
            newArray[i] = _items[i];

        _items = newArray;
    }
}
