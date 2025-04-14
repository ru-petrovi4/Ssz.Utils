using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Utils;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.DataAccess
{
    public partial class DsDataAccessProvider : GrpcDataAccessProvider
    {
        #region public functions
        
        public event Action<IEnumerable<DsAlarmInfoViewModelBase>> AlarmNotification
        {
            add
            {
                var list = PlayDsProjectView.EventSourceModel.GetExistingAlarmInfoViewModels().OfType<DsAlarmInfoViewModelBase>().ToList();
                if (list.Count != 0)
                    value.Invoke(list);

                _alarmNotification += value;
            }
            remove => _alarmNotification -= value;
        }

        public event Action<IEnumerable<JournalRecordViewModel>> JournalRecordNotification = delegate { };

        public void AckAlarms(EventId[] eventIds)
        {
            base.AckAlarms("", "", eventIds);
        }

        #endregion

        #region protected functions

        protected override async void OnClientEventListManager_EventMessagesCallback(EventMessagesCollection eventMessagesCollection)
        {
            base.OnClientEventListManager_EventMessagesCallback(eventMessagesCollection);

            var genericDataEngine = DsProject.Instance.DataEngine;

            (List<DsAlarmInfoViewModelBase> newAlarmInfoViewModels, List<JournalRecordViewModel> newJournalRecordViewModels) = 
                await genericDataEngine.ProcessEventMessages(eventMessagesCollection);            

            if (newAlarmInfoViewModels.Count > 0 && _alarmNotification is not null)
                _alarmNotification(newAlarmInfoViewModels);

            if (newJournalRecordViewModels.Count > 0)
                JournalRecordNotification(newJournalRecordViewModels);
        }

        #endregion        

        #region private fields
        
        private Action<IEnumerable<DsAlarmInfoViewModelBase>>? _alarmNotification;        

        #endregion
    }
}