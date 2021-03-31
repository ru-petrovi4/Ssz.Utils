using System;
using System.Xml.Serialization;
using Ssz.Utils.DataSource;
using Ssz.Utils.Wpf;

namespace Ssz.WpfHmi.Common.ModelData.Events
{
    public class AlarmInfoViewModelBase : ViewModelBase
    {
        #region construction and destruction
        /// <summary>
        /// Default constructor
        /// </summary>
        public AlarmInfoViewModelBase()
        {
        }

        /// <summary>
        ///     Copy constructor       
        /// </summary>
        /// <param name="that"></param>
        public AlarmInfoViewModelBase(AlarmInfoViewModelBase that)
        {
            _active = that._active;
            _unacked = that._unacked;
            _occurrenceTime = that._occurrenceTime;
            _timeLastActive = that._timeLastActive;
            _tagName = that._tagName;
            _parameter = that._parameter;
            _desc = that._desc;
            _area = that._area;
            _originalAlarmCondition = that._originalAlarmCondition;
            _currentAlarmCondition = that._currentAlarmCondition;
            _categoryName = that._categoryName;
            _categoryId = that._categoryId;
            _priority = that._priority;
            _textMessage = that._textMessage;
            _currentValue = that._currentValue;
            _currentValueRegister = that._currentValueRegister;
            _currentValueText = that._currentValueText;
            _tripValue = that._tripValue;
            _tripValueText = that._tripValueText;
            EventId = that.EventId;
            OriginalEventMessage = that.OriginalEventMessage;
            _eu = that._eu;
            _isDigital = that._isDigital;
            _tripValue = that._tripValue;
            _tripValueText = that._tripValueText;
        }
        #endregion

