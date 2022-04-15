using System.Collections;
using System.Diagnostics;

namespace Ssz.Utils.Net4.MonitoredUndo.Changes
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class CollectionAddChange : CollectionAddRemoveChangeBase
    {
        #region construction and destruction

        public CollectionAddChange(object target, string propertyName, IList collection, int index, object element)
            : base(target, propertyName, collection, index, element)
        {
        }

        #endregion

        #region protected functions

        protected override void PerformUndo()
        {
            Collection.Remove(Element);
        }

        protected override void PerformRedo()
        {
            Collection.Insert(RedoIndex, RedoElement);
        }

        #endregion
    }
}