using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.WpfHmi.Common.ControlsRuntime.GenericRuntime;
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

            App.DataAccessProvider.Initialize(this, null, true, true, @"http://localhost:60080/SszCtcmXiServer/ServerDiscovery", "TestWpfApp", Environment.MachineName, "", new CaseInsensitiveDictionary<string>());
            App.DataAccessProvider.EventMessagesCallback += XiDataAccessProviderOnEventMessagesCallback;
            //App.DataAccessProvider.Disconnected += XiDataAccessProviderOnDisconnected;

            _valueSubscription = new ValueSubscription(App.DataAccessProvider,
                "BP2.propTransmValueDspl",
                (oldVst, newVst) =>
                {
                    MainTextBlock.Text = newVst.Value.ValueAsString(true);
                });
        }

        private void XiDataAccessProviderOnDisconnected()
        {
            _alarmsListViewModel.Clear();

            App.EventSourceModel.Clear();
        }

        private async void XiDataAccessProviderOnEventMessagesCallback(EventMessage[] newEventMessages)
        {
            List<AlarmInfoViewModelBase> newAlarmInfoViewModels = new List<AlarmInfoViewModelBase>();
            foreach (EventMessage eventMessage in newEventMessages.Where(em => em != null).OrderBy(em => em.OccurrenceTime))
            {
                var alarmInfoViewModels = await DeltaSimHelper.ProcessEventMessage(App.EventSourceModel, eventMessage);
                if (alarmInfoViewModels != null)
                {
                    newAlarmInfoViewModels.AddRange(alarmInfoViewModels);
                }
            }

            if (newAlarmInfoViewModels.Count > 0)
            {
                _alarmsListViewModel.OnAlarmNotification(newAlarmInfoViewModels);

                App.EventSourceModel.OnAlarmsListChanged();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _valueSubscription.Dispose();

            App.DataAccessProvider.Close();

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
