using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public static class ObservableCollectionExtensions
    {
        #region public functions

        /// <summary>
        ///     Preconditions: oldCollection and newCollection are ordered by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oldCollection"></param>
        /// <param name="newCollection"></param>
        public static void Update<T>(this ObservableCollection<T> oldCollection, T[] newCollection)
            where T : IObservableCollectionItem
        {
            foreach (T o in oldCollection)
            {
                o.IsDeleted = true;
            }

            foreach (T n in newCollection)
            {
                var o = oldCollection.FirstOrDefault(i => String.Equals(i.Id, n.Id, StringComparison.InvariantCultureIgnoreCase));
                if (o is null)
                {
                    n.IsAdded = true;
                }
                else
                {
                    n.IsAdded = false;
                    o.IsDeleted = false;
                    o.Update(n);
                }
            }
            
            for (int oldCollectionIndex = oldCollection.Count - 1; oldCollectionIndex >= 0; oldCollectionIndex -= 1)
            {
                if (oldCollection[oldCollectionIndex].IsDeleted)
                    oldCollection.RemoveAt(oldCollectionIndex);
            }

            for (int newCollectionIndex = 0; newCollectionIndex < newCollection.Length; newCollectionIndex++)
            {
                var n = newCollection[newCollectionIndex];
                if (n.IsAdded)
                {
                    int oldCollectionIndex = oldCollection.Count - 1;
                    while (oldCollectionIndex >= 0)
                    {
                        if (String.Compare(oldCollection[oldCollectionIndex].Id, n.Id) < 0)
                            break;
                        oldCollectionIndex -= 1;
                    }
                    oldCollectionIndex += 1;                    
                    oldCollection.Insert(oldCollectionIndex, n);                    
                }
                    
            }
        }

        #endregion
    }

    public interface IObservableCollectionItem
    {
        /// <summary>
        ///     Need implementation for comparison.
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     Used by the framework.
        /// </summary>
        bool IsDeleted { get; set; }

        /// <summary>
        ///     Used by the framework.
        /// </summary>
        bool IsAdded { get; set; }

        /// <summary>
        ///     Need implementation for updating.
        /// </summary>
        /// <param name="item"></param>
        void Update(IObservableCollectionItem item);
    }
}
