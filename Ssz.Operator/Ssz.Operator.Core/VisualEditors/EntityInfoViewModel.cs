using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors
{
    public class EntityInfoViewModel : ViewModelBase, ISelectable
    {
        #region construction and destruction

        public EntityInfoViewModel(EntityInfo entityInfo)
        {
            _entityInfo = entityInfo;

            if (entityInfo.PreviewImageBytes is not null)
                try
                {
                    var previewImage = new BitmapImage();
                    previewImage.BeginInit();
                    previewImage.StreamSource = new MemoryStream(entityInfo.PreviewImageBytes);
                    previewImage.EndInit();
                    PreviewImage = previewImage;
                }
                catch (Exception)
                {
                }

            OnEntityInfoChanged();
        }

        #endregion

        #region protected functions

        protected virtual void OnEntityInfoChanged()
        {
            Header = GetNameAndDesc();
            if (!string.IsNullOrWhiteSpace(EntityInfo.Desc))
                ToolTip = EntityInfo.Desc;
            else
                ToolTip = null;
        }

        #endregion

        #region public functions

        public string Header
        {
            get => _header;
            set => SetValue(ref _header, value);
        }

        public string? ToolTip
        {
            get => _toolTip;
            set => SetValue(ref _toolTip, value);
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
            set => SetValue(ref _isSelected, value);
        }

        public bool IsFirstSelected
        {
            get => _isFirstSelected;
            set => SetValue(ref _isFirstSelected, value);
        }

        public ImageSource? PreviewImage { get; }

        public Visibility PreviewImageVisibility => PreviewImage is not null ? Visibility.Visible : Visibility.Collapsed;


        public string GetDescOrName()
        {
            if (string.IsNullOrWhiteSpace(EntityInfo.Desc)) return EntityInfo.Name;
            return EntityInfo.Desc;
        }


        public string GetNameAndDesc()
        {
            if (string.IsNullOrWhiteSpace(EntityInfo.Desc) ||
                EntityInfo.Desc == EntityInfo.Name) return EntityInfo.Name;
            return EntityInfo.Name + " [" + EntityInfo.Desc + "]";
        }

        public override string ToString()
        {
            return GetNameAndDesc();
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