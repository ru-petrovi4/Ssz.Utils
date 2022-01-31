using System;
using System.Xml.Serialization;
using Ssz.Utils.DataAccess;

namespace Ssz.Utils.DataAccess
{
    public class AlarmInfoViewModelBase : ViewModelBase
    {
        #region construction and destruction
        
        public AlarmInfoViewModelBase()
        {
        }

        /// <summary>
        ///     Copy constructor       
        /// </summary>
        /// <param name="that"></param>
        public AlarmInfoViewModelBase(AlarmInfoViewModelBase that)
        {
            _alarmIsActive = that._alarmIsActive;
            _alarmIsUnacked = that._alarmIsUnacked;
            _occurrenceTime = that._occurrenceTime;
            _timeLastActive = that._timeLastActive;
            _tag = that._tag;
            _propertyPath = that._propertyPath;
            _desc = that._desc;
            _area = that._area;
            _originalAlarmCondition = that._originalAlarmCondition;
            _currentAlarmCondition = that._currentAlarmCondition;
            _alarmConditionType = that._alarmConditionType;
            _categoryId = that._categoryId;
            _priority = that._priority;
            _textMessage = that._textMessage;
            _currentValue = that._currentValue;            
            _currentValueText = that._currentValueText;
            _tripValue = that._tripValue;
            _tripValueText = that._tripValueText;
            EventId = that.EventId;
            OriginalEventMessage = that.OriginalEventMessage;
            _eu = that._eu;
            _isDigital = that._isDigital;
            _tripValue = that._tripValue;
            _tripValueText = that._tripValueText;
            AlarmConditionChanged = that.AlarmConditionChanged;
            UnackedChanged = that.UnackedChanged;
        }

        #endregion

        #region public functions
        
        public virtual bool AlarmIsActive
        {
            get { return _alarmIsActive; }
            set
            {
                SetValue(ref _alarmIsActive, value);                
            }
        }        
        
        public virtual bool AlarmIsUnacked
        {
            get { return _alarmIsUnacked; }
            set
            {
                SetValue(ref _alarmIsUnacked, value);                
            }
        }        
        
        public virtual DateTime OccurrenceTime
        {
            get { return _occurrenceTime; }
            set { SetValue(ref _occurrenceTime, value); }
        }
        
        public virtual DateTime TimeLastActive
        {
            get { return _timeLastActive; }
            set { SetValue(ref _timeLastActive, value); }
        }
        
        /// <summary>
        ///     Calculated propery. When set, assumes dot as parameter separator.
        /// </summary>
        public virtual string ElementId
        {
            get { return Tag + PropertyPath; }
            set
            {
                if (value == @"")
                {
                    Tag = @"";
                    PropertyPath = @"";
                    return;
                }
                int i = value.IndexOf('.');
                if (i > 0)
                {
                    Tag = value.Substring(0, i);
                    PropertyPath = value.Substring(i);
                }                
                else
                {
                    Tag = value;
                    PropertyPath = @"";
                }                
            }
        }

        /// <summary>
        ///     The tag part of the ElementId. E.g. 'FIC3310' 
        /// </summary>
        public virtual string Tag
        {
            get { return _tag; }
            set
            {
                SetValue(ref _tag, value);
                OnPropertyChanged(() => ElementId);
            }
        }

        /// <summary>
        ///     The property part of the ElementId. E.g. '.PV' 
        /// </summary>
        public virtual string PropertyPath
        {
            get { return _propertyPath; }
            set
            {
                SetValue(ref _propertyPath, value);
                OnPropertyChanged(() => ElementId);
            }
        }
       
        public virtual string Desc
        {
            get { return _desc; }
            set { SetValue(ref _desc, value); }
        }
        
        public virtual string Area
        {
            get { return _area; }
            set { SetValue(ref _area, value); }
        }
        
        public virtual AlarmCondition OriginalAlarmCondition
        {
            get { return _originalAlarmCondition; }
            set { SetValue(ref _originalAlarmCondition, value); }
        }
        
        public virtual AlarmCondition CurrentAlarmCondition
        {
            get { return _currentAlarmCondition; }
            set { SetValue(ref _currentAlarmCondition, value); }
        }

