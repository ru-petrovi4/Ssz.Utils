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
        public static async Task UpdateAsync<T>(this ObservableCollection<T> oldCollection, T[] newCollection, IDispatcher? dispatcher, CancellationToken cancellationToken)
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

            List<T> collectionToClose = new(oldCollection.Count);
            for (int oldCollectionIndex = oldCollection.Count - 1; oldCollectionIndex >= 0; oldCollectionIndex -= 1)
            {
                var o = oldCollection[oldCollectionIndex];
                if (o.ObservableCollectionItemIsDeleted)
                {
                    collectionToClose.Add(o);
                    oldCollection.RemoveAt(oldCollectionIndex);
                }
            }

            List<T> collectionToInitialize = new(newCollection.Length);
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
                    collectionToInitialize.Add(n);                    
                    oldCollection.Insert(oldCollectionIndex, n);                    
                }                    
            }

            if (dispatcher is null)
            {
                foreach (var it in collectionToClose)
                {
                    await it.ObservableCollectionItemCloseAsync();
                }

                foreach (var it in collectionToInitialize)
                {
                    await it.ObservableCollectionItemInitializeAsync(cancellationToken);
                }
            }
            else
            {
                await dispatcher.InvokeExAsync(async ct =>
                {
                    foreach (var it in collectionToClose)
                    {
                        await it.ObservableCollectionItemCloseAsync();
                    }

                    foreach (var it in collectionToInitialize)
                    {
                        await it.ObservableCollectionItemInitializeAsync(cancellationToken);
                    }

                    return 0;
                });
            }                
        }

        public static async Task SafeClearAsync<T>(this ObservableCollection<T> collection, IDispatcher? dispatcher)
            where T : IObservableCollectionItem
        {
            List<T> collectionToClose = new (collection);

            for (int collectionIndex = collection.Count - 1; collectionIndex >= 0; collectionIndex -= 1)
            {                
                collection.RemoveAt(collectionIndex);                
            }

            if (dispatcher is null)
            {
                foreach (var it in collectionToClose)
                {
                    await it.ObservableCollectionItemCloseAsync();
                }
            }
            else
            {
                await dispatcher.InvokeExAsync(async ct =>
                {
                    foreach (var it in collectionToClose)
                    {
                        await it.ObservableCollectionItemCloseAsync();
                    }

                    return 0;
                });
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

        Task ObservableCollectionItemInitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Need implementation for updating.
        /// </summary>
        /// <param name="item"></param>
        void ObservableCollectionItemUpdate(IObservableCollectionItem item);

        Task ObservableCollectionItemCloseAsync();
    }
}
