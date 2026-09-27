using Avalonia.Media;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     The content a shape shows: the XAML that holds it, what it is, and how it fills the shape.
    /// </summary>
    public class ConstContentEditorViewModel : ViewModelBase
    {
        #region public functions

        public string Xaml
        {
            get => _xaml;
            set
            {
                if (_xaml == value) return;
                _xaml = value;

                ContentPreview = XamlHelper.GetContentPreview(_xaml, out var contentDesc,
                    out Stretch contentStretch);
                ContentDesc = contentDesc;

                if (contentStretch != _contentStretch)
                {
                    _contentStretch = contentStretch;
                    OnPropertyChanged(nameof(ContentStretchComboBoxSelectedItem));
                }
            }
        }

        public object? ContentPreview
        {
            get => _contentPreview;
            set => SetProperty(ref _contentPreview, value);
        }

        public string ContentDesc
        {
            get => _contentDesc;
            set => SetProperty(ref _contentDesc, value);
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