        #region public functions
        /// <summary>
        /// Determines if the alarm is active or not.  An alarm can be in one of the following states:
        /// - Active and Acknowledged
        /// - Active and Unacknowledged
        /// - Inactive and Acknowledged
        /// - Inactive and Unacknowledged
        /// This is the same as the <seealso cref="IsInAlarm"/> property
        /// </summary>
        public virtual bool Active
        {
            get { return _active; }
            set
            {
                SetValue(ref _active, value);
                OnPropertyChanged(() => IsInAlarm);
            }
        }
        /// <summary>
        /// Determines if the item is in alarm or not.  This is the same as the <seealso cref="Active"/> property
        /// </summary>
        /// <remarks>
        /// An alarm is still "active" when it is no longer in alarm, but still unacknowledged.
        /// </remarks>
        /// <returns></returns>
        public virtual bool IsInAlarm
        {
            get { return Active; }
        }
        /// <summary>
        /// Determines if the alarm is acknowledged or not.  his is the inverse of the <seealso cref="IsAcknowledged"/>
        /// property
        /// </summary>
        public virtual bool Unacked
        {
            get { return _unacked; }
            set
            {
                SetValue(ref _unacked, value);
                OnPropertyChanged(() => IsAcknowledged);
            }
        }
        /// <summary>
        /// Determines if the alarm is acknowledged or not.  This is the inverse of the <seealso cref="Unacked"/> property
        /// </summary>
        public virtual bool IsAcknowledged
        {
            get { return !Unacked; }
        }
        /// <summary>
        /// The time when the alarm went into alarm
        /// </summary>
        public virtual DateTime OccurrenceTime
        {
            get { return _occurrenceTime; }
            set { SetValue(ref _occurrenceTime, value); }
        }
        /// <summary>
        /// The time when the alarm went out of alarm
        /// </summary>
        public virtual DateTime TimeLastActive
        {
            get { return _timeLastActive; }
            set { SetValue(ref _timeLastActive, value); }
        }
        /// <summary>
        /// The full name (tag.parameter)
        /// != null
        /// </summary>
        [XmlIgnore] //serialized by the Tag and Parameter properties
        public virtual string FullTagName
        {
            get { return String.IsNullOrEmpty(Parameter) ? Tag : Tag + "." + Parameter; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    Tag = string.Empty;
                    Parameter = string.Empty;
                    return;
                }
                int i = value.IndexOf('.');
                if (i > 0)
                {
                    Tag = value.Substring(0, i);
                    Parameter = value.Substring(i + 1);
                }                
                else
                {
                    Tag = value;
                    Parameter = string.Empty;
                }                
            }
        }
        /// <summary>
        /// The tag part of the tag.parameter
        /// </summary>
        public virtual string Tag
        {
            get { return _tagName; }
            set
            {
                SetValue(ref _tagName, value);
                OnPropertyChanged(() => FullTagName);
            }
        }
        /// <summary>
        /// The parameter part of the tag.parameter
        /// </summary>
        public virtual string Parameter
        {
            get { return _parameter; }
            set
            {
                SetValue(ref _parameter, value);
                OnPropertyChanged(() => FullTagName);
            }
        }
        /// <summary>
        /// The tag description (same as PTDesc)
        /// </summary>
        /// <remarks>
        /// This is determined from the ClientRequestedFields as defined by Attributes_PtDesc
        /// </remarks>
        public virtual string Desc
        {
            get { return _desc; }
            set { SetValue(ref _desc, value); }
        }
        /// <summary>
        /// The area of the plant where the tag is located
        /// </summary>
        public virtual string Area
        {
            get { return _area; }
            set { SetValue(ref _area, value); }
        }
        /// <summary>
        /// The type of alarm (High, HighHigh, Low, LowLow, ChangeOfState, OffNormal etc)
        /// </summary>
        /// <remarks>
        /// The alarm type can change over the course of time.  The tag may have originally been a PVL, but has
        /// now become more severe and turned into a PVLL.  This property contains the state of the alarm when
        /// it was first created
        /// </remarks>
        public virtual AlarmConditionType OriginalAlarmCondition
        {
            get { return _originalAlarmCondition; }
            set { SetValue(ref _originalAlarmCondition, value); }
        }
        /// <summary>
        /// The type of alarm (High, HighHigh, Low, LowLow, ChangeOfState, OffNormal etc)
        /// </summary>
        /// <remarks>
        /// The alarm type can change over the course of time.  The tag may have originally been a PVL, but has
        /// now become more severe and turned into a PVLL.  This property contains the current state of the tag
        /// </remarks>
        public virtual AlarmConditionType CurrentAlarmCondition
        {
            get { return _currentAlarmCondition; }
            set { SetValue(ref _currentAlarmCondition, value); }
        }
        /// <summary>
        /// The alarm category (i.e. Level, Deviation, RateOfChange etc)
        /// </summary>
        public virtual string CategoryName
        {
            get { return _categoryName; }
            set { SetValue(ref _categoryName, value); }
        }
        /// <summary>
        /// 0 - No Alarm (Green); 1 - Warning Alarm (Yellow); 2 - Blocking Alarm (Red)
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
        /// A message to display to the user regarding this alarm.  This message is generated by the alarm OPC server
        /// </summary>
        public virtual string TextMessage
        {
            get { return _textMessage; }
            set { SetValue(ref _textMessage, value); }
        }
        /// <summary>
        /// The current value that the tag.parameter is in
        /// </summary>
        public virtual double CurrentValue
        {
            get { return _currentValue; }
            set { SetValue(ref _currentValue, value); }
        }
        /// <summary>
        /// The mapped register ID where the CurrentValue is being pulled from
        /// </summary>
        /// <remarks>
        /// This is determined from the ClientRequestedFields as defined by Attribute_CVRegister
        /// The ValueRegister is used to determine which alarms are "lumped together" as "similar" alarms.
        /// i.e. PVH, PVHH, PVL, and PVLL alarms are all using the PV register as the source of their
        /// <see cref="CurrentValue"/>
        /// </remarks>
        public virtual int CurrentValueRegister
        {
            get { return _currentValueRegister; }
            set { SetValue(ref _currentValueRegister, value); }
        }
        /// <summary>
        /// The <see cref="CurrentValue"/> as a string
        /// </summary>
        /// <remarks>
        /// This is determined from the ClientRequestedFields as defined by Attributes_CurrentValueText
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
        /// <remarks>
        /// This is determined from the ClientRequestedFields as defined by Attributes_TripValue
        /// </remarks>
        public virtual double TripValue
        {
            get { return _tripValue; }
            set { SetValue(ref _tripValue, value); }
        }
        /// <summary>
        /// The trip value as a string
        /// </summary>
        /// <remarks>
        /// This is determined from the ClientRequestedFields as defined by Attributes_TripValueText
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
        /// The EventId associated with the alarm.  The EventId is important because we
        /// use it for acknowledging the alarms.
        /// </summary>
        public virtual EventId? EventId { get; set; }
        /// <summary>
        /// The EventMessage object that was used to generate this AlarmInfoViewModelBase
        /// </summary>
        public virtual EventMessage? OriginalEventMessage { get; set; }
        /// <summary>
        /// The engineering units for the current alarm
        /// </summary>
        /// <remarks>
        /// This is determined from the ClientRequestedFields as defined by Attributes_EuDesc
        /// </remarks>
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
        /// <summary>
        /// Convert the alarm into a string.
        /// </summary>
        /// <returns>
        /// An empty string - we don't want this to visualize as anything
        /// </returns>
        public override string ToString()
        {
            return string.Empty;
        }
        #endregion
        #region private fields

        private DateTime _occurrenceTime;
        private DateTime _timeLastActive;
        
        private string _tagName = String.Empty;
        
        private string _parameter = String.Empty;
        private string _desc = String.Empty;
        private string _area = String.Empty;
        private bool _active;
        private bool _unacked;
        private uint _categoryId;
        private uint _priority;
        private string _textMessage = String.Empty;

        protected string _categoryName = String.Empty;
        protected AlarmConditionType _currentAlarmCondition;
        protected AlarmConditionType _originalAlarmCondition;
        protected double _currentValue;
        protected string _currentValueText = String.Empty;
        protected double _tripValue;
        protected string _tripValueText = String.Empty;
        protected string _eu = String.Empty;
        protected int _currentValueRegister;
        protected bool _isDigital;

        #endregion
    }
    /// <summary>
    /// The different Alarm conditions that an alarm can be in.
    /// </summary>
    public enum AlarmConditionType
    {
        None,
        Low,
        LowLow,
        High,
        HighHigh,
        PVLevel,
        DVLow,
        DVHigh,
        DigitalHigh,
        DigitalLow,
        NegativeRate,
        PositiveRate,
        OffNormal,
        ChangeOfState,
        CommandDisagree,
        CommandFail,
        Uncommanded,
        Trip,
        Interlock,
        AnswerbackHigh,
        AnswerbackLow,
        Other,
    }
}