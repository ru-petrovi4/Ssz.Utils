using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace Ssz.Utils.CommandLine.Infrastructure
{
    internal sealed class ReflectionCache
    {
        #region construction and destruction

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline",
            Justification = "Singleton, by design")]
        static ReflectionCache()
        {
            Singleton = new ReflectionCache();
        }

        private ReflectionCache()
        {
            _cache = new Dictionary<Pair<Type, object>, WeakReference>();
        }

        #endregion

        #region public functions

        public static ReflectionCache Instance
        {
            get { return Singleton; }
        }

        public object? this[Pair<Type, object> key]
        {
            get
            {
                return _cache.ContainsKey(key) ? _cache[key].Target : null;
            }

            set
            {
                _cache[key] = new WeakReference(value);
            }
        }

        #endregion

        #region private fields

        private static readonly ReflectionCache Singleton;
        private readonly IDictionary<Pair<Type, object>, WeakReference> _cache;

        #endregion
    }
}