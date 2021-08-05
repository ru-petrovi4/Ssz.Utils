using System.Collections;

namespace Ssz.Utils.MonitoredUndo.Changes
{
    public abstract class CollectionAddRemoveChangeBase : CollectionChange
    {
        #region construction and destruction

        public CollectionAddRemoveChangeBase(object target, string propertyName, IList collection, int index,
            object element)
            : base(target, propertyName, collection)
            // new ChangeKey<object, string, object>(target, propertyName, element)
        {
            _element = element;
            _index = index;

            RedoElement = element;
            RedoIndex = index;
        }

        #endregion

        #region public functions

        public override void MergeWith(Change latestChange)
        {
            var other = latestChange as CollectionAddRemoveChangeBase;

            if (null != other)
            {
                RedoElement = other.RedoElement;
                RedoIndex = other.RedoIndex;
            }
        }

        public object Element
        {
            get { return _element; }
        }

        public int Index
        {
            get { return _index; }
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

        protected string DebuggerDisplay
        {
            get
            {
                return string.Format(
                    "{4}(Property={0}, Target={{{1}}}, Index={2}, Element={{{3}}})",
                    PropertyName, Target, Index, Element, GetType().Name);
            }
        }

        protected object RedoElement;
        protected int RedoIndex;

        #endregion

        #region private fields

        private readonly object _element;
        private readonly int _index;

        #endregion
    }
}