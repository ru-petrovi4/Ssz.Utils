using Ssz.Utils;
using Ssz.Utils.DataSource;
using Ssz.WpfHmi.Common.ControlsRuntime.GenericRuntime;
using Ssz.WpfHmi.Common.ModelData.Events;
using Ssz.Xi.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TestWpfApp.Alarms;
using Xi.Contracts.Data;

namespace TestWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDispatcher
    {
        public MainWindow()
        {
            InitializeComponent();

            _alarmsListViewModel = new AlarmsListViewModel();
            MainAlarmListControl.MainDataGrid.ItemsSource = _alarmsListViewModel.Alarms;

            App.XiDataProvider.Initialize(this, true, @"http://localhost:60080/SszCtcmXiServer/ServerDiscovery", "TestWpfApp", Environment.MachineName, new string[0], new CaseInsensitiveDictionary<string>());
            App.XiDataProvider.EventNotification += XiDataProviderOnEventNotification;
            App.XiDataProvider.Disconnected += XiDataProviderOnDisconnected;

            _valueSubscription = new ValueSubscription(App.XiDataProvider,
                "BP2.propTransmValueDspl",
                (oldValue, newValue) =>
                {
                    MainTextBlock.Text = newValue.ValueAsString(true);
                });
        }

        private void XiDataProviderOnDisconnected()
        {
            _alarmsListViewModel.Clear();

            EventSourceModel.Instance.Clear();
        }

        private async void XiDataProviderOnEventNotification(IEnumerable<Xi.Contracts.Data.EventMessage> newEventMessages)
        {
            List<AlarmInfoViewModelBase> newAlarmInfoViewModels = new List<AlarmInfoViewModelBase>();
            foreach (Xi.Contracts.Data.EventMessage eventMessage in newEventMessages.Where(em => em != null).OrderBy(em => em.OccurrenceTime))
            {
                var alarmInfoViewModels = await CtcmModelEngine.ProcessEventMessage(eventMessage);
                if (alarmInfoViewModels != null)
                {
                    newAlarmInfoViewModels.AddRange(alarmInfoViewModels);
                }
            }

            if (newAlarmInfoViewModels.Count > 0)
            {
                _alarmsListViewModel.OnAlarmNotification(newAlarmInfoViewModels);

                EventSourceModel.Instance.OnAlarmsListChanged();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _valueSubscription.Dispose();

            App.XiDataProvider.Close();

            base.OnClosed(e);
        }

        public void BeginInvoke(Action<CancellationToken> action)
        {
            Dispatcher.Invoke(action, _cancellationTokenSource.Token);
        }

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ValueSubscription _valueSubscription;

        private readonly AlarmsListViewModel _alarmsListViewModel;
    }
}
