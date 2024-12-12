using Ssz.Utils;
using Ssz.Utils.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ssz.Operator.Core.ControlsDesign
{
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
            get { return _title; }
            set { SetValue(ref _title, value); }
        }

        public ICommand CloseCommand { get; }

        public bool CanClose
        {
            get { return _canClose; }
            set { SetValue(ref _canClose, value); }
        }

        public virtual bool IsSelected
        {
            get { return _isSelected; }
            set { SetValue(ref _isSelected, value); }
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
