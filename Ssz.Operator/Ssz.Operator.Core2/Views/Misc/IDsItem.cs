using Ssz.Operator.Core.Constants;
using System.Collections.Generic;

namespace Ssz.Operator.Core
{
    public interface IDsItem : IConstantsHolder //, IPropertyGridItem
    {
        IDsItem? ParentItem { get; set; }        

        void ReplaceConstants(IDsContainer? container);

        void RefreshForPropertyGrid(IDsContainer? container);
    }

    public static class ItemExtensions
    {
        #region public functions

        public static T? Find<T>(this IDsItem? item)
            where T : class
        {
            if (item is null) return null;
            var t = item as T;
            if (t is not null) return t;
            return Find<T>(item.ParentItem);
        }

        #endregion
    }
}