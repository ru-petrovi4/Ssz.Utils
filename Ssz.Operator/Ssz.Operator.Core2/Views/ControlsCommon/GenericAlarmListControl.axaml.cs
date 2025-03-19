using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsPlay.GenericPlay;
using Ssz.Operator.Core.DataAccess;
using Ssz.Utils.DataAccess;
using Ssz.Operator.Core.Utils;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsCommon
{
    public partial class GenericAlarmListControl : UserControl, IAlarmListControl
    {
        #region construction and destruction

        public GenericAlarmListControl()
        {
            InitializeComponent();

            MainDataGrid.LoadingRow += MainDataGrid_LoadingRow;
            MainDataGrid.UnloadingRow += MainDataGrid_UnloadingRow;
        }

        public GenericAlarmListControl(IList itemsSource)
        {
            InitializeComponent();

            MainDataGrid.LoadingRow += MainDataGrid_LoadingRow;
            MainDataGrid.UnloadingRow += MainDataGrid_UnloadingRow;

            MainDataGrid.ItemsSource = itemsSource;            
        }        

        #endregion

        #region public functions

        public void AckAlarms()
        {
            var eventIds = new List<EventId>(200);

            for (var i = 0; i < _dataGridRows.Count; i += 1)
            {
                var dataGridRow = _dataGridRows[i];
                if (TreeHelper.IsUserVisible(dataGridRow, MainDataGrid))
                {
                    var alarm = dataGridRow.DataContext as GenericDsAlarmInfoViewModel;
                    if (alarm?.EventId is not null)
                        eventIds.Add(alarm.EventId);
                }
            }

            if (eventIds.Count > 0) 
                DsDataAccessProvider.Instance.AckAlarms("", "", eventIds.ToArray());
        }

        #endregion

        #region private functions

        private void MainDataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            _dataGridRows.Add(e.Row);
        }

        private void MainDataGrid_UnloadingRow(object? sender, DataGridRowEventArgs e)
        {
            _dataGridRows.Remove(e.Row);
        }        

        private void MainDataGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (sender is null) 
                return;

            var selectedItem = (GenericDsAlarmInfoViewModel)((DataGrid)sender).SelectedItem;
            if (selectedItem is null)
                return;

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

        #region private fields

        private readonly List<DataGridRow> _dataGridRows = new();

        #endregion
    }
}