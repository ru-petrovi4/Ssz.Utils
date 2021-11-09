using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Utils.Wpf;
using TestWpfApp;
using Ssz.WpfHmi.Common.ControlsRuntime.GenericRuntime;
using Ssz.Utils.DataAccess;

namespace Ssz.WpfHmi.Common.ControlsCommon
{
    /// <summary>
    ///     Interaction logic for AlarmListControl.xaml
    /// </summary>
    public partial class GenericAlarmListControl
    {
        #region construction and destruction

        public GenericAlarmListControl()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public void AckAlarms()
        {
            var eventIds = new List<EventId>(MainDataGrid.Items.Count);

            for (int i = 0; i < MainDataGrid.Items.Count; i++)
            {
                if (TreeHelper.IsUserVisible(MainDataGrid.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement, MainDataGrid))
                {
                    var alarm = MainDataGrid.Items[i] as AlarmInfoViewModel;
                    if (alarm is not null && alarm.EventId is not null) 
                        eventIds.Add(alarm.EventId);
                }
            }

            if (eventIds.Count > 0) App.DataAccessProvider.AckAlarms("", "", eventIds.ToArray());
        }

        #endregion

        #region private functions


        #endregion
    }
}