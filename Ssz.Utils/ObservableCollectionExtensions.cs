using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public static class ObservableCollectionExtensions
    {
        #region public functions

        /// <summary>
        ///     Preconditions: oldCollection and newCollection are ordered by Id.
        ///     Preconditions: newCollection is Uninitialized.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oldCollection"></param>
        /// <param name="newCollection"></param>
        /// <param name="cancellationToken"></param>
        public static void Update<T>(this ObservableCollection<T> oldCollection, T[] newCollection, CancellationToken cancellationToken)
            where T : IObservableCollectionItem
        {
            foreach (T o in oldCollection)
            {
                o.ObservableCollectionItemIsDeleted = true;
            }

            foreach (T n in newCollection)
            {
                var o = oldCollection.FirstOrDefault(i => String.Equals(i.ObservableCollectionItemId, n.ObservableCollectionItemId, StringComparison.InvariantCultureIgnoreCase));
                if (o is null)
                {
                    n.ObservableCollectionItemIsAdded = true;
                }
                else
                {
                    n.ObservableCollectionItemIsAdded = false;
                    o.ObservableCollectionItemIsDeleted = false;
                    o.ObservableCollectionItemUpdate(n);
                }
            }
            
            for (int oldCollectionIndex = oldCollection.Count - 1; oldCollectionIndex >= 0; oldCollectionIndex -= 1)
            {
                var o = oldCollection[oldCollectionIndex];
                if (o.ObservableCollectionItemIsDeleted)
                {                    
                    oldCollection.RemoveAt(oldCollectionIndex);
                    try
                    {
                        o.Close();
                    }
                    catch
                    {
                    }
                }
            }
            
            for (int newCollectionIndex = 0; newCollectionIndex < newCollection.Length; newCollectionIndex++)
            {
                var n = newCollection[newCollectionIndex];
                if (n.ObservableCollectionItemIsAdded)
                {
                    int oldCollectionIndex = oldCollection.Count - 1;
                    while (oldCollectionIndex >= 0)
                    {
                        if (String.Compare(oldCollection[oldCollectionIndex].ObservableCollectionItemId, n.ObservableCollectionItemId) < 0)
                            break;
                        oldCollectionIndex -= 1;
                    }
                    oldCollectionIndex += 1;
                    try
                    {
                        n.Initialize(cancellationToken);
                    }
                    catch
                    {
                    }                                   
                    oldCollection.Insert(oldCollectionIndex, n);                    
                }                    
            }            
        }

        public static void SafeClear<T>(this ObservableCollection<T> collection)
            where T : IObservableCollectionItem
        {
            List<T> collectionToClose = new (collection);

            for (int collectionIndex = collection.Count - 1; collectionIndex >= 0; collectionIndex -= 1)
            {                
                collection.RemoveAt(collectionIndex);                
            }

            foreach (var it in collectionToClose)
            {
                try
                {
                    it.Close();
                }
                catch
                {
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
        string ObservableCollectionItemId { get; }

        /// <summary>
        ///     Used by the framework.
        /// </summary>
        bool ObservableCollectionItemIsDeleted { get; set; }

        /// <summary>
        ///     Used by the framework.
        /// </summary>
        bool ObservableCollectionItemIsAdded { get; set; }

        void Initialize(CancellationToken cancellationToken);

        /// <summary>
        ///     Need implementation for updating.
        /// </summary>
        /// <param name="item"></param>
        void ObservableCollectionItemUpdate(IObservableCollectionItem item);

        void Close();
    }
}
