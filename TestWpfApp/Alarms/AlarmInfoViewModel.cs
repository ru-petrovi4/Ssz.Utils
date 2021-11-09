using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Wpf;
using Ssz.WpfHmi.Common.ModelEngines;
using Xi.Contracts.Data;

namespace Ssz.WpfHmi.Common.ControlsRuntime.GenericRuntime
{
    /// <summary>
    /// A ViewModel class (in the MVVM pattern) for displaying OPC AE alarms
    /// </summary>
    /// <remarks>
    /// The AlarmInfo VM extends the AlarmInfoViewModel Base class for display purposes 
    /// </remarks>
    public class AlarmInfoViewModel : AlarmInfoViewModelBase
    {
        #region construction and destruction

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="alarmInfoViewModel">An object of the base class that we are extending</param>
        public AlarmInfoViewModel(AlarmInfoViewModelBase alarmInfoViewModel) :
            base(alarmInfoViewModel)
        {
            _occurrenceTimeString = OccurrenceTime.ToString("dd.MM HH:mm:ss"); 
            
            _alarmTypeBrushes = new AlarmTypeBrushes();

            PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case @"CurrentAlarmCondition":
                        RefreshCurrentAlarmCondition();
                        break;
                    case @"Unacked":
                    case @"CategoryId":
                        RefreshUnackedCategoryId();
                        break;
                }
            };

            RefreshCurrentAlarmCondition();
            RefreshUnackedCategoryId();
        }

        #endregion

        #region public functions        

        public int Num
        {
            get { return _num; }
            set { SetValue(ref _num, value); }
        }

        public string OccurrenceTimeString
        {
            get { return _occurrenceTimeString; }
            set { SetValue(ref _occurrenceTimeString, value); }
        }

        public string ConditionString
        {
            get { return _strCondition; }
            set { SetValue(ref _strCondition, value); }
        }

        public Brush AlarmRectBrush
        {
            get { return _alarmRectBrush; }
            set { SetValue(ref _alarmRectBrush, value); }
        }

        #endregion

        #region private functions

        private void RefreshCurrentAlarmCondition()
        {
            switch (CurrentAlarmCondition)
            {
                case Utils.DataAccess.AlarmCondition.LowLow:
                    ConditionString = @"LL";
                    break;
                case Utils.DataAccess.AlarmCondition.Low:
                    ConditionString = @"L";
                    break;
                case Utils.DataAccess.AlarmCondition.None:
                    ConditionString = @"NR";
                    break;
                case Utils.DataAccess.AlarmCondition.High:
                    ConditionString = @"H";
                    break;
                case Utils.DataAccess.AlarmCondition.HighHigh:
                    ConditionString = @"HH";
                    break;
                default:
                    ConditionString = @"ALM";
                    break;
            }
        }

        private void RefreshUnackedCategoryId()
        {
            if (_alarmTypeBrushes is null)
            {
                AlarmRectBrush = Brushes.Lime;
                return;
            }

            if (AlarmIsUnacked)
            {
                switch (CategoryId)
                {
                    case 0:
                        AlarmRectBrush = _alarmTypeBrushes.AlarmCategory0BlinkingBrush;
                        break;
                    case 1:
                        AlarmRectBrush = _alarmTypeBrushes.AlarmCategory1BlinkingBrush;
                        break;
                    case 2:
                        AlarmRectBrush = _alarmTypeBrushes.AlarmCategory2BlinkingBrush;
                        break;
                    default:
                        AlarmRectBrush = _alarmTypeBrushes.AlarmCategory1BlinkingBrush;
                        break;
                }
            }
            else // Acked
            {
                switch (CategoryId)
                {
                    case 0:
                        AlarmRectBrush = _alarmTypeBrushes.AlarmCategory0Brush;
                        break;
                    case 1:
                        AlarmRectBrush = _alarmTypeBrushes.AlarmCategory1Brush;
                        break;
                    case 2:
                        AlarmRectBrush = _alarmTypeBrushes.AlarmCategory2Brush;
                        break;
                    default:
                        AlarmRectBrush = _alarmTypeBrushes.AlarmCategory1Brush;
                        break;
                }
            }
        }

        #endregion

        #region private fields

        private int _num;
        private string _occurrenceTimeString;
        private string _strCondition = "";
        private Brush _alarmRectBrush = Brushes.Transparent;
        private readonly AlarmTypeBrushes _alarmTypeBrushes;

        #endregion
    }
}
