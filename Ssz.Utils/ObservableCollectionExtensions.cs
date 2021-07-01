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
        ///     Preconditions: source and destination are ordered by Id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void Update<T>(this ObservableCollection<T> source, T[] destination)
            where T : IObservableCollectionItem
        {
            foreach (T s in source)
            {
                s.IsDeleted = true;
            }

            foreach (T d in destination)
            {
                var s = source.FirstOrDefault(i => String.Equals(i.Id, d.Id, StringComparison.InvariantCultureIgnoreCase));
                if (s == null)
                {
                    d.IsAdded = true;
                }
                else
                {
                    d.IsAdded = false;
                    s.IsDeleted = false;
                    s.Update(d);
                }
            }
            
            for (int index = source.Count - 1; index >= 0; index--)
            {
                if (source[index].IsDeleted)
                    source.RemoveAt(index);
            }

            for (int index = 0; index < destination.Length; index++)
            {
                var d = destination[index];
                if (d.IsAdded)
                    source.Insert(index, d);
            }
        }

        #endregion
    }

    public interface IObservableCollectionItem
    {
        string Id { get; }

        bool IsDeleted { get; set; }

        bool IsAdded { get; set; }

        void Update(IObservableCollectionItem item);
    }
}
