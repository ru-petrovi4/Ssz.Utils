using System.Collections.Generic;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.VisualEditors;
using Ssz.Utils;

namespace Ssz.Operator.Design.Core.Controls
{
    public class GroupViewModel : ViewModelBase
    {
        #region construction and destruction

        public GroupViewModel()
        {
            ChildGroups = new List<GroupViewModel>();
            Entities = new List<EntityInfoViewModel>();
            IsExpanded = true;
        }

        #endregion

        #region public functions

        public string Header
        {
            get { return _header; }
            set { SetValue(ref _header, value); }
        }

        public IList<GroupViewModel> ChildGroups { get; set; }

        public IList<EntityInfoViewModel> Entities { get; set; }

        public IList<object> Items
        {
            get
            {
                IList<object> childNodes = new List<object>();

                foreach (EntityInfoViewModel entity in Entities)
                    childNodes.Add(entity);
                foreach (GroupViewModel group in ChildGroups)
                    childNodes.Add(group);

                return childNodes;
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetValue(ref _isExpanded, value); }
        }

        public bool IsEmpty()
        {
            return Entities.Count == 0 && ChildGroups.Count == 0;
        }

        #endregion

        #region private fields

        private string _header = @"";
        private bool _isExpanded;

        #endregion
    }

    public static class GroupViewModelExtensions
    {
        #region public functions

        public static void InitializeSelectionService<T>(this GroupViewModel groupViewModel,
            SelectionService<T> selectionService)
            where T: class, ISelectable
        {
            foreach (EntityInfoViewModel entity in groupViewModel.Entities)
                selectionService.Attach((entity as T)!);
            foreach (GroupViewModel group in groupViewModel.ChildGroups)
                group.InitializeSelectionService(selectionService);
        }

        #endregion
    }
}