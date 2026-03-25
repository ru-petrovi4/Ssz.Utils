using Ssz.Utils.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ssz.Utils;

public interface IFastList<out T>
{
    //
    // Summary:
    //     Gets the number of elements contained in the System.Collections.Generic.ICollection`1.
    //
    //
    // Returns:
    //     The number of elements contained in the System.Collections.Generic.ICollection`1.
    int Count { get; }
    //
    // Summary:
    //     Gets a value indicating whether the System.Collections.Generic.ICollection`1
    //     is read-only.
    //
    // Returns:
    //     true if the System.Collections.Generic.ICollection`1 is read-only; otherwise,
    //     false.
    bool IsReadOnly { get; }
    
    //
    // Summary:
    //     Removes all items from the System.Collections.Generic.ICollection`1.
    //
    // Exceptions:
    //   T:System.NotSupportedException:
    //     The System.Collections.Generic.ICollection`1 is read-only.
    void Clear();

    //
    // Summary:
    //     Gets or sets the element at the specified index.
    //
    // Parameters:
    //   index:
    //     The zero-based index of the element to get or set.
    //
    // Returns:
    //     The element at the specified index.
    //
    // Exceptions:
    //   T:System.ArgumentOutOfRangeException:
    //     index is not a valid index in the System.Collections.Generic.IList`1.
    //
    //   T:System.NotSupportedException:
    //     The property is set and the System.Collections.Generic.IList`1 is read-only.
    T this[int index] { get; }    
    
    //
    // Summary:
    //     Removes the System.Collections.Generic.IList`1 item at the specified index.
    //
    // Parameters:
    //   index:
    //     The zero-based index of the item to remove.
    //
    // Exceptions:
    //   T:System.ArgumentOutOfRangeException:
    //     index is not a valid index in the System.Collections.Generic.IList`1.
    //
    //   T:System.NotSupportedException:
    //     The System.Collections.Generic.IList`1 is read-only.
    void RemoveAt(int index);
}

public class FastList<T> : IFastList<T>, IList<T>, IReadOnlyList<T>, IOwnedDataSerializable        
{
    #region construction and destruction

    public FastList(int capacity = DefaultCapacity)
    {
        ItemsBuffer = new T[capacity];
        _count = 0;
    }

    public FastList(T[] items)
    {
        ItemsBuffer = items;
        _count = items.Length;
    }

    #endregion

    #region public functions

    public T[] ItemsBuffer;

    public Span<T> Items => ItemsBuffer.AsSpan(0, _count);    

    public int Count => _count;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        if (_count >= ItemsBuffer.Length)
            IncreaseSize(ItemsBuffer.Length == 0 ? DefaultCapacity : ItemsBuffer.Length * 2);

        ItemsBuffer[_count] = item;
        _count += 1;
    }

    public void AddRange(ReadOnlySpan<T> span)
    {
        int required = _count + span.Length;

        if (required > ItemsBuffer.Length)
        {
            int newCapacity = ItemsBuffer.Length == 0 ? DefaultCapacity : ItemsBuffer.Length;
            while (newCapacity < required)
                newCapacity *= 2;

            IncreaseSize(newCapacity);
        }

        span.CopyTo(ItemsBuffer.AsSpan(_count));
        _count += span.Length;
    }

    public void AddRangeOfDefault(int count)
    {
        if (count < 1)
            return;

        int required = _count + count;

        if (required > ItemsBuffer.Length)
        {
            int newCapacity = ItemsBuffer.Length == 0 ? DefaultCapacity : ItemsBuffer.Length;
            while (newCapacity < required)
                newCapacity *= 2;

            IncreaseSize(newCapacity);
        }

        Array.Clear(ItemsBuffer, _count, count);

        _count += count;
    }

    public void Swap(FastList<T> that)
    {
        (that.ItemsBuffer, that._count, ItemsBuffer, _count) = 
            (ItemsBuffer, _count, that.ItemsBuffer, that._count);
    }

    public void Clear()
    {
        _count = 0;
    }

    public T this[int index]
    {
        get => ItemsBuffer[index]; // Без проверок на выход за пределы
        set => ItemsBuffer[index] = value; // Без проверок на выход за пределы
    }

    /// <summary>
    ///     Peconditions: newSize must be greater than old.
    /// </summary>
    /// <param name="newSize"></param>
    private void IncreaseSize(int newSize)
    {
        var newArray = new T[newSize];
        for (int i = 0; i < _count; i++)
            newArray[i] = ItemsBuffer[i];

        ItemsBuffer = newArray;
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf(ItemsBuffer, item, 0, _count);
    }

    public void Insert(int index, T item)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        if (index == _count - 1)
        {
            _count -= 1;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public bool Contains(T item)
    {
        return Array.IndexOf(ItemsBuffer, item, 0, _count) > -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(ItemsBuffer, 0, array, arrayIndex, _count);
    }

    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);    

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    public void SerializeOwnedData(SerializationWriter writer, object? context)
    {
        using (writer.EnterBlock(1))
        {
            writer.WriteOptimized(_count);
            if (typeof(T) == typeof(Int32))
            {
                var items = (ItemsBuffer as int[])!;
                for (int i = 0; i < _count; i++)
                {
                    writer.WriteOptimized(items[i]);
                }
            }
            else if (typeof(T) == typeof(float))
            {
                var items = (ItemsBuffer as float[])!;
                for (int i = 0; i < _count; i++)
                {
                    writer.Write(items[i]);
                }
            }
            else if (typeof(T) == typeof(double))
            {
                var items = (ItemsBuffer as double[])!;
                for (int i = 0; i < _count; i++)
                {
                    writer.Write(items[i]);
                }
            }
            else
            {                
                for (int i = 0; i < _count; i++)
                {
                    writer.WriteObject(ItemsBuffer[i]);
                }
            }
        }
    }

    public void DeserializeOwnedData(SerializationReader reader, object? context)
    {
        using (Block block = reader.EnterBlock())
        {
            switch (block.Version)
            {
                case 1:
                    _count = reader.ReadOptimizedInt32();
                    if (_count >= ItemsBuffer.Length)
                        ItemsBuffer = new T[_count];
                    if (typeof(T) == typeof(Int32))
                    {
                        var items = (ItemsBuffer as int[])!;
                        for (int i = 0; i < _count; i++)
                        {
                            items[i] = reader.ReadOptimizedInt32();
                        }
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        var items = (ItemsBuffer as float[])!;
                        for (int i = 0; i < _count; i++)
                        {
                            items[i] = reader.ReadSingle();
                        }
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        var items = (ItemsBuffer as double[])!;
                        for (int i = 0; i < _count; i++)
                        {
                            items[i] = reader.ReadDouble();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < _count; i++)
                        {
                            ItemsBuffer[i] = (T)reader.ReadObject()!;
                        }
                    }
                    break;
            }
        }
    }    

    #endregion

    #region private fields

    private const int DefaultCapacity = 16;    

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
                _current = localList.ItemsBuffer[_index];
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

public static class FastListExtensions
{
    public static FastList<T> ToFastList<T>(this IEnumerable<T> enumerable)
    {
        return new FastList<T>(enumerable.ToArray());
    }
}
