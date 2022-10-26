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
        ///     Preconditions: newCollection is Uninitialized.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oldCollection"></param>
        /// <param name="newCollection"></param>
        public static void Update<T>(this ObservableCollection<T> oldCollection, T[] newCollection)
            where T : IObservableCollectionItem
        {
            foreach (T o in oldCollection)
            {
                o.ObservableCollectionItem_IsDeleted = true;
            }

            foreach (T n in newCollection)
            {
                var o = oldCollection.FirstOrDefault(i => String.Equals(i.ObservableCollectionItem_Id, n.ObservableCollectionItem_Id, StringComparison.InvariantCultureIgnoreCase));
                if (o is null)
                {
                    n.ObservableCollectionItem_IsAdded = true;
                }
                else
                {
                    n.ObservableCollectionItem_IsAdded = false;
                    o.ObservableCollectionItem_IsDeleted = false;
                    o.Update(n);
                }
            }
            
            for (int oldCollectionIndex = oldCollection.Count - 1; oldCollectionIndex >= 0; oldCollectionIndex -= 1)
            {
                if (oldCollection[oldCollectionIndex].ObservableCollectionItem_IsDeleted)
                {
                    var o = oldCollection[oldCollectionIndex];
                    oldCollection.RemoveAt(oldCollectionIndex);
                    o.Close();
                }
            }

            for (int newCollectionIndex = 0; newCollectionIndex < newCollection.Length; newCollectionIndex++)
            {
                var n = newCollection[newCollectionIndex];
                if (n.ObservableCollectionItem_IsAdded)
                {
                    int oldCollectionIndex = oldCollection.Count - 1;
                    while (oldCollectionIndex >= 0)
                    {
                        if (String.Compare(oldCollection[oldCollectionIndex].ObservableCollectionItem_Id, n.ObservableCollectionItem_Id) < 0)
                            break;
                        oldCollectionIndex -= 1;
                    }
                    oldCollectionIndex += 1;
                    n.Initialize();
                    oldCollection.Insert(oldCollectionIndex, n);                    
                }                    
            }
        }

        public static void SafeClear<T>(this ObservableCollection<T> collection)
            where T : IObservableCollectionItem
        {
            for (int collectionIndex = collection.Count - 1; collectionIndex >= 0; collectionIndex -= 1)
            {
                var o = collection[collectionIndex];
                collection.RemoveAt(collectionIndex);
                o.Close();
            }
        }

        #endregion
    }

    public interface IObservableCollectionItem
    {
        /// <summary>
        ///     Need implementation for comparison.
        /// </summary>
        string ObservableCollectionItem_Id { get; }

        /// <summary>
        ///     Used by the framework.
        /// </summary>
        bool ObservableCollectionItem_IsDeleted { get; set; }

        /// <summary>
        ///     Used by the framework.
        /// </summary>
        bool ObservableCollectionItem_IsAdded { get; set; }

        void Initialize();

        /// <summary>
        ///     Need implementation for updating.
        /// </summary>
        /// <param name="item"></param>
        void Update(IObservableCollectionItem item);

        void Close();
    }
}
