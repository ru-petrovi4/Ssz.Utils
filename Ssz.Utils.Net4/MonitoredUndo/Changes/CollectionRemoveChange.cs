using System.Collections;
using System.Diagnostics;

namespace Ssz.Utils.MonitoredUndo.Changes
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class CollectionRemoveChange : CollectionAddRemoveChangeBase
    {
        #region construction and destruction

        public CollectionRemoveChange(object target, string propertyName, IList collection, int index, object element)
            : base(target, propertyName, collection, index, element)
        {
        }

        #endregion

        #region protected functions

        protected override void PerformUndo()
        {
            Collection.Insert(Index, Element);
        }

        protected override void PerformRedo()
        {
            Collection.Remove(RedoElement);
        }

        #endregion
    }
}