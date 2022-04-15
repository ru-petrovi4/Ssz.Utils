using System.Collections.Generic;

namespace Ssz.Utils.Net4
{
    public interface ICacheObject
    {
        void Initialize();

        void Close();
    }
    
    public static class ObjectsCache<T>
        where T : class, ICacheObject, new()
    {
        #region public functions

        /// <summary>
        ///     Calls Initialize() on object.
        /// </summary>
        /// <returns></returns>
        public static T GetObject()
        {
            T cacheObject = null;
            lock (ClosedQueue)
            {
                if (ClosedQueue.Count > 0)
                {
                    cacheObject = ClosedQueue.Dequeue();
                }
            }
            if (cacheObject is null) cacheObject = new T();
            cacheObject.Initialize();
            return cacheObject;
        }

        /// <summary>
        ///     Calls Close() on object.
        /// </summary>
        /// <param name="cacheObject"></param>
        public static void ReturnObject(T cacheObject)
        {
            cacheObject.Close();
            lock (ClosedQueue)
            {                
                ClosedQueue.Enqueue(cacheObject);
            }
        }

        #endregion

        #region private fields

        private static readonly Queue<T> ClosedQueue = new Queue<T>();

        #endregion
    }
}