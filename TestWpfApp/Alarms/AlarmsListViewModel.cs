using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Wpf;
using TestWpfApp;

namespace Ssz.WpfHmi.Common.ControlsRuntime.GenericRuntime
{
    public class AlarmsListViewModel : ViewModelBase
    {       

        #region public functions        

        public ObservableCollection<AlarmInfoViewModel> Alarms
        {
            get { return _alarms; }
        }

        public ObservableCollection<AlarmInfoViewModel> HighPriorityAlarms
        {
            get { return _highPriorityAlarms; }
        }

        public ObservableCollection<AlarmInfoViewModel> Last5UnackedAlarms
        {
            get { return _last5UnackedAlarms; }
        }

        public int UnackedAlarmsCount
        {
            get { return _unackedAlarmsCount; }
            set { SetValue(ref _unackedAlarmsCount, value); }
        }

        public int ActiveOrUnackedAlarmsCount
        {
            get { return _activeOrUnackedAlarmsCount; }
            set { SetValue(ref _activeOrUnackedAlarmsCount, value); }
        }

        public bool HasUnackedAlarms
        {
            get { return _hasUnackedAlarms; }
            set { SetValue(ref _hasUnackedAlarms, value); }
        }

        public bool HasActiveOrUnackedAlarms
        {
            get { return _hasActiveOrUnackedAlarms; }
            set { SetValue(ref _hasActiveOrUnackedAlarms, value); }
        }

        public void OnAlarmNotification(IEnumerable<AlarmInfoViewModelBase> alarmInfoViewModels)
        {
            bool listsChanged = false;

            foreach (AlarmInfoViewModelBase alarmInfoViewModel in alarmInfoViewModels)
            {
                if (OnAlarmNotification(alarmInfoViewModel)) listsChanged = true;
            }

            if (listsChanged)
            {
                int unackedAlarmsCount = 0;
                int activeOrUnackedAlarmsCount = 0;
                for (int i = 0; i < _alarms.Count; i++)
                {
                    AlarmInfoViewModel alarm = _alarms[i];
                    alarm.Num = i + 1;
                    if (alarm.AlarmIsUnacked)
                        unackedAlarmsCount++;
                    if ((alarm.AlarmIsActive || alarm.AlarmIsUnacked) &&
                        alarm.CurrentAlarmCondition != AlarmCondition.None)
                        activeOrUnackedAlarmsCount++;
                }
                UnackedAlarmsCount = unackedAlarmsCount;
                ActiveOrUnackedAlarmsCount = activeOrUnackedAlarmsCount;
                HasUnackedAlarms = unackedAlarmsCount > 0;
                HasActiveOrUnackedAlarms = activeOrUnackedAlarmsCount > 0;

                for (int i = 0; i < _highPriorityAlarms.Count; i++)
                {
                    _highPriorityAlarms[i].Num = i + 1;
                }

                AlarmInfoViewModel[] last5UnackedAlarms = _alarms.Where(a => a.AlarmIsUnacked && a.CategoryId > 0).Take(5).ToArray();
                if (!last5UnackedAlarms.SequenceEqual(_last5UnackedAlarms))
                {
                    _last5UnackedAlarms.Clear();
                    foreach (AlarmInfoViewModel row in last5UnackedAlarms)
                    {
                        _last5UnackedAlarms.Add(row);
                    }
                }
            }
        }

        public void Clear()
        {
            UnackedAlarmsCount = 0;
            ActiveOrUnackedAlarmsCount = 0;
            HasUnackedAlarms = false;
            HasActiveOrUnackedAlarms = false;
            _alarms.Clear();
            _last5UnackedAlarms.Clear();
            _highPriorityAlarms.Clear();
        }

        #endregion

        #region private functions        

