using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     What a list of the editor shows for one entity: its header, its tooltip and, when the entity
    ///     carries one, its preview image.
    /// </summary>
    public class EntityInfoViewModel : ViewModelBase, ISelectable
    {
        #region construction and destruction

        public EntityInfoViewModel(EntityInfo entityInfo)
        {
            _entityInfo = entityInfo;

            if (entityInfo.PreviewImageBytes is not null)
                try
                {
                    PreviewImage = new Bitmap(new MemoryStream(entityInfo.PreviewImageBytes));
                }
                catch (Exception)
                {
                    // A preview that cannot be decoded is not worth failing the list over.
                }

            OnEntityInfoChanged();
        }

        #endregion

        #region public functions

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public string? ToolTip
        {
            get => _toolTip;
            set => SetProperty(ref _toolTip, value);
        }

        public EntityInfo EntityInfo
        {
            get => _entityInfo;
            set
            {
                _entityInfo = value;
                OnEntityInfoChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (!SetProperty(ref _isSelected, value)) return;
                OnPropertyChanged(nameof(SelectionBackground));
                OnPropertyChanged(nameof(SelectionForeground));
            }
        }

        public bool IsFirstSelected
        {
            get => _isFirstSelected;
            set
            {
                if (!SetProperty(ref _isFirstSelected, value)) return;
                OnPropertyChanged(nameof(SelectionBackground));
                OnPropertyChanged(nameof(SelectionForeground));
            }
        }

        /// <summary>
        ///     What the row is painted with when it is selected. The item first selected is the one a
        ///     command works from, so it is shown apart from the rest of the selection.
        ///     <para>
        ///         The WPF lists did this with two multi-value converters over IsSelected and
        ///         IsFirstSelected; a pair of properties says the same thing and binds plainly.
        ///     </para>
        /// </summary>
        public IBrush SelectionBackground => SelectionBrushes.Background(_isSelected, _isFirstSelected);

        public IBrush SelectionForeground => SelectionBrushes.Foreground(_isSelected, _isFirstSelected);

        public IImage? PreviewImage { get; }

        /// <summary>
        ///     The WPF version exposed a Visibility here; Avalonia binds IsVisible to a bool.
        /// </summary>
        public bool HasPreviewImage => PreviewImage is not null;

        public string GetDescOrName()
        {
            if (String.IsNullOrWhiteSpace(EntityInfo.Desc)) return EntityInfo.Name;
            return EntityInfo.Desc;
        }

        public string GetNameAndDesc()
        {
            if (String.IsNullOrWhiteSpace(EntityInfo.Desc) ||
                EntityInfo.Desc == EntityInfo.Name) return EntityInfo.Name;
            return EntityInfo.Name + " [" + EntityInfo.Desc + "]";
        }

        public override string ToString()
        {
            return GetNameAndDesc();
        }

        #endregion

        #region protected functions

        protected virtual void OnEntityInfoChanged()
        {
            Header = GetNameAndDesc();
            if (!String.IsNullOrWhiteSpace(EntityInfo.Desc))
                ToolTip = EntityInfo.Desc;
            else
                ToolTip = null;
        }

        #endregion

        #region private fields


        private string _header = @"";
        private string? _toolTip;
        private bool _isSelected;
        private bool _isFirstSelected;
        private EntityInfo _entityInfo;

        #endregion
    }
}
