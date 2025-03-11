using System.Globalization;
using Avalonia.Media;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.DataEngines;
using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.ControlsPlay.GenericPlay
{
    public class GenericDsAlarmInfoViewModel : DsAlarmInfoViewModelBase
    {
        #region construction and destruction

        public GenericDsAlarmInfoViewModel(DsAlarmInfoViewModelBase alarmInfoViewModel) :
            base(alarmInfoViewModel)
        {
            OccurrenceTimeString = OccurrenceTime.ToString("G", CultureInfo.CurrentCulture);            

            Update();
            Update2();

            PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(TagName):
                    case nameof(AlarmIsUnacked):
                    case nameof(CategoryId):
                    case nameof(Priority):
                        Update2();
                        break;
                }
            };
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Invoked when TagName or CurrentAlarmConditionType are changed.
        ///     Possibly updates CurrentAlarmConditionTypeToDisplay, Priority, CategoryId, ActivateBuzzer
        /// </summary>
        public override void Update()
        {
            if (UpdateIsDisabled)
                return;

            switch (CurrentAlarmConditionType)
            {
                case AlarmConditionType.LowLow:
                    CurrentAlarmConditionTypeToDisplay = @"LL";
                    break;
                case AlarmConditionType.Low:
                    CurrentAlarmConditionTypeToDisplay = @"L";
                    break;
                case AlarmConditionType.None:
                    CurrentAlarmConditionTypeToDisplay = @"NR";
                    break;
                case AlarmConditionType.High:
                    CurrentAlarmConditionTypeToDisplay = @"H";
                    break;
                case AlarmConditionType.HighHigh:
                    CurrentAlarmConditionTypeToDisplay = @"HH";
                    break;
                default:
                    CurrentAlarmConditionTypeToDisplay = @"ALM";
                    break;
            }

            base.Update();
        }

        #endregion

        #region private functions

        private void Update2()
        {
            var tagAlarmsBrushes = TagAlarmsInfo.GetTagAlarmsBrushes();

            if (tagAlarmsBrushes.PriorityBrushes is not null)
            {
                if (tagAlarmsBrushes.PriorityBrushes.TryGetValue(Priority, out AlarmBrushes alarmBrushes))
                {
                    if (AlarmIsUnacked)
                        AlarmRectBrush = alarmBrushes.BlinkingBrush;
                    else
                        AlarmRectBrush = alarmBrushes.Brush;
                    return;
                }
            }

            if (AlarmIsUnacked)
                switch (CategoryId)
                {
                    case 0:
                        AlarmRectBrush = tagAlarmsBrushes.AlarmCategory0Brushes.BlinkingBrush;
                        break;
                    case 1:
                        AlarmRectBrush = tagAlarmsBrushes.AlarmCategory1Brushes.BlinkingBrush;
                        break;
                    case 2:
                        AlarmRectBrush = tagAlarmsBrushes.AlarmCategory2Brushes.BlinkingBrush;
                        break;
                    default:
                        AlarmRectBrush = tagAlarmsBrushes.AlarmCategory1Brushes.BlinkingBrush;
                        break;
                }
            else // Acked
                switch (CategoryId)
                {
                    case 0:
                        AlarmRectBrush = tagAlarmsBrushes.AlarmCategory0Brushes.Brush;
                        break;
                    case 1:
                        AlarmRectBrush = tagAlarmsBrushes.AlarmCategory1Brushes.Brush;
                        break;
                    case 2:
                        AlarmRectBrush = tagAlarmsBrushes.AlarmCategory2Brushes.Brush;
                        break;
                    default:
                        AlarmRectBrush = tagAlarmsBrushes.AlarmCategory1Brushes.Brush;
                        break;
                }
        }

        #endregion

        #region public functions

        public int Num
        {
            get => _num;
            set => SetValue(ref _num, value);
        }

        public string OccurrenceTimeString
        {
            get => _occurrenceTimeString;
            set => SetValue(ref _occurrenceTimeString, value);
        }

        public Brush? AlarmRectBrush
        {
            get => _alarmRectBrush;
            set => SetValue(ref _alarmRectBrush, value);
        }

        #endregion

        #region private fields

        private int _num;
        private string _occurrenceTimeString = @"";
        private Brush? _alarmRectBrush;

        #endregion
    }
}