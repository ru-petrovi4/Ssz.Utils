using Avalonia.Media;

using Ssz.Utils;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Constants
{
    public class ConstantValueViewModel : ViewModelBase
    {
        #region private functions

        private void SetIsUsed(bool value)
        {
            if (value == _isUsed) return;
            _isUsed = value;
            OnPropertyChanged("Foreground");
        }

        #endregion

        #region public functions

        public string Value { get; set; } = "";

        public string Desc { get; set; } = "";

        public string ValueToDisplay =>
            Value + (string.IsNullOrWhiteSpace(Desc) || Value.Contains(Desc) ? "" : "\t[" + Desc + "]");

        public SolidColorBrush Foreground =>
            _isUsed
                ? SolidDsBrush.GetSolidColorBrush(Colors.DarkGray)
                : SolidDsBrush.GetSolidColorBrush(Colors.Black);

        public void IncrementUseCount()
        {
            _useCount += 1;
            SetIsUsed(_useCount > 0);
        }

        public void DecrementUseCount()
        {
            _useCount--;
            if (_useCount < 0) _useCount = 0;
            SetIsUsed(_useCount > 0);
        }

        #endregion

        #region private fields

        private bool _isUsed;
        private int _useCount;

        #endregion
    }
}