        /// <summary>
        ///     Process alarm list manipulation when new alarm message is arrived. Removed old alarmd (returned to normal) and so
        ///     on
        /// </summary>
        /// <param name="alarmsList">alarm list</param>
        /// <param name="newAlarm">new incomming alarm</param>
        /// <returns>true if alarms list had been changed</returns>
        private static bool ProcessAlarm(IList<AlarmInfoViewModel> alarmsList, AlarmInfoViewModel newAlarm)
        {            
            IEnumerable<AlarmInfoViewModel> alarmsSameEventSourceObject = alarmsList.Where(a =>
                    String.Equals(a.Tag, newAlarm.Tag, StringComparison.InvariantCultureIgnoreCase)
                    ).ToArray();

            var alarmsToRemove = new List<AlarmInfoViewModel>();

            if (newAlarm.CurrentAlarmCondition == AlarmCondition.None)
            {
                if (newAlarm.AlarmIsUnacked)
                {
                    foreach (AlarmInfoViewModel a in alarmsSameEventSourceObject)
                    {
                        a.AlarmIsActive = false;                        
                    }
                }
                else
                {
                    alarmsToRemove.AddRange(alarmsSameEventSourceObject);                    
                }
            }
            else
            {
                alarmsToRemove.AddRange(alarmsSameEventSourceObject);

                var eventSourceObject = App.EventSourceModel.GetEventSourceObject(newAlarm.Tag);
                var activeConditions = eventSourceObject.AlarmConditions.Where(kvp => kvp.Value.Active).ToArray();
                if (activeConditions.Length > 0)
                {
                    var kvp = activeConditions.OrderByDescending(i => i.Value.CategoryId).ThenByDescending(i => i.Value.ActiveOccurrenceTime).First();
                    newAlarm.CategoryId = kvp.Value.CategoryId;
                    newAlarm.CurrentAlarmCondition = kvp.Key;
                    newAlarm.AlarmIsActive = kvp.Value.Active;
                    newAlarm.AlarmIsUnacked = kvp.Value.Unacked;                    

                    alarmsList.Insert(0, newAlarm);
                }
                else
                {
                    var unackedConditions = eventSourceObject.AlarmConditions.Where(kvp => kvp.Value.Unacked).ToArray();
                    if (unackedConditions.Length > 0)
                    {
                        var kvp = unackedConditions.OrderByDescending(i => i.Value.CategoryId).ThenByDescending(i => i.Value.ActiveOccurrenceTime).First();
                        newAlarm.CategoryId = kvp.Value.CategoryId;
                        newAlarm.CurrentAlarmCondition = kvp.Key;
                        newAlarm.AlarmIsActive = kvp.Value.Active;
                        newAlarm.AlarmIsUnacked = kvp.Value.Unacked;

                        alarmsList.Insert(0, newAlarm);
                    }
                }
            }            

            foreach (AlarmInfoViewModel a in alarmsToRemove)
            {
                alarmsList.Remove(a);
            }

            return true;
        }

        /// <summary>
        ///     Process single alarm message.
        /// </summary>
        /// <param name="alarmInfoViewModel"></param>
        /// <returns></returns>
        private bool OnAlarmNotification(AlarmInfoViewModelBase alarmInfoViewModel)
        {
            bool listsChanged = false;

            // Add alarm to general alarm list
            var newAlarm = new AlarmInfoViewModel(alarmInfoViewModel);
            if (ProcessAlarm(_alarms, newAlarm)) listsChanged = true;

            if (newAlarm.Priority > 2)
            {
                // Add alarm to high priority alarm list
                // We have to create separate variable for each particular alarm lists
                var highPriorityAlarm = new AlarmInfoViewModel(alarmInfoViewModel);
                if (ProcessAlarm(_highPriorityAlarms, highPriorityAlarm)) listsChanged = true;
            }

            return listsChanged;
        }

        #endregion

        #region private fields

        private readonly ObservableCollection<AlarmInfoViewModel> _alarms =
            new ObservableCollection<AlarmInfoViewModel>();

        private readonly ObservableCollection<AlarmInfoViewModel> _highPriorityAlarms =
            new ObservableCollection<AlarmInfoViewModel>();

        private readonly ObservableCollection<AlarmInfoViewModel> _last5UnackedAlarms =
            new ObservableCollection<AlarmInfoViewModel>();

        private int _unackedAlarmsCount;
        private int _activeOrUnackedAlarmsCount;

        private bool _hasUnackedAlarms;
        private bool _hasActiveOrUnackedAlarms;

        #endregion
    }    
}