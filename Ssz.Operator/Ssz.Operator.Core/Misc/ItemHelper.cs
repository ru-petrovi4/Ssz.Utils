using System.Collections.Generic;
using System.Reflection;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public static class ItemHelper
    {
        #region public functions

        public static void ReplaceConstants(IDsItem? item, IDsContainer? container)
        {
            if (item is null) return;

            item.ReplaceConstants(container);
        }

        public static void ReplaceConstants<T>(List<T> list, IDsContainer? container)
            where T : IDsItem
        {
            foreach (var item in list)
            {
                item.ReplaceConstants(container);
            }            
        }

        public static void RefreshForPropertyGrid(IDsItem? item, IDsContainer? container)
        {
            if (item is null) return;

            item.RefreshForPropertyGrid(container);
        }

        public static void OnPropertyGridRefreshInFields(object? item, IDsContainer? container)
        {
            if (item is null) 
                return;

            IEnumerable<FieldInfo> fields = ObjectHelper.GetAllFields(item);
            foreach (FieldInfo field in fields)
                if (typeof(IDsItem).IsAssignableFrom(field.FieldType))
                {
                    var dsItem = field.GetValue(item) as IDsItem;
                    if (dsItem is null) continue;
                    RefreshForPropertyGrid(dsItem, container);
                }
        }

        #endregion
    }
}