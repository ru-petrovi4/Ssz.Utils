using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core
{
    public class DataBindingItemViewModel : ViewModelBase
    {
        #region private fields

        private int _index;

        #endregion

        #region construction and destruction

        public DataBindingItemViewModel()
        {
            DataBindingItem = new DataBindingItem();
        }

        public DataBindingItemViewModel(DataBindingItem dataBindingItem)
        {
            DataBindingItem = dataBindingItem;
        }

        #endregion

        #region public functions

        public DataBindingItem DataBindingItem { get; }

        public int Index
        {
            get => _index;
            set => SetValue(ref _index, value);
        }

        public DataSourceType Type
        {
            get => DataBindingItem.Type;
            set
            {
                if (Equals(value, DataBindingItem.Type)) return;
                DataBindingItem.Type = value;
                OnPropertyChangedAuto();
            }
        }

        public string IdString
        {
            get => DataBindingItem.IdString;
            set
            {
                value = value.Trim();
                if (Equals(value, DataBindingItem.IdString)) return;
                DataBindingItem.IdString = value;
                OnPropertyChangedAuto();
            }
        }

        public string DefaultValue
        {
            get => DataBindingItem.DefaultValue;
            set
            {
                value = value.Trim();
                if (Equals(value, DataBindingItem.DefaultValue)) return;
                DataBindingItem.DefaultValue = value;
                OnPropertyChangedAuto();
            }
        }

        public bool IsEmpty()
        {
            switch (DataBindingItem.Type)
            {
                case DataSourceType.Constant:
                case DataSourceType.AlarmUnacked:
                case DataSourceType.AlarmCategory:
                case DataSourceType.AlarmBrush:
                case DataSourceType.RootWindowNum:
                case DataSourceType.Random:
                case DataSourceType.CurrentTimeSeconds:
                case DataSourceType.AlarmsCount:
                case DataSourceType.BuzzerState:
                case DataSourceType.BuzzerIsEnabled:
                    return false;
                default:
                    return string.IsNullOrEmpty(DataBindingItem.IdString);
            }
        }

        #endregion
    }
}