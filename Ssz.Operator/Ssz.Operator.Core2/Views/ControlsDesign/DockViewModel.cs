using System.Windows.Input;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     A pane of the editor: what its tab says and whether it can be closed.
    /// </summary>
    public class DockViewModel : DisposableViewModelBase
    {
        #region construction and destruction

        public DockViewModel(bool canClose)
        {
            _canClose = canClose;
            CloseCommand = new RelayCommand(parameter => OnCloseCommandExecuted(), parameter => _canClose, false);
        }

        #endregion

        #region public functions

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ICommand CloseCommand { get; }

        public bool CanClose
        {
            get => _canClose;
            set => SetProperty(ref _canClose, value);
        }

        public virtual bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        #endregion

        #region protected functions

        protected virtual void OnCloseCommandExecuted()
        {
        }

        #endregion

        #region private fields

        private string _title = @"";
        private bool _canClose;
        private bool _isSelected;

        #endregion
    }
}
