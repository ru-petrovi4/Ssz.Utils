using System.Windows.Media;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors
{
    internal class ConstContentEditorViewModel : ViewModelBase
    {
        #region construction and destruction

        #endregion

        #region public functions

        public string Xaml
        {
            get => _xaml;
            set
            {
                if (_xaml == value) return;
                _xaml = value;

                string contentDesc;
                Stretch contentStretch;
                ContentPreview = XamlHelper.GetContentPreview(_xaml, out contentDesc, out contentStretch);
                ContentDesc = contentDesc;
                if (contentStretch != _contentStretch)
                {
                    _contentStretch = contentStretch;
                    OnPropertyChanged("ContentStretchComboBoxSelectedItem");
                }
            }
        }

        public object? ContentPreview
        {
            get => _contentPreview;
            set => SetValue(ref _contentPreview, value);
        }

        public string ContentDesc
        {
            get => _contentDesc;
            set => SetValue(ref _contentDesc, value);
        }

        public Stretch ContentStretchComboBoxSelectedItem
        {
            get => _contentStretch;
            set
            {
                if (value != _contentStretch) Xaml = XamlHelper.SetXamlContentStretch(Xaml, value);
            }
        }

        #endregion

        #region private fields

        private string _xaml = @"";
        private object? _contentPreview;
        private Stretch _contentStretch = Stretch.Fill;
        private string _contentDesc = @"";

        #endregion
    }
}