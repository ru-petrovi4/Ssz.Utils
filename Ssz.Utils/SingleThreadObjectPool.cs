using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils;

/// <summary>
/// Single thread implementation of <see cref="ObjectPool{T}"/>.
/// </summary>
/// <typeparam name="T">The type to pool objects for.</typeparam>
/// <remarks>This implementation keeps a cache of retained objects. This means that if objects are returned when the pool has already reached "maximumRetained" objects they will be available to be Garbage Collected.</remarks>
public class SingleThreadObjectPool<T> : ObjectPool<T> where T : class
{
    private readonly Func<T> _createFunc;
    private readonly Func<T, bool> _returnFunc;
    private readonly int _maxCapacity;
    private int _numItems;

    private protected readonly Queue<T> _items = new();
    private protected T? _fastItem;    

    /// <summary>
    /// Creates an instance of <see cref="SingleThreadObjectPool{T}"/>.
    /// </summary>
    /// <param name="policy">The pooling policy to use.</param>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public SingleThreadObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
    {
        // cache the target interface methods, to avoid interface lookup overhead
        _createFunc = policy.Create;
        _returnFunc = policy.Return;
        _maxCapacity = maximumRetained - 1;  // -1 to account for _fastItem
    }

    /// <inheritdoc />
    public override T Get()
    {
        var item = _fastItem;
        if (item == null)
        {

#if NETCOREAPP
            if (_items.TryDequeue(out item))
            {
                _numItems -= 1;
                return item;
            }
#else
            if (_items.Count > 0)
            {
                _numItems -= 1;
                return _items.Dequeue();
            }
#endif

            // no object available, so go get a brand new one
            return _createFunc();
        }
        else
        {
            _fastItem = null;
        }

        return item;
    }

    /// <inheritdoc />
    public override void Return(T obj)
    {
        ReturnCore(obj);
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <returns>true if the object was returned to the pool</returns>
    private protected bool ReturnCore(T obj)
    {
        if (!_returnFunc(obj))
        {
            // policy says to drop this object
            return false;
        }

        if (_fastItem != null)
        {
            _numItems += 1;
            if (_numItems <= _maxCapacity)
            {
                _items.Enqueue(obj);
                return true;
            }

            // no room, clean up the count and drop the object on the floor
            _numItems -= 1;
            return false;
        }
        else
        {
            _fastItem = obj;
        }

        return true;
    }
}
