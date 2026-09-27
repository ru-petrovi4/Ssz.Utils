using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ssz.Operator.Core.VisualEditors;

namespace Ssz.Operator.Core.FindReplace
{
    public class SearchResultGroupViewModel : EntityInfoViewModel
    {
        #region construction and destruction

        public SearchResultGroupViewModel(ObservableCollection<SearchResultGroupViewModel> parentCollection,
            EntityInfo entityInfo, IEnumerable<SearchResultViewModel>? searchResults) :
            base(entityInfo)
        {
            ParentCollection = parentCollection;

            SearchResults = new ObservableCollection<SearchResultViewModel>();

            if (searchResults is null) return;

            foreach (SearchResultViewModel searchResult in searchResults)
            {
                searchResult.ParentGroup = this;
                SearchResults.Add(searchResult);
            }
        }

        #endregion

        #region public functions

        public ObservableCollection<SearchResultGroupViewModel> ParentCollection { get; }

        public ObservableCollection<SearchResultViewModel> SearchResults { get; }

        public void Remove(SearchResultViewModel searchResultViewModel)
        {
            if (SearchResults is null) return;

            SearchResults.Remove(searchResultViewModel);

            if (SearchResults.Count == 0 && ParentCollection is not null) ParentCollection.Remove(this);
        }

        #endregion
    }
}