using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;

namespace Ssz.Utils
{
    public static class ListExtensions
    {
        #region public functions

        /// <summary>
        ///     baseIntersectionCollection same size and order as otherIntersectionCollection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCollection"></param>
        /// <param name="otherCollection"></param>
        /// <param name="baseUniqueCollection"></param>
        /// <param name="baseIntersectionCollection"></param>
        /// <param name="otherUniqueCollection"></param>
        /// <param name="equalityComparer"></param>
        /// <param name="additionalEqualityComparer"></param>
        public static void Intersect<T>(this List<T> baseCollection, List<T> otherCollection,
            out List<T> baseUniqueCollection,
            out List<T> baseIntersectionCollection,
            out List<T> otherUniqueCollection,
            out List<T> otherIntersectionCollection,
            IEqualityComparer<T> equalityComparer,
            IEqualityComparer<T>? additionalEqualityComparer = null)
        {
            baseUniqueCollection = new List<T>(baseCollection.Count);
            baseIntersectionCollection = new List<T>(baseCollection.Count);
            otherUniqueCollection = new List<T>(otherCollection);
            otherIntersectionCollection = new List<T>(baseCollection.Count);

            //var otherParallelQuery = otherUniqueCollection.AsParallel();
            //T? existingOtherElement = ParallelEnumerable.FirstOrDefault(otherParallelQuery, i => equalityComparer.Equals(i, baseElement));
            if (additionalEqualityComparer is null)
            {
                foreach (T baseElement in baseCollection)
                {   
                    int index = otherUniqueCollection.FindIndex(i => equalityComparer.Equals(i, baseElement));
                    if (index != -1)
                    {
                        baseIntersectionCollection.Add(baseElement);
                        otherIntersectionCollection.Add(otherUniqueCollection[index]);
                        otherUniqueCollection.RemoveAt(index);
                    }
                    else
                    {
                        baseUniqueCollection.Add(baseElement);
                    }
                }
            }
            else
            {
                foreach (T baseElement in baseCollection)
                {                    
                    int index = otherUniqueCollection.FindIndex(i => equalityComparer.Equals(i, baseElement) && additionalEqualityComparer.Equals(i, baseElement));
                    if (index != -1)
                    {
                        baseIntersectionCollection.Add(baseElement);
                        otherIntersectionCollection.Add(otherUniqueCollection[index]);
                        otherUniqueCollection.RemoveAt(index);
                    }
                    else
                    {
                        index = otherUniqueCollection.FindIndex(i => equalityComparer.Equals(i, baseElement));
                        if (index != -1)
                        {
                            baseIntersectionCollection.Add(baseElement);
                            otherIntersectionCollection.Add(otherUniqueCollection[index]);
                            otherUniqueCollection.RemoveAt(index);
                        }
                        else
                        {
                            baseUniqueCollection.Add(baseElement);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     baseIntersectionCollection NOT same size and order as otherIntersectionCollection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseCollection"></param>
        /// <param name="otherCollection"></param>
        /// <param name="baseUniqueCollection"></param>
        /// <param name="baseIntersectionCollection"></param>
        /// <param name="otherUniqueCollection"></param>
        /// <param name="equalityComparer"></param>
        public static void IntersectMultiId<T>(this List<T> baseCollection, List<T> otherCollection, 
            out List<T> baseUniqueCollection, 
            out List<T> baseIntersectionCollection, 
            out List<T> otherUniqueCollection,
            out List<T> otherIntersectionCollection,
            IEqualityComparer<T> equalityComparer)
        {
            baseUniqueCollection = new List<T>(baseCollection.Count);
            baseIntersectionCollection = new List<T>(baseCollection.Count);
            otherUniqueCollection = new List<T>(otherCollection);            
            otherIntersectionCollection = new List<T>(baseCollection.Count);

            //var otherParallelQuery = otherUniqueCollection.AsParallel();            
            foreach (T baseElement in baseCollection)
            {
                //T? existingOtherElement = ParallelEnumerable.FirstOrDefault(otherParallelQuery, i => equalityComparer.Equals(i, baseElement));
                int startIndex = otherUniqueCollection.Count - 1;
                while (true)
                {
                    int index = otherUniqueCollection.FindLastIndex(startIndex, i => equalityComparer.Equals(i, baseElement));
                    if (index != -1)
                    {
                        otherIntersectionCollection.Add(otherUniqueCollection[index]);
                        otherUniqueCollection.RemoveAt(index);
                        baseIntersectionCollection.Add(baseElement);
                        if (index == 0)
                            break;
                        startIndex = index - 1;
                    }
                    else
                    {
                        index = otherIntersectionCollection.FindIndex(i => equalityComparer.Equals(i, baseElement));
                        if (index != -1)
                            baseIntersectionCollection.Add(baseElement);
                        else
                            baseUniqueCollection.Add(baseElement);
                        break;
                    }
                }
            }
        }

        #endregion
    }
}