        /// <summary>
        /// The alarm type (i.e. Level, Deviation, RateOfChange etc)
        /// </summary>
        public virtual string AlarmConditionType
        {
            get { return _alarmConditionType; }
            set { SetValue(ref _alarmConditionType, value); }
        }

        /// <summary>
        ///     0 - No Alarm (Green); 1 - Warning Alarm (Yellow); 2 - Blocking Alarm (Red)
        /// </summary>
        public virtual uint CategoryId
        {
            get { return _categoryId; }
            set { SetValue(ref _categoryId, value); }
        }

        /// <summary>
        /// An integer value representing the severity, or priority, of the alarm
        /// 0 - No Action, 1 - Low, 2 - High, 3 - Emergency
        /// </summary>
        public virtual uint Priority
        {
            get { return _priority; }
            set { SetValue(ref _priority, value); }
        }

        /// <summary>
        /// A message to display to the user regarding this alarm.
        /// </summary>
        public virtual string TextMessage
        {
            get { return _textMessage; }
            set { SetValue(ref _textMessage, value); }
        }
        
        public virtual double CurrentValue
        {
            get { return _currentValue; }
            set { SetValue(ref _currentValue, value); }
        }
        
        /// <summary>
        /// The <see cref="CurrentValue"/> as a string
        /// </summary>
        /// <remarks>        
        /// This property is mostly intended for use with digital alarms.  While the value may be "1" or "0",
        /// the alarm text should be "On/Off", or "Start/Stop", or "High/Low"  So if required, the text
        /// can be displayed rather than the actual numeric value.
        /// </remarks>
        public virtual string CurrentValueText
        {
            get { return _currentValueText; }
            set { SetValue(ref _currentValueText, value); }
        }

        /// <summary>
        /// The value at which the alarm occurs
        /// </summary>        
        public virtual double TripValue
        {
            get { return _tripValue; }
            set { SetValue(ref _tripValue, value); }
        }

        /// <summary>
        /// The trip value as a string
        /// </summary>
        /// <remarks>        
        /// This property is mostly intended for use with digital alarms.  While the value may be "1" or "0",
        /// the alarm text should be "On/Off", or "Start/Stop", or "High/Low"  So if required, the text
        /// can be displayed rather than the actual numeric value.
        /// </remarks>
        public virtual string TripValueText
        {
            get { return _tripValueText; }
            set { SetValue(ref _tripValueText, value); }
        }

        /// <summary>
        /// The EventId associated with the alarm.
        /// Used for acknowledging the alarms.
        /// </summary>
        public virtual EventId? EventId { get; set; }

        /// <summary>
        /// The EventMessage object that was used to generate this AlarmInfoViewModelBase
        /// </summary>
        public virtual EventMessage? OriginalEventMessage { get; set; }

        /// <summary>
        /// The engineering units for the current alarm
        /// </summary>        
        public virtual string EU
        {
            get { return _eu; }
            set { SetValue(ref _eu, value); }
        }

        /// <summary>
        /// A quick reference to determine if the value is a digital value.
        /// </summary>
        public virtual bool IsDigital
        {
            get { return _isDigital; }
            set { SetValue(ref _isDigital, value); }
        }

        public bool AlarmConditionChanged { get; set; }

        public bool UnackedChanged { get; set; }

        public override string ToString()
        {
            return @"";
        }

        #endregion

        #region private fields

        private DateTime _occurrenceTime;
        private DateTime _timeLastActive;
        
        private string _tag = @"";        
        private string _propertyPath = @"";

        private string _desc = @"";
        private string _area = @"";
        private bool _alarmIsActive;
        private bool _alarmIsUnacked;
        private uint _categoryId;
        private uint _priority;
        private string _textMessage = @"";

        protected string _alarmConditionType = @"";
        protected AlarmCondition _currentAlarmCondition;
        protected AlarmCondition _originalAlarmCondition;
        protected double _currentValue;
        protected string _currentValueText = @"";
        protected double _tripValue;
        protected string _tripValueText = @"";
        protected string _eu = @"";        
        protected bool _isDigital;

        #endregion
    }
}