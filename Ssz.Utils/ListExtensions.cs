using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Ssz.Utils
{
    public static class ListExtensions
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCollection"></param>
        /// <param name="otherCollection"></param>
        /// <param name="baseUniqueCollection"></param>
        /// <param name="baseIntersectionCollection"></param>
        /// <param name="otherUniqueCollection"></param>
        /// <param name="equalityComparer"></param>
        public static void Intersect<T>(this List<T> baseCollection, List<T> otherCollection, 
            out List<T> baseUniqueCollection, 
            out List<T> baseIntersectionCollection, 
            out List<T> otherUniqueCollection,
            out List<T> otherIntersectionCollection,
            IEqualityComparer<T> equalityComparer)
        {
            baseUniqueCollection = new List<T>(baseCollection.Count);
            baseIntersectionCollection = new List<T>(baseCollection.Count);
            otherUniqueCollection = new List<T>(otherCollection.Count);
            otherUniqueCollection.AddRange(otherCollection);
            otherIntersectionCollection = new List<T>(baseCollection.Count);

            //var otherParallelQuery = otherUniqueCollection.AsParallel();            
            foreach (T baseElement in baseCollection)
            {
                //T? existingOtherElement = ParallelEnumerable.FirstOrDefault(otherParallelQuery, i => equalityComparer.Equals(i, baseElement));
                int index = otherUniqueCollection.FindIndex(i => equalityComparer.Equals(i, baseElement));
                if (index != -1)
                {
                    otherIntersectionCollection.Add(otherUniqueCollection[index]);
                    otherUniqueCollection.RemoveAt(index);                    
                    baseIntersectionCollection.Add(baseElement);
                }                    
                else
                {
                    baseUniqueCollection.Add(baseElement);
                }                    
            }
        }

        /// <summary>
        ///     Returns -1 if not found.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            var index = 0;
            foreach (var item in source)
            {
                if (predicate.Invoke(item))
                    return index;
                index += 1;
            }
            return -1;
        }

        #endregion
    }
}
