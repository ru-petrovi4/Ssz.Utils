using System.Collections.Generic;
using Ssz.Operator.Core.Drawings;

namespace Ssz.Operator.Core.VisualEditors.AddDrawingsFromLibrary
{
    public class ItemViewModel : EntityInfoViewModel
    {
        #region construction and destruction

        public ItemViewModel(EntityInfo entityInfo) :
            base(entityInfo)
        {
            Children = new List<ItemViewModel>();
        }

        #endregion

        #region public functions

        public List<ItemViewModel> Children { get; }

        public bool IsInitiallySelected { get; set; }


        public bool? IsChecked
        {
            get => _isChecked;
            set => SetIsChecked(value, true, true);
        }

        public void GetChecked(List<DrawingInfo> drawingInfos)
        {
            var drawingInfo = EntityInfo as DrawingInfo;
            if (IsChecked == true && drawingInfo is not null) drawingInfos.Add(drawingInfo);
            foreach (ItemViewModel child in Children) child.GetChecked(drawingInfos);
        }

        public void Initialize()
        {
            foreach (ItemViewModel child in Children)
            {
                child._parent = this;
                child.Initialize();
            }
        }

        #endregion

        #region private functions

        private void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _isChecked)
                return;

            _isChecked = value;

            if (updateChildren && _isChecked.HasValue)
                Children.ForEach(c => c.SetIsChecked(_isChecked, true, false));

            if (updateParent && _parent is not null)
                _parent.VerifyCheckState();

            OnPropertyChanged("IsChecked");
        }

        private void VerifyCheckState()
        {
            bool? state = null;
            for (var i = 0; i < Children.Count; i += 1)
            {
                var current = Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }

            SetIsChecked(state, false, true);
        }

        #endregion

        #region private fields

        private bool? _isChecked = false;
        private ItemViewModel? _parent;

        #endregion
    }
}