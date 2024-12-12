using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.DataAccess;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay.GenericPlay
{
    public class AlarmsListViewModel : ViewModelBase
    {
        #region construction and destruction

        public AlarmsListViewModel()
        {
            DsDataAccessProvider.Instance.AlarmNotification += OnAlarmNotification;
            DsDataAccessProvider.Instance.PropertyChanged += DataAccessProviderOnPropertyChanged;
        }

        #endregion

        #region public functions

        public ObservableCollection<GenericDsAlarmInfoViewModel> Alarms { get; } = new();

        public ObservableCollection<GenericDsAlarmInfoViewModel> HighPriorityAlarms { get; } = new();

        public ObservableCollection<GenericDsAlarmInfoViewModel> Last5UnackedAlarms { get; } = new();

        public int UnackedAlarmsCount
        {
            get => _unackedAlarmsCount;
            set => SetValue(ref _unackedAlarmsCount, value);
        }

        public int ActiveOrUnackedAlarmsCount
        {
            get => _activeOrUnackedAlarmsCount;
            set => SetValue(ref _activeOrUnackedAlarmsCount, value);
        }

        public bool HasUnackedAlarms
        {
            get => _hasUnackedAlarms;
            set => SetValue(ref _hasUnackedAlarms, value);
        }

        public bool HasActiveOrUnackedAlarms
        {
            get => _hasActiveOrUnackedAlarms;
            set => SetValue(ref _hasActiveOrUnackedAlarms, value);
        }        

        public void OnAlarmNotification(IEnumerable<DsAlarmInfoViewModelBase> alarmInfoViewModels)
        {
            var listsChanged = false;

            foreach (DsAlarmInfoViewModelBase alarmInfoViewModel in alarmInfoViewModels)
                if (OnAlarmNotification(alarmInfoViewModel))
                    listsChanged = true;

            if (listsChanged)
            {
                var unackedAlarmsCount = 0;
                var activeOrUnackedAlarmsCount = 0;
                for (var i = 0; i < Alarms.Count; i += 1)
                {
                    GenericDsAlarmInfoViewModel alarm = Alarms[i];
                    if (!alarm.IsVisible)
                        continue;
                    alarm.Num = i + 1;
                    if (alarm.AlarmIsUnacked)
                        unackedAlarmsCount += 1;
                    if ((alarm.AlarmIsActive || alarm.AlarmIsUnacked) &&
                        alarm.CurrentAlarmConditionType != AlarmConditionType.None)
                        activeOrUnackedAlarmsCount += 1;
                }

                UnackedAlarmsCount = unackedAlarmsCount;
                ActiveOrUnackedAlarmsCount = activeOrUnackedAlarmsCount;
                HasUnackedAlarms = unackedAlarmsCount > 0;
                HasActiveOrUnackedAlarms = activeOrUnackedAlarmsCount > 0;

                for (var i = 0; i < HighPriorityAlarms.Count; i += 1)
                {
                    GenericDsAlarmInfoViewModel alarm = HighPriorityAlarms[i];
                    if (!alarm.IsVisible)
                        continue;
                    alarm.Num = i + 1;                    
                }                    

                GenericDsAlarmInfoViewModel[] last5UnackedAlarms =
                    Alarms.Where(a => a.AlarmIsUnacked && a.CategoryId > 0).Take(5).ToArray();
                if (!last5UnackedAlarms.SequenceEqual(Last5UnackedAlarms))
                {
                    Last5UnackedAlarms.Clear();
                    foreach (GenericDsAlarmInfoViewModel row in last5UnackedAlarms) Last5UnackedAlarms.Add(row);
                }

                if (last5UnackedAlarms.Length == 0)
                    PlayDsProjectView.Buzzer.BuzzerState = BuzzerStateEnum.Silent;
            }
        }

        #endregion

        #region private functions

        private void DataAccessProviderOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case @"IsConnected":
                    Clear();
                    break;
            }
        }

        private static bool ProcessAlarm(IList<GenericDsAlarmInfoViewModel> list, GenericDsAlarmInfoViewModel newAlarmInfoViewModel)
        {
            IEnumerable<GenericDsAlarmInfoViewModel> alarmsSameEventSourceObject = list.Where(a =>
                StringHelper.CompareIgnoreCase(a.TagName, newAlarmInfoViewModel.TagName)
            ).ToArray();

            var alarmsToRemove = new List<GenericDsAlarmInfoViewModel>();

            if (newAlarmInfoViewModel.CurrentAlarmConditionType == AlarmConditionType.None)
            {
                if (newAlarmInfoViewModel.AlarmIsUnacked)
                    foreach (GenericDsAlarmInfoViewModel a in alarmsSameEventSourceObject)
                        a.AlarmIsActive = false;
                else
                    alarmsToRemove.AddRange(alarmsSameEventSourceObject);
            }
            else
            {
                alarmsToRemove.AddRange(alarmsSameEventSourceObject);

                var eventSourceObject = PlayDsProjectView.EventSourceModel.GetOrCreateEventSourceObject(newAlarmInfoViewModel.TagName);
                var activeConditions = eventSourceObject.AlarmConditions.Where(kvp => kvp.Value.Active).ToArray();
                if (activeConditions.Length > 0)
                {
                    var kvp = activeConditions.OrderByDescending(i => i.Value.CategoryId)
                        .ThenByDescending(i => i.Value.ActiveOccurrenceTimeUtc).First();
                    newAlarmInfoViewModel.CategoryId = kvp.Value.CategoryId;
                    newAlarmInfoViewModel.Priority = kvp.Value.Priority;
                    newAlarmInfoViewModel.CurrentAlarmConditionType = kvp.Key;
                    newAlarmInfoViewModel.AlarmIsActive = kvp.Value.Active;
                    newAlarmInfoViewModel.AlarmIsUnacked = kvp.Value.Unacked;

                    list.Insert(0, newAlarmInfoViewModel);
                }
                else
                {
                    var unackedConditions = eventSourceObject.AlarmConditions.Where(kvp => kvp.Value.Unacked).ToArray();
                    if (unackedConditions.Length > 0)
                    {
                        var kvp = unackedConditions.OrderByDescending(i => i.Value.CategoryId)
                            .ThenByDescending(i => i.Value.ActiveOccurrenceTimeUtc).First();
                        newAlarmInfoViewModel.CategoryId = kvp.Value.CategoryId;
                        newAlarmInfoViewModel.Priority = kvp.Value.Priority;
                        newAlarmInfoViewModel.CurrentAlarmConditionType = kvp.Key;
                        newAlarmInfoViewModel.AlarmIsActive = kvp.Value.Active;
                        newAlarmInfoViewModel.AlarmIsUnacked = kvp.Value.Unacked;

                        list.Insert(0, newAlarmInfoViewModel);
                    }
                }
            }

            foreach (GenericDsAlarmInfoViewModel a in alarmsToRemove)
            {
                list.Remove(a);
            }

            return true;
        }

        private void Clear()
        {
            UnackedAlarmsCount = 0;
            ActiveOrUnackedAlarmsCount = 0;
            HasUnackedAlarms = false;
            HasActiveOrUnackedAlarms = false;
            Alarms.Clear();
            Last5UnackedAlarms.Clear();
            HighPriorityAlarms.Clear();
            PlayDsProjectView.Buzzer.BuzzerState = BuzzerStateEnum.Silent;
        }


        private bool OnAlarmNotification(DsAlarmInfoViewModelBase alarmInfoViewModelBase)
        {
            var listsChanged = false;

            // Add alarm to general alarm list
            var newAlarmInfoViewModel = new GenericDsAlarmInfoViewModel(alarmInfoViewModelBase);
            if (ProcessAlarm(Alarms, newAlarmInfoViewModel)) listsChanged = true;

            if (newAlarmInfoViewModel.Priority > 2)
            {
                // Add alarm to high priority alarm list
                // We have to create separate variable for each particular alarm lists
                var highPriorityAlarm = new GenericDsAlarmInfoViewModel(alarmInfoViewModelBase);
                if (ProcessAlarm(HighPriorityAlarms, highPriorityAlarm)) listsChanged = true;
            }

            if (newAlarmInfoViewModel.CurrentAlarmConditionType != AlarmConditionType.None &&
                newAlarmInfoViewModel.AlarmIsActive && newAlarmInfoViewModel.AlarmIsUnacked && newAlarmInfoViewModel.ActivateBuzzer && newAlarmInfoViewModel.IsVisible)                
                {
                    if (newAlarmInfoViewModel.Priority == 1 || newAlarmInfoViewModel.Priority == 2)
                    {
                        if (PlayDsProjectView.Buzzer.BuzzerState != BuzzerStateEnum.ProcessAlarmHighPriority)
                            PlayDsProjectView.Buzzer.BuzzerState = BuzzerStateEnum.ProcessAlarmMediumPriority;
                    }
                    else if (newAlarmInfoViewModel.Priority > 2)
                    {
                        PlayDsProjectView.Buzzer.BuzzerState = BuzzerStateEnum.ProcessAlarmHighPriority;
                    }
                }

            return listsChanged;
        }

        #endregion

        #region private fields

        private int _unackedAlarmsCount;
        private int _activeOrUnackedAlarmsCount;

        private bool _hasUnackedAlarms;
        private bool _hasActiveOrUnackedAlarms;

        #endregion
    }
}