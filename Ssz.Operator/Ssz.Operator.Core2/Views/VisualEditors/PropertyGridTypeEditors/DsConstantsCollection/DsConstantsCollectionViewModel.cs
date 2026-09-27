using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ssz.Operator.Core.Constants;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    /// <summary>
    ///     The constants of a shape or a page while they are being edited.
    ///     <para>
    ///         Ported from the WPF editor. The rows are copies: they are written back into the shape
    ///         only when they say something different, and a change made elsewhere is picked up
    ///         instead of overwritten.
    ///     </para>
    /// </summary>
    public class DsConstantsCollectionViewModel
    {
        #region construction and destruction

        public DsConstantsCollectionViewModel(ObservableCollection<DsConstant> sourceCollection)
        {
            SourceCollection = sourceCollection;
            EditedCollection = new ObservableCollection<DsConstantViewModel>();

            InitializeCollections();
        }

        #endregion

        #region public functions

        public ObservableCollection<DsConstant> SourceCollection { get; }

        public ObservableCollection<DsConstantViewModel> EditedCollection { get; }

        public void Refresh()
        {
            var equals = _sourceCollectionCopy.Count == SourceCollection.Count;
            if (equals)
                for (var i = 0; i < _sourceCollectionCopy.Count; i += 1)
                    if (!_sourceCollectionCopy[i].Equals(SourceCollection[i]))
                    {
                        equals = false;
                        break;
                    }

            if (!equals)
            {
                InitializeCollections();
                return;
            }

            equals = ConstantsHelper.UpdateDsConstants(SourceCollection, EditedCollection
                .Where(vm => !string.IsNullOrWhiteSpace(vm.Name) && !vm.IsEmpty())
                .Select(vm => vm.DsConstant).ToArray());

            if (equals) return;

            _sourceCollectionCopy.Clear();
            foreach (DsConstant dsConstant in SourceCollection)
                _sourceCollectionCopy.Add(new DsConstant(dsConstant));
        }

        #endregion

        #region private functions

        private void InitializeCollections()
        {
            _sourceCollectionCopy.Clear();
            EditedCollection.Clear();

            foreach (DsConstant dsConstant in SourceCollection)
            {
                _sourceCollectionCopy.Add(new DsConstant(dsConstant));
                EditedCollection.Add(new DsConstantViewModel(new DsConstant(dsConstant)));
            }
        }

        #endregion

        #region private fields

        private readonly List<DsConstant> _sourceCollectionCopy = new();

        #endregion
    }
}
