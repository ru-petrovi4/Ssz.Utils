using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ssz.Utils;

public class DummyOrderedEnumerable<T> : IOrderedEnumerable<T>
{
    private readonly IEnumerable<T> _source;

    public DummyOrderedEnumerable(IEnumerable<T> source)
    {
        _source = source;
    }

    public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(
        Func<T, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
    {
        // Delegate to OrderBy/OrderByDescending if needed, or just return self if you don't want sorting.
        return this;
    }

    public IEnumerator<T> GetEnumerator() => _source.GetEnumerator();    

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _source.GetEnumerator();
}
