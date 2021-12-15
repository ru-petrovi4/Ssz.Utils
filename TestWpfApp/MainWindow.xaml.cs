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

            var contextParams = new CaseInsensitiveDictionary<string>();
            //contextParams.Add("SessionId", "ade2a090-d388-40ce-87fe-d5017cdf9c66");
            //contextParams.Add("UserName", "valpo");
            //contextParams.Add("UserRole", "TRAINEE");
            //contextParams.Add("WindowsUserName", "valpo");            
            App.DataAccessProvider.Initialize(null, true, true, @"http://SRVEPKS01B:60080/SimcodeOpcNetServer/ServerDiscovery", "TestWpfApp", Environment.MachineName, "", contextParams);
            App.DataAccessProvider.EventMessagesCallback += XiDataAccessProviderOnEventMessagesCallback;
            App.DataAccessProvider.PropertyChanged += DataAccessProviderOnPropertyChanged;
            XiDataAccessProviderOnConnectedOrDisconnected();

            _valueSubscription = new ValueSubscription(App.DataAccessProvider,
                "/ASSETS/PI/PI_D1.PV",
                (oldVst, newVst) =>
                {
                    MainTextBlock.Text = newVst.Value.ValueAsString(true);
                });
        }

        private void DataAccessProviderOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == @"IsConnected")
            {
                XiDataAccessProviderOnConnectedOrDisconnected();
            }
        }

        private void XiDataAccessProviderOnConnectedOrDisconnected()
        {
            if (App.DataAccessProvider.IsConnected)
            {
                Title = "Connected";
            }
            else
            {
                Title = "Disconnected";
                _alarmsListViewModel.Clear();

                App.EventSourceModel.Clear();
            }            
        }

        private async void XiDataAccessProviderOnEventMessagesCallback(EventMessage[] newEventMessages)
        {
            List<AlarmInfoViewModelBase> newAlarmInfoViewModels = new List<AlarmInfoViewModelBase>();
            foreach (EventMessage eventMessage in newEventMessages.Where(em => em is not null).OrderBy(em => em.OccurrenceTimeUtc))
            {
                var alarmInfoViewModels = await ExperionHelper.ProcessEventMessage(App.EventSourceModel, eventMessage);
                if (alarmInfoViewModels is not null)
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
