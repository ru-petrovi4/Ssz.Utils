using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Constants
{
    public class DsConstantViewModel : ViewModelBase
    {
        #region construction and destruction

        public DsConstantViewModel() : this(new DsConstant())
        {
        }

        public DsConstantViewModel(DsConstant dsConstant)
        {
            DsConstant = dsConstant;

            RefreshValuesItemsSource();

            Refresh_Foreground_ToolTip_BackgroundHintVisibility();
        }

        #endregion

        #region public functions

        public string Name
        {
            get => DsConstant.Name;
            set
            {
                value = "%(" + value.Replace("%(", "").Replace(")", "") + ")";
                if (DsConstant.Name == value) return;
                DsConstant.Name = value;
                OnPropertyChangedAuto();
            }
        }

        public string Desc
        {
            get => DsConstant.Desc;
            set
            {
                if (DsConstant.Desc == value) return;
                DsConstant.Desc = value;
                OnPropertyChangedAuto();
            }
        }

        public string Type
        {
            get => DsConstant.Type;
            set
            {
                if (DsConstant.Type == value) return;
                DsConstant.Type = value;

                RefreshValuesItemsSource();

                Refresh_Foreground_ToolTip_BackgroundHintVisibility();

                OnPropertyChangedAuto();
            }
        }

        public string Value
        {
            get => DsConstant.Value;
            set
            {
                if (DsConstant.Value == value) return;
                DsConstant.Value = value;

                Refresh_Foreground_ToolTip_BackgroundHintVisibility();

                OnPropertyChangedAuto();
            }
        }

        public Brush Foreground
        {
            get => _foreground;
            set => SetValue(ref _foreground, value);
        }

        public string? ToolTip
        {
            get => _toolTip;
            set => SetValue(ref _toolTip, value);
        }

        public Visibility BackgroundHintVisibility
        {
            get => _backgroundHintVisibility;
            set => SetValue(ref _backgroundHintVisibility, value);
        }

        public ConstantValueViewModel[]? ValuesItemsSource
        {
            get => _valuesItemsSource;
            set => SetValue(ref _valuesItemsSource, value);
        }

        public DsConstant DsConstant { get; }

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Desc) &&
                   string.IsNullOrWhiteSpace(Type) &&
                   string.IsNullOrWhiteSpace(Value);
        }

        #endregion

        #region private functions

        private void RefreshValuesItemsSource()
        {
            ValuesItemsSource = (ConstantValueViewModel[])
                DsProject.Instance.GetConstantValuesForDropDownList(DsConstant.Type).Clone();
        }

        private void Refresh_Foreground_ToolTip_BackgroundHintVisibility()
        {
            if (DsConstant.IsDsProjectDsConstant)
                BackgroundHintVisibility = Visibility.Hidden;
            else
                BackgroundHintVisibility = DsConstant.Value == @"" ? Visibility.Visible : Visibility.Hidden;

            if (DsConstant.Value != @"" && _valuesItemsSource is not null && _valuesItemsSource.Length > 0)
            {
                var existing = _valuesItemsSource.FirstOrDefault(
                    pvd =>
                        pvd.Value == DsConstant.Value);
                if (existing is not null)
                {
                    Foreground = _normalBrush;
                    ToolTip = existing.Desc;
                    return;
                }

                Foreground = _errorBrush;
                ToolTip = Resources.ConstantValueNotInListWarning;
                return;
            }

            Foreground = _normalBrush;
            ToolTip = null;
        }

        #endregion

        #region private fields

        private Brush _foreground = null!;
        private string? _toolTip;
        private Visibility _backgroundHintVisibility;
        private readonly Brush _normalBrush = new SolidColorBrush(Colors.Black);
        private readonly Brush _errorBrush = new SolidColorBrush(Colors.Red);
        private ConstantValueViewModel[]? _valuesItemsSource;

        #endregion
    }
}