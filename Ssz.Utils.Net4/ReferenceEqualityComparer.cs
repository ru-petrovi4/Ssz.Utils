using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ssz.Utils
{
    /// <summary>
    ///     A generic object comparerer that would only use object's reference,
    ///     ignoring any <see cref="IEquatable{T}" /> or <see cref="object.Equals(object)" />  overrides.
    /// </summary>
    public class ReferenceEqualityComparer<T> : EqualityComparer<T>
        where T : class
    {
        #region public functions

        public new static readonly IEqualityComparer<T> Default = new ReferenceEqualityComparer<T>();

        public override bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }

        #endregion
    }
}