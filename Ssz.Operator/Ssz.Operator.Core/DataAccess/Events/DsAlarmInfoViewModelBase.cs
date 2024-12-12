using System;
using System.Linq;
using System.Xml.Serialization;
using Ssz.Operator.Core.DataEngines;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.DataAccess
{
    public class DsAlarmInfoViewModelBase : AlarmInfoViewModelBase
    {
        #region construction and destruction

        public DsAlarmInfoViewModelBase()
        {
        }

        public DsAlarmInfoViewModelBase(DsAlarmInfoViewModelBase that) :
            base(that)
        {
            _tagNameToDisplay = that._tagNameToDisplay;
            _currentAlarmConditionTypeToDisplay = that._currentAlarmConditionTypeToDisplay;            
            _isVisible = that._isVisible;
            _tagAlarmsInfo = that._tagAlarmsInfo;

            that.PropertyChanged += (sender, args) =>
            {
                if (sender is null || args.PropertyName is null) 
                    return;
                var propertyInfo = sender.GetType().GetProperty(args.PropertyName);
                if (propertyInfo is null) 
                    return;
                if (!propertyInfo.CanRead || !propertyInfo.CanWrite) 
                    return;
                var value = propertyInfo.GetValue(sender, null);
                propertyInfo.SetValue(this, value);
            };
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Invokes Update()
        /// </summary>
        public override string TagName
        {
            get => base.TagName;
            set
            {
                base.TagName = value;

                _tagAlarmsInfo = DsProject.Instance.DataEngine.GetTagAlarmsInfo(TagName);

                IsVisible = _tagAlarmsInfo.AlarmsIsVisible;

                Update();
            }
        }        

        public virtual string TagNameToDisplay
        {
            get => _tagNameToDisplay;
            set => SetValue(ref _tagNameToDisplay, value);
        }

        /// <summary>
        ///     Invokes Update()
        /// </summary>
        public override AlarmConditionType CurrentAlarmConditionType
        {
            get => base.CurrentAlarmConditionType;
            set
            {
                base.CurrentAlarmConditionType = value;

                Update();
            }
        }

        /// <summary>
        ///     Possibly updated in Update() method.
        /// </summary>
        public virtual string CurrentAlarmConditionTypeToDisplay
        {
            get => _currentAlarmConditionTypeToDisplay;
            set => SetValue(ref _currentAlarmConditionTypeToDisplay, value);
        }              

        public virtual bool IsVisible
        {
            get => _isVisible;
            set => SetValue(ref _isVisible, value);
        }

        /// <summary>
        ///     Possibly updated in Update() method.
        /// </summary>
        public virtual bool ActivateBuzzer
        {
            get => _activateBuzzer;
            set => SetValue(ref _activateBuzzer, value);
        }

        public virtual TagAlarmsInfo TagAlarmsInfo => _tagAlarmsInfo;

        public bool UpdateIsDisabled { get; set; }

        /// <summary>
        ///     Invoked when Tag or CurrentAlarmConditionType are changed.
        ///     Possibly updates CurrentAlarmConditionTypeToDisplay, Priority, CategoryId, ActivateBuzzer
        /// </summary>
        public virtual void Update()
        {
            if (UpdateIsDisabled)
                return;

            var alarmConditionInfo =
                _tagAlarmsInfo.AlarmConditionInfosList.FirstOrDefault(i =>
                    i.AlarmConditionType == CurrentAlarmConditionType);
            if (alarmConditionInfo is not null)
            {
                if (!String.IsNullOrEmpty(alarmConditionInfo.AlarmConditionTypeToDisplay))
                    CurrentAlarmConditionTypeToDisplay = alarmConditionInfo.AlarmConditionTypeToDisplay;
                if (!String.IsNullOrEmpty(alarmConditionInfo.Priority)) 
                    Priority = new Any(alarmConditionInfo.Priority).ValueAsUInt32(false);
                if (!String.IsNullOrEmpty(alarmConditionInfo.CategoryId)) 
                    CategoryId = new Any(alarmConditionInfo.CategoryId).ValueAsUInt32(false);
                if (!String.IsNullOrEmpty(alarmConditionInfo.ActivateBuzzer))
                    ActivateBuzzer = new Any(alarmConditionInfo.ActivateBuzzer).ValueAsBoolean(false);
            }
        }

        #endregion

        #region private fields

        private string _tagNameToDisplay = @"";
        private string _currentAlarmConditionTypeToDisplay = @"";        
        private bool _isVisible;
        private bool _activateBuzzer = true;
        private TagAlarmsInfo _tagAlarmsInfo = TagAlarmsInfo.Default;

        #endregion
    }
}