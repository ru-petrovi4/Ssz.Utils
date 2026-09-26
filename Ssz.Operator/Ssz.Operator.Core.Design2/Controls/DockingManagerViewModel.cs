using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Design.Controls
{
    public class DockingManagerViewModel : ViewModelBase
    {
        #region construction and destruction

        public DockingManagerViewModel()
        {
            AnchorablesSource = new DockViewModel[] {
                DsPagesListDockViewModel,
                DsShapesListDockViewModel,
                DsDrawingDsShapesDockViewModel
            };

            DesignDsProjectViewModel.Instance.PropertyChanged += DesignDsProjectViewModel_OnPropertyChanged;
        }

        #endregion

        #region public functions

        public static DockingManagerViewModel Instance { get; } = new();

        public ObservableCollection<DockViewModel> DocumentsSource { get; } = new();

        public IEnumerable<DockViewModel> AnchorablesSource { get; }

        public object? ActiveContent
        {
            get { return _activeContent; }
            set
            {
                if (SetValue(ref _activeContent, value))
                {
                    if (_activeContent is DesignDrawingViewModel designDrawingViewModel)
                    {
                        DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel = designDrawingViewModel;
                        return;
                    }
                    var designDrawingViewModels = DocumentsSource.OfType<DesignDrawingViewModel>().ToArray();
                    if (designDrawingViewModels.Length == 0)
                    {
                        DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel = null;
                    }
                }
            }
        }

        public DsPagesListDockViewModel DsPagesListDockViewModel { get; } = new();

        public DsShapesListDockViewModel DsShapesListDockViewModel { get; } = new();

        public DrawingDsShapesDockViewModel DsDrawingDsShapesDockViewModel { get; } = new();

        #endregion

        #region private functions

        private void DesignDsProjectViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DesignDsProjectViewModel.FocusedDesignDrawingViewModel))
            {
                var focusedDesignDrawingViewModel = DesignDsProjectViewModel.Instance.FocusedDesignDrawingViewModel;
                if (focusedDesignDrawingViewModel is not null && !focusedDesignDrawingViewModel.IsDisposed)
                {
                    SetValue(ref _activeContent, focusedDesignDrawingViewModel, nameof(ActiveContent));
                }
            }
        }

        #endregion        

        #region private fields

        private object? _activeContent;

        #endregion
    }
}
