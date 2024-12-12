using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsPlay.GenericPlay;
using Ssz.Operator.Core.DataAccess;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsCommon
{
    public partial class GenericAlarmListControl : IAlarmListControl
    {
        #region construction and destruction

        public GenericAlarmListControl(IEnumerable itemsSource)
        {
            InitializeComponent();

            MainDataGrid.ItemsSource = itemsSource;
        }

        #endregion

        #region public functions

        public void AckAlarms()
        {
            var eventIds = new List<EventId>(MainDataGrid.Items.Count);

            for (var i = 0; i < MainDataGrid.Items.Count; i += 1)
                if (TreeHelper.IsUserVisible(
                    MainDataGrid.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement, MainDataGrid))
                {
                    var alarm = MainDataGrid.Items[i] as GenericDsAlarmInfoViewModel;
                    if (alarm is not null && alarm.EventId is not null)
                        eventIds.Add(alarm.EventId);
                }

            if (eventIds.Count > 0) DsDataAccessProvider.Instance.AckAlarms("", "", eventIds.ToArray());
        }

        #endregion

        #region private functions

        private void MainDataGridOnMouseDoubleClick(object? sender, MouseButtonEventArgs e)
        {
            if (sender is null) return;

            var selectedItem = (GenericDsAlarmInfoViewModel) ((DataGrid) sender).SelectedItem;
            if (selectedItem is null) return;

            var dsPageDrawing = DsProject.Instance.AllDsPagesCacheFindDsPageDrawing(selectedItem.TagName)
                .FirstOrDefault();
            if (dsPageDrawing is not null && !dsPageDrawing.IsFaceplate)
                CommandsManager.NotifyCommand(PlayDsProjectView.GetPlayWindow(this)?.MainFrame,
                    CommandsManager.JumpCommand,
                    new JumpDsCommandOptions
                    {
                        TargetWindow = TargetWindow.RootWindow,
                        FileRelativePath = DsProject.Instance.GetFileRelativePath(dsPageDrawing.FileFullName)
                    });
        }

        #endregion
    }
}