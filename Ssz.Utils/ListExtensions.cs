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
            out List<T> baseUniqueCollection, out List<T> baseIntersectionCollection, out List<T> otherUniqueCollection,
            IEqualityComparer<T> equalityComparer)
        {
            baseUniqueCollection = new List<T>(baseCollection.Count);
            baseIntersectionCollection = new List<T>(baseCollection.Count);
            otherUniqueCollection = new List<T>(otherCollection.Count);
            otherUniqueCollection.AddRange(otherCollection);

            //var otherParallelQuery = otherUniqueCollection.AsParallel();            
            foreach (T baseElement in baseCollection)
            {
                //T? existingOtherElement = ParallelEnumerable.FirstOrDefault(otherParallelQuery, i => equalityComparer.Equals(i, baseElement));
                int index = otherUniqueCollection.FindIndex(i => equalityComparer.Equals(i, baseElement));
                if (index != -1)
                {
                    otherUniqueCollection.RemoveAt(index);                    
                    baseIntersectionCollection.Add(baseElement);
                }                    
                else
                {
                    baseUniqueCollection.Add(baseElement);
                }                    
            }
        }

        #endregion
    }
}
