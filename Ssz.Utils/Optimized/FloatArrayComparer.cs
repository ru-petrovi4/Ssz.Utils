using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Optimized;

public sealed class FloatArrayComparer : IEqualityComparer<float[]>
{
    private readonly int _length;

    public FloatArrayComparer(int length)
    {
        _length = length;
    }

    public bool Equals(float[]? x, float[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.Length != _length || y.Length != _length) return false;

        for (int i = 0; i < _length; i++)
        {
            if (!x[i].Equals(y[i]))   // при необходимости можно сделать своё сравнение с допуском
                return false;
        }

        return true;
    }

    public int GetHashCode(float[] obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));
        if (obj.Length != _length)
            throw new ArgumentException($"Array length must be equal to {_length}", nameof(obj));

        unchecked
        {
            int hash = 17;
            for (int i = 0; i < _length; i++)
            {
                hash = hash * 31 + obj[i].GetHashCode();
            }
            return hash;
        }
    }
}
