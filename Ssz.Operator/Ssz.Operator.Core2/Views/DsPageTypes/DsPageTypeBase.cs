using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsPageTypes
{
    public abstract class DsPageTypeBase : OwnedDataSerializableAndCloneable, IUsedAddonsInfo
    {
        #region public functions

        [Browsable(false)] public abstract Guid Guid { get; }

        [Browsable(false)] public abstract string Name { get; }

        [DsCategory(ResourceStrings.SystemCategory)]
        [DsDisplayName(ResourceStrings.DsPageTypeBase_Desc)]
        //[PropertyOrder(1)]
        public abstract string Desc { get; }

        [DsCategory(ResourceStrings.SystemCategory)]
        [DsDisplayName(ResourceStrings.DsPageTypeBase_IsFaceplate)]
        //[PropertyOrder(2)]
        public abstract bool IsFaceplate { get; }

        [Browsable(false)] public virtual string Hint => "";

        public IEnumerable<Guid> GetUsedAddonGuids()
        {
            yield return AddonsManager.GetAddonGuidFromDsPageType(Guid);
        }

        public override string ToString()
        {
            return Name + @" ...";
        }

        #endregion
    }
